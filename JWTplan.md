Plan: JWT 驗證與授權完整實作（含測試與文檔）
建立完整的 JWT 驗證系統，包含 Users 資料表、RefreshToken 機制、SHA256 + salt 密碼雜湊、Role enum、密碼複雜度驗證、測試使用者與訂單資料、角色權限控管、登出功能、錯誤碼區分、詳細日誌記錄、Rate Limiting 整合、完整單元測試覆蓋，以及 JWT 驗證說明文檔。

Steps
建立 Role enum - 在 Models/Entities 新增 UserRole.cs enum（Admin = 1, User = 2）

建立 User 與 RefreshToken 實體模型 - 在 Models/Entities 新增 User.cs（UserId int PK, Username string, PasswordHash string 儲存 salt:hash 格式, Role UserRole, CreatedAt, UpdatedAt）與 RefreshToken.cs（TokenId long PK, UserId int FK, Token string, ExpiresAt, CreatedAt, IsRevoked bool, RevokedAt nullable），建立雙向導航屬性

建立 Entity Configuration - 在 Data/Configurations 新增 UserConfiguration.cs（Username unique index + Chinese_Taiwan_Stroke_CI_AS collation, PasswordHash max length 500）與 RefreshTokenConfiguration.cs（Token unique index + collation max length 128, UserId index, FK to Users cascade delete），在 TradingDbContext.cs 加入 DbSet<User> 與 DbSet<RefreshToken> 並套用 configurations

產生與執行 EF Migration - 執行 dotnet ef migrations add AddUsersAndRefreshTokens，檢查生成的 migration 確認 collation、constraints、indexes 正確，執行 dotnet ef database update 更新資料庫

建立 PasswordHasher 工具類別 - 在 Infrastructure 新增 PasswordHasher.cs static class，實作 HashPassword(string password) 使用 RandomNumberGenerator 產生 32-byte salt + SHA256 hash 返回 Base64Salt:Base64Hash 格式、VerifyPassword(string password, string storedHash) 解析並驗證

建立測試使用者種子資料 - 在 scripts 新增 06_SeedUsers.sql，使用 PasswordHasher 預先產生 hash 後 INSERT 3 個使用者：admin (UserId=1, Username='admin', Role=1, password: Admin@123)、user1 (UserId=2, Username='user1', Role=2, password: User1@123)、user2 (UserId=3, Username='user2', Role=2, password: User2@123)

建立測試訂單資料 - 在 scripts 新增 07_SeedTestOrders.sql，INSERT 模擬訂單資料到 Orders_Write 與 Orders_Read，分配 UserId（admin: 3-5 筆訂單, user1: 5-8 筆訂單, user2: 3-5 筆訂單），包含不同 StockCode、BuySell、OrderStatus、TradeDate

安裝 JWT NuGet 套件 - 在 SecuritiesTradingApi.csproj 新增 Microsoft.AspNetCore.Authentication.JwtBearer

新增 JWT 設定與錯誤碼 - 在 appsettings.json 與 appsettings.Development.json 加入 JwtSettings section（SecretKey 至少 256-bit, Issuer, Audience, AccessTokenExpirationMinutes: 15, RefreshTokenExpirationDays: 7），在 Models 新增 ErrorCodes.cs 定義常數（USER_NOT_FOUND, INVALID_PASSWORD, TOKEN_EXPIRED, REFRESH_TOKEN_EXPIRED, TOKEN_INVALID, TOKEN_REVOKED, FORBIDDEN）

實作 JwtService - 新增 Services/IJwtService.cs 與 Services/JwtService.cs，包含 GenerateAccessToken(User user) 使用 JwtSecurityTokenHandler 產生 JWT with claims（ClaimTypes.NameIdentifier=UserId, ClaimTypes.Name=Username, ClaimTypes.Role=Admin/User）、GenerateRefreshToken() 產生 64-byte Base64 隨機字串、GetPrincipalFromToken(string token, bool validateLifetime = true) 驗證與解析 token

實作 AuthService 含詳細日誌 - 新增 Services/IAuthService.cs 與 Services/AuthService.cs，注入 ILogger<AuthService>，實作 LoginAsync 記錄登入嘗試（成功/失敗 with username & reason）、RefreshTokenAsync 記錄 token refresh（成功/失敗 with userId）、RevokeTokenAsync 記錄撤銷操作、CleanupExpiredTokensAsync 記錄清理數量，使用適當 log levels（Information for success, Warning for failures）

建立 Auth DTOs 與驗證含密碼複雜度 - 在 Models/Dtos 新增 LoginRequestDto、LoginResponseDto（AccessToken, RefreshToken, ExpiresIn, TokenType="Bearer"）、RefreshTokenRequestDto、ErrorResponseDto（ErrorCode, Message），在 Infrastructure/Validators 建立 FluentValidation validators：Username required + length 3-50、Password required + MinimumLength(8) + Matches regex（至少一個大寫、一個小寫、一個數字、一個特殊字元）with 錯誤訊息

建立 AuthController - 新增 Controllers/AuthController.cs 提供 POST /api/auth/login catch exceptions 返回 401 with ErrorResponseDto、POST /api/auth/refresh 返回 401 with error code、POST /api/auth/logout with [Authorize] 撤銷當前 user 的指定 token、DELETE /api/auth/cleanup-expired-tokens with [Authorize(Roles = "Admin")] 返回清理數量

設定 Authentication & Authorization - 在 Program.cs services 區註冊 AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer() 配置 TokenValidationParameters（ValidateIssuer/Audience/Lifetime/IssuerSigningKey=true, ClockSkew=5 mins, ValidAlgorithms=[SecurityAlgorithms.HmacSha256]）與 AddAuthorization()，註冊 IJwtService 與 IAuthService 為 scoped，在 middleware pipeline 的 UseHttpsRedirection() 後插入 UseAuthentication() 與 UseAuthorization()（在 UseRateLimiter() 前）

調整 Rate Limiting 使用 UserId - 在 Program.cs:62 修改 fixed window rate limiter 的 partition key 為 context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous"

實作 OrdersController 授權邏輯 - 在 OrdersController.cs class level 加入 [Authorize]，修改 GET /api/orders 從 HttpContext.User.FindFirst(ClaimTypes.NameIdentifier/Role) 取得 UserId 與 Role，Admin 查全部，User 強制使用 authenticated UserId filter，修改 GET /api/orders/{id} 查詢後檢查 order.UserId（User role 不匹配返回 403 with FORBIDDEN error code），修改 POST /api/orders 強制使用 authenticated UserId 覆蓋 DTO

更新 Swagger 配置 - 在 Program.cs 的 AddSwaggerGen() 加入 AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type=SecuritySchemeType.Http, Scheme="bearer", BearerFormat="JWT", Description="輸入 JWT token（格式：Bearer {token}）" }) 與 AddSecurityRequirement

建立 PasswordHasher 單元測試 - 在 SecuritiesTradingApi.UnitTests 新增 Infrastructure/PasswordHasherTests.cs，測試 HashPassword 產生正確格式、VerifyPassword 正確驗證、相同密碼產生不同 hash（salt 隨機性）、錯誤密碼驗證失敗、空字串或 null 處理

建立 JwtService 單元測試 - 在 Services 新增 JwtServiceTests.cs，測試 GenerateAccessToken 產生有效 JWT with 正確 claims（NameIdentifier, Name, Role）、GenerateRefreshToken 產生 64-byte Base64 字串、GetPrincipalFromToken 正確解析、過期 token 驗證失敗、無效 token 拋出例外

建立 AuthService 單元測試 - 在 Services 新增 AuthServiceTests.cs，使用 Moq mock DbContext/IJwtService/ILogger，測試 LoginAsync 成功流程與日誌、使用者不存在錯誤、密碼錯誤、RefreshTokenAsync 成功流程、token 過期/撤銷/不存在錯誤、RevokeTokenAsync、CleanupExpiredTokensAsync 與日誌記錄驗證（使用 Mock.Verify 確認 logger 呼叫）

建立 AuthController 單元測試 - 在 Controllers 新增 AuthControllerTests.cs，mock IAuthService，測試所有 endpoints 的成功與失敗場景（login 200/401, refresh 200/401, logout 200/401, cleanup 200/403）、返回正確 HTTP status codes 與 ErrorResponseDto、Authorize attribute 驗證

建立 Auth DTOs Validators 單元測試 - 在 Validators 新增驗證器測試，測試 LoginRequestDto 密碼複雜度規則（太短、無大寫、無小寫、無數字、無特殊字元、合法密碼），Username 長度驗證

更新 OrdersController 單元測試 - 在 OrdersControllerTests.cs 加入授權測試，mock ClaimsPrincipal 與 HttpContext.User 模擬 authenticated user，測試 Admin 查全部訂單、User 只能查自己訂單、User 查他人訂單返回 403、POST 使用 authenticated UserId 覆蓋 DTO

執行所有測試驗證 - 執行 dotnet test 確保所有新舊單元測試通過，使用 Swagger UI 測試完整端到端流程（login with 複雜密碼 → access orders → refresh → logout → cleanup）

建立 JWT 驗證說明文檔 - 在 003-securities-trading-api 新增 jwt-authentication.md，說明架構設計（Users/RefreshToken 資料表、Role enum、SHA256 密碼雜湊）、密碼複雜度要求、API endpoints（/api/auth/login, /api/auth/refresh, /api/auth/logout, /api/auth/cleanup-expired-tokens）、授權規則（Admin vs User 權限）、測試使用者帳號與密碼、Token 過期時間、錯誤碼說明、Swagger 使用方式、日誌記錄說明

計劃已確定完成，可以開始實作！
# JWT 驗證與授權系統文檔

## 目錄
- [概述](#概述)
- [架構設計](#架構設計)
- [密碼安全](#密碼安全)
- [API 端點](#api-端點)
- [測試使用者](#測試使用者)
- [使用 Swagger UI](#使用-swagger-ui)
- [授權規則](#授權規則)
- [Token 生命週期](#token-生命週期)
- [錯誤代碼](#錯誤代碼)
- [日誌記錄](#日誌記錄)

## 概述

本系統實作了完整的 JWT (JSON Web Token) 驗證與授權機制，包含：
- SHA256 + salt 密碼雜湊
- Access Token 與 Refresh Token 機制
- 角色權限控管 (Admin/User)
- Token 撤銷與清理功能
- 密碼複雜度驗證
- 詳細的日誌記錄

## 架構設計

### 資料庫表格

#### Users 表格
```sql
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,  -- 格式：salt:hash
    Role INT NOT NULL,  -- 1=Admin, 2=User
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);
```

#### RefreshTokens 表格
```sql
CREATE TABLE RefreshTokens (
    TokenId BIGINT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Token NVARCHAR(128) NOT NULL UNIQUE,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsRevoked BIT NOT NULL DEFAULT 0,
    RevokedAt DATETIME2 NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
```

### 角色列舉
```csharp
public enum UserRole
{
    Admin = 1,  // 管理員
    User = 2    // 一般使用者
}
```

## 密碼安全

### 密碼雜湊
- 使用 **SHA256** 演算法
- 每個密碼使用**唯一的 32-byte 隨機 salt**
- 儲存格式：`Base64Salt:Base64Hash`
- 使用固定時間比較避免 timing attack

### 密碼複雜度要求
密碼必須符合以下所有條件：
- **長度**：至少 8 個字元
- **大寫字母**：至少一個 (A-Z)
- **小寫字母**：至少一個 (a-z)
- **數字**：至少一個 (0-9)
- **特殊字元**：至少一個 (@$!%*?&)

✅ 有效密碼範例：
- `Admin@123`
- `Test$Pass1`
- `Secure!2024`

❌ 無效密碼範例：
- `test1234` (無大寫、無特殊字元)
- `Test123` (長度不足、無特殊字元)
- `TestPass` (無數字、無特殊字元)

## API 端點

### 1. 登入
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin@123"
}
```

**成功回應 (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64-encoded-random-string",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "username": "admin",
  "role": "Admin"
}
```

**失敗回應 (401 Unauthorized):**
```json
{
  "errorCode": "INVALID_PASSWORD",
  "message": "使用者名稱或密碼錯誤"
}
```

### 2. 更新 Token
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "previous-refresh-token"
}
```

**成功回應 (200 OK):**
返回新的 Access Token 和 Refresh Token（格式同登入回應）

**失敗回應 (401 Unauthorized):**
```json
{
  "errorCode": "TOKEN_INVALID",
  "message": "Refresh Token 無效、已過期或已被撤銷"
}
```

### 3. 登出
```http
POST /api/auth/logout
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "refreshToken": "refresh-token-to-revoke"
}
```

**成功回應 (200 OK):**
```json
{
  "message": "登出成功"
}
```

### 4. 清理過期 Token (僅管理員)
```http
DELETE /api/auth/cleanup-expired-tokens
Authorization: Bearer <admin-access-token>
```

**成功回應 (200 OK):**
```json
{
  "message": "已清理 15 個過期或已撤銷的 Refresh Token",
  "count": 15
}
```

## 測試使用者

系統提供以下測試帳號（執行 `scripts/06_SeedUsers.sql` 後建立）：

| Username | Password | Role | UserId | 說明 |
|----------|----------|------|--------|------|
| admin | Admin@123 | Admin | 1 | 管理員帳號，可存取所有功能 |
| user1 | User1@123 | User | 2 | 一般使用者，僅可存取自己的資料 |
| user2 | User2@123 | User | 3 | 一般使用者，僅可存取自己的資料 |

測試訂單資料（執行 `scripts/07_SeedTestOrders.sql` 後建立）：
- **admin**: 3-5 筆訂單
- **user1**: 5-8 筆訂單
- **user2**: 3-5 筆訂單

## 使用 Swagger UI

### 步驟1：登入取得 Token
1. 開啟 Swagger UI: `http://localhost:5205/swagger`
2. 找到 **POST /api/auth/login** 端點
3. 點擊 "Try it out"
4. 輸入使用者名稱和密碼：
   ```json
   {
     "username": "admin",
     "password": "Admin@123"
   }
   ```
5. 點擊 "Execute"
6. 從回應中複製 `accessToken` 的值

### 步驟2：設定 Bearer Token
1. 點擊 Swagger UI 右上角的 **Authorize** 按鈕（鎖頭圖示）
2. 在 "Value" 欄位中直接貼上 Access Token（**不需要**加上 "Bearer " 前綴）
   ```
   eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
3. 點擊 "Authorize"
4. 點擊 "Close"

### 步驟3：測試需要授權的端點
現在可以測試所有需要授權的端點，例如：
- `GET /api/v1/orders` - 查詢訂單
- `POST /api/v1/orders` - 建立訂單
- `GET /api/v1/orders/{orderId}` - 查詢單筆訂單

## 授權規則

### OrdersController 權限

#### GET /api/v1/orders
- **Admin**: 可查詢所有使用者的訂單
  - 可使用 `?userId=2` 查詢特定使用者訂單
  - 省略 `userId` 查詢所有訂單
- **User**: 只能查詢自己的訂單
  - 系統自動使用登入使用者的 UserId
  - 忽略請求中的 `userId` 參數

#### POST /api/v1/orders
- **所有角色**: 建立訂單時，系統自動使用登入使用者的 UserId
- 請求中的 `userId` 會被忽略並覆蓋

#### GET /api/v1/orders/{orderId}
- **Admin**: 可查詢任何訂單
- **User**: 只能查詢自己的訂單
  - 嘗試查詢他人訂單會返回 403 Forbidden

### AuthController 權限

| 端點 | 權限要求 | 說明 |
|------|----------|------|
| POST /api/auth/login | 無 | 公開端點 |
| POST /api/auth/refresh | 無 | 公開端點 |
| POST /api/auth/logout | Authenticated | 需要有效的 Access Token |
| DELETE /api/auth/cleanup-expired-tokens | Admin | 僅管理員可使用 |

## Token 生命週期

### Access Token
- **有效期限**: 15 分鐘（可在 appsettings.json 中設定）
- **用途**: API 請求驗證
- **Claims 包含**:
  - `nameidentifier`: 使用者 ID
  - `name`: 使用者名稱
  - `role`: "Admin" 或 "User"

### Refresh Token
- **有效期限**: 7 天（可在 appsettings.json 中設定）
- **用途**: 更新 Access Token
- **特性**:
  - 64-byte 隨機字串
  - 一次性使用（refresh 後舊 token 立即撤銷）
  - 可手動撤銷（登出）
  - 過期後自動失效

### Token 更新流程
1. Access Token 過期時，使用 Refresh Token 呼叫 `/api/auth/refresh`
2. 系統驗證 Refresh Token 的有效性
3. 產生新的 Access Token 和 Refresh Token
4. 舊的 Refresh Token 立即被撤銷
5. 返回新的 tokens 給客戶端

## 錯誤代碼

| 錯誤代碼 | 說明 | HTTP 狀態碼 |
|----------|------|-------------|
| USER_NOT_FOUND | 使用者不存在 | 401 |
| INVALID_PASSWORD | 密碼錯誤 | 401 |
| TOKEN_EXPIRED | Access Token 過期 | 401 |
| REFRESH_TOKEN_EXPIRED | Refresh Token 過期 | 401 |
| TOKEN_INVALID | Token 無效或格式錯誤 | 401 |
| TOKEN_REVOKED | Token 已被撤銷 | 401 |
| FORBIDDEN | 權限不足 | 403 |
| VALIDATION_FAILED | 驗證失敗（如密碼格式不符） | 400 |

## 日誌記錄

系統會記錄以下認證相關事件：

### Information Level
- 登入成功: `Login successful. Username={Username}, UserId={UserId}`
- Token 更新成功: `Token refresh successful. UserId={UserId}, Username={Username}`
- Token 撤銷成功: `Token revoked successfully. UserId={UserId}`
- Token 清理: `Cleaned up {Count} expired/revoked refresh tokens`

### Warning Level
- 登入失敗 - 使用者不存在: `Login failed: User not found. Username={Username}`
- 登入失敗 - 密碼錯誤: `Login failed: Invalid password. Username={Username}, UserId={UserId}`
- Token 更新失敗: `Token refresh failed: Token not found/expired/revoked. UserId={UserId}`
- Token 撤銷失敗: `Token revocation failed: Token not found. UserId={UserId}`

### 日誌位置
- **Console**: 開發環境即時輸出
- **File**: `logs/app-YYYYMMDD.log`
- **保留天數**: 30 天

## 安全建議

### 生產環境配置
1. **Secret Key**:
   - 使用環境變數或 Azure Key Vault 儲存
   - 至少 256 bits (32 字元)
   - 定期輪替

2. **HTTPS**:
   - 強制使用 HTTPS
   - 禁用 HTTP 端點

3. **Token 過期時間**:
   - Access Token: 建議 5-15 分鐘
   - Refresh Token: 建議 1-7 天

4. **Rate Limiting**:
   - 已實作，基於 UserId 的流量限制
   - 建議對登入端點額外限制

5. **密碼政策**:
   - 定期要求使用者更改密碼
   - 實作密碼歷史記錄
   - 多次登入失敗後鎖定帳號

## 常見問題

### Q: Token 過期後如何處理？
A: 捕捉 401 錯誤，使用 Refresh Token 呼叫 `/api/auth/refresh` 更新 Access Token。如果 Refresh Token 也過期，需要重新登入。

### Q: 如何實作「記住我」功能？
A: 安全儲存 Refresh Token（如使用 httpOnly cookie 或安全的本地儲存），並在 Access Token 過期時自動更新。

### Q: Admin 可以查看所有使用者的訂單嗎？
A: 是的，Admin 角色可以使用 `GET /api/v1/orders?userId=2` 查詢特定使用者的訂單，或省略 userId 查詢所有訂單。

### Q: 使用者可以更改自己的 Role 嗎？
A: 不行，Role 是在資料庫中設定的，API 不提供更改 Role 的端點。

### Q: 如何撤銷所有使用者的 Token？
A: 作為管理員，可以直接在資料庫中刪除 RefreshTokens 表格的記錄，或執行 `DELETE FROM RefreshTokens`。

## 範例程式碼

### C# 客戶端範例
```csharp
// 登入
var loginRequest = new { username = "admin", password = "Admin@123" };
var response = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

// 設定 Authorization Header
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

// 查詢訂單
var orders = await httpClient.GetFromJsonAsync<List<OrderDto>>("/api/v1/orders");

// Token 更新
var refreshRequest = new { refreshToken = loginResponse.RefreshToken };
var refreshResponse = await httpClient.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
```

### JavaScript/Fetch 範例
```javascript
// 登入
const loginResponse = await fetch('/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ username: 'admin', password: 'Admin@123' })
});
const { accessToken, refreshToken } = await loginResponse.json();

// 查詢訂單
const ordersResponse = await fetch('/api/v1/orders', {
  headers: { 'Authorization': `Bearer ${accessToken}` }
});
const orders = await ordersResponse.json();

// Token 更新
const refreshResponse = await fetch('/api/auth/refresh', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ refreshToken })
});
```

## 相關檔案

- **實體模型**: 
  - `Models/Entities/User.cs`
  - `Models/Entities/RefreshToken.cs`
  - `Models/Entities/UserRole.cs`
- **DTOs**: 
  - `Models/Dtos/LoginRequestDto.cs`
  - `Models/Dtos/LoginResponseDto.cs`
  - `Models/Dtos/RefreshTokenRequestDto.cs`
  - `Models/Dtos/ErrorResponseDto.cs`
- **服務**: 
  - `Services/IJwtService.cs` & `Services/JwtService.cs`
  - `Services/IAuthService.cs` & `Services/AuthService.cs`
- **控制器**: 
  - `Controllers/AuthController.cs`
  - `Controllers/OrdersController.cs`
- **基礎設施**: 
  - `Infrastructure/PasswordHasher.cs`
  - `Infrastructure/Validators/LoginRequestValidator.cs`
- **資料庫腳本**: 
  - `scripts/06_SeedUsers.sql`
  - `scripts/07_SeedTestOrders.sql`

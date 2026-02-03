# Securities Trading API

證券交易資料查詢系統 - 台灣股票資訊查詢和下單 API

## 專案概述

這是一個基於 .NET 8.0 的企業級證券交易 API 系統，支援台灣股票市場的即時報價查詢與委託單管理。系統採用 In-Memory OLTP 技術提供高效能資料處理，並整合 JWT 身份驗證、角色授權、限流保護等企業級安全機制。

**實作狀態**: ✅ **100% COMPLETE** - 所有核心功能已完成並通過測試

## 核心功能

### 🔐 認證與授權
- **JWT 驗證** - Access Token (15 分鐘) + Refresh Token (7 天)
- **角色權限** - Admin / User 雙層級授權
- **密碼安全** - SHA256 + Salt 加密，密碼複雜度驗證
- **Token 管理** - 登入、登出、Token 刷新、自動清理過期 Token

### 📊 股票查詢
- **User Story 1**: 查詢股票基本資料 (GET /api/stocks/{stockCode})
- **User Story 2**: 查詢即時報價 (GET /api/stocks/{stockCode}/quote)
- **台證所整合** - 即時串接台灣證交所 API
- **智能快取** - 5 秒記憶體快取 + 自動重試機制

### 📝 委託單管理
- **User Story 3**: 建立委託單 (POST /api/orders)
- **User Story 4**: 查詢委託單 (GET /api/orders, GET /api/orders/{orderId})
- **權限控管** - Admin 可查所有單，User 僅限自己的委託單
- **CQRS 模式** - 讀寫分離，優化查詢效能

## 技術棧

### 核心框架
- **.NET 8.0** - 最新 LTS 版本
- **ASP.NET Core Web API** - RESTful API 架構
- **Entity Framework Core 8.0.11** - ORM 框架

### 資料庫
- **SQL Server 2019+** (需 Developer/Enterprise Edition)
- **In-Memory OLTP** - 高效能記憶體內交易處理
- **CQRS 模式** - Orders_Write (寫入) / Orders_Read (讀取) 分離

### 安全性
- **JWT Bearer Authentication** - 無狀態身份驗證
- **Role-Based Authorization** - 角色權限控管
- **Password Hashing** - SHA256 + Random Salt

### 驗證與日誌
- **FluentValidation 11.10.0** - 優雅的輸入驗證
- **Serilog 8.0.3** - 結構化日誌記錄
- **File + Console Logging** - 多目標日誌輸出

### 效能優化
- **Memory Cache** - 5 秒報價快取
- **Rate Limiting** - 滑動視窗限流 (10 req/s)
- **Connection Pooling** - 資料庫連線池
- **Retry Policy** - 外部 API 自動重試 (指數退避)

## 快速開始

### 前置需求

- .NET 8 SDK (9.0.305 或更高)
- SQL Server 2019+ (Developer 或 Enterprise Edition - In-Memory OLTP 需求)
- Visual Studio 2022 或 VS Code

### 資料庫設定

1. **建立資料庫和 In-Memory OLTP 檔案群組**:

```powershell
sqlcmd -S localhost -E -i scripts\01_CreateDatabase.sql
```

請根據您的 SQL Server 安裝路徑調整腳本中的檔案路徑。

2. **套用 EF Core 遷移**:

```powershell
cd src\SecuritiesTradingApi
dotnet ef database update
```

3. **載入種子資料與測試使用者**:

```powershell
# 股票主檔資料（10檔台股）
sqlcmd -S localhost -E -d TradingSystemDB_Dev -i scripts\02_SeedData.sql

# 測試使用者（admin, user1, user2）
sqlcmd -S localhost -E -d TradingSystemDB_Dev -i scripts\06_SeedUsers.sql

# 測試委託單資料
sqlcmd -S localhost -E -d TradingSystemDB_Dev -i scripts\07_SeedTestOrders.sql
```

**測試帳號**:
- `admin` / `Admin@123` (管理員)
- `user1` / `User1@123` (一般使用者)
- `user2` / `User2@123` (一般使用者)

### 執行應用程式

```ba1. 登入取得 JWT Token

```powershell
curl -X POST https://localhost:7001/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{\"username\":\"user1\",\"password\":\"User1@123\"}'
```

**回應範例**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123...",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "username": "user1",
  "role": "User"
}
```

#### 2. 查詢股票基本資料（無需驗證）

```powershell
curl https://localhost:7001/api/stocks/2330
```

#### 3. 查詢即時報價（無需驗證）

```powershell
curl https://localhost:7001/api/stocks/2330/quote
```

#### 4. 建立委託單（需要驗證）

```powershell
$token = "your_access_token_here"
curl -X POST https://localhost:7001/api/v1/orders `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer $token" `
  -d '{\"stockCode\":\"2330\",\"orderType\":1,\"price\":580.00,\"quantity\":1000}'
```

**注意**: 建立委託單會自動使用登入使用者的 UserId，無需在 body 提供。

#### 5. 查詢委託單列表（需要驗證）
SystemDB_Dev;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-at-least-256-bits-long",
    "Issuer": "SecuritiesTradingApi",
    "Audience": "SecuritiesTradingApiUsers",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
```powershell
# 一般使用者：查詢自己的委託單
curl https://localhost:7001/api/v1/orders `
  -H "Authorization: Bearer $token"

# 管理員：查詢特定使用者的委託單
curl https://localhost:7001/api/v1/orders?userId=2 `
  -H "Authorization: Bearer $adminToken"
```
 存放敏感資訊:

```powershell
cd src\SecuritiesTradingApi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:TradingDb" "Your_Connection_String"
dotnet user-secrets set "JwtSettings:SecretKey" "Your_256_Bit_Secret_Key_Here"
```

**安全提醒**: 
- `JwtSettings:SecretKey` 至少需要 256 bits (32 bytes)
- 生產環境務必使用環境變數或 Azure Key Vault 等安全儲存方式H "Authorization: Bearer $token"
```

#### 7. 刷新 Token

```powershell
curl -X POST https://localhost:7001/api/auth/refresh `
  -H "Content-Type: application/json" `
  -d '{\"refreshToken\":\"your_refresh_token_here\"}'
```

#### 8. 登出（撤銷 Token）

```powershell
curl -X POST https://localhost:7001/api/auth/logout `
  -H "Authorization: Bearer $token"
#### 建立委託單

```bash
curl -X POST https://localhost:7001/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "stockCode": "2330",
    "orderType": 1,
    "price": 580.00,
    "quantity": 1000
  }'
```

## 配置說明

### appsettings.json

```json
{
  "ConnectionStrings": {
    "TradingDb": "Server=localhost;Database=TradingDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "TwseApi": {
    "BaseUrl": "https://mis.twse.com.tw",
    "TimeoutSeconds": 2,
    "MaxRetries": 2,
    "CacheSeconds": 5
  },
  "RateLimiting": {
    "PermitLimit": 10,
    "WindowSeconds": 1
  }
}
```

### 環境變數, `RefreshTokens` (In-Memory OLTP)
  - 極高寫入效能
  - 低延遲讀取
  - 無鎖定並行處理
- **Warm Layer**: `Orders_Read` (傳統資料表 + 索引)
  - 優化查詢效能
  - 包含預先 JOIN 的欄位（StockName, Username）
- **Cold Layer**: `StockMaster`, `Users` (主資料)
  - 低異動頻率
  - 完整資料欄位

### CQRS 模式 (Command Query Responsibility Segregation)

**Orders 資料流**:
```
[Client] 
   ↓ POST /api/v1/orders
[OrdersController] 
   ↓ CreateOrderAsync()
[OrderService] 
   → Orders_Write (In-Memory OLTP) → 極速寫入
   → Orders_Read (Traditional) → 查詢優化

   ↓ GET /api/v1/orders
[OrderService]
   → Orders_Read (Traditional) ← 讀取專用，JOIN StockName, Username
```

**優勢**:
### Swagger UI

啟動專案後訪問: https://localhost:7001/swagger

**使用 JWT 驗證**:
1. 點擊右上角 🔓 **Authorize** 按鈕
2. 輸入格式: `Bearer {your_access_token}`
3. 點擊 **Authorize** 確認
4. 現在可以測試需要驗證的 API 端點

### API 端點總覽

#### 所有測試

```powershell
# 從根目錄執行所有測試
dotnet test

# 或個別執行
cd tests\SecuritiesTradingApi.UnitTests
dotnet test --verbosity normal

cd tests\SecuritiesTradingApi.IntegrationTests
dotnet test --verbosity normal
```

### 測試覆蓋率

- ✅ **26+ 單元測試** 通過
- ✅ **整合測試** 完成
- 測試範圍:
  - ✅ Controllers (AuthController, OrdersController, StocksController)
  - ✅ Services (AuthService, JwtService, OrderService, StockService)
  - ✅ Validators (LoginRequestValidator, RefreshTokenRequestValidator, CreateOrderValidator)
  - ✅ Infrastructure (PasswordHasher, TwseApiClient)

### 壓力測試 (k6)

使用 k6 進行負載測試:

```powershell
# 安裝 k6
choco install k6

# 執行壓力測試
cd k6-tests
k6 run stress-test.js
```

測試場景請參考 [`k6-tests/README.md`](k6-tests/README.md)ET | `/api/stocks/{stockCode}` | 查詢股票基本資料 | ❌ |
| GET | `/api/stocks/{stockCode}/quote` | 查詢即時報價 | ❌ |

#### 📝 委託單 API
| 方法 | 端點 | 說明 | 驗證 |
|------|------|------|------|
| GET | `/api/v1/orders` | 查詢委託單列表 | ✅ |
| POST | `/api/v1/orders` | 建立委託單 | ✅ |
| GET | `/api/v1/orders/{orderId}` | 查詢單筆委託單 | ✅ |

### OpenAPI 規格

完整 API 規格文件: [`specs/003-securities-trading-api/contracts/openapi.yaml`](specs/003-securities-trading-api/contracts/openapi.yaml)

### 身份驗證流程

```
[Client] → POST /api/auth/login (username, password)
   ↓
[AuthController] → [AuthService]
   ↓ 1. 驗證密碼 (PasswordHasher.VerifyPassword)
   ↓ 2. 產生 Access Token (JwtService, 15min)
   ↓ 3. 產生 Refresh Token (64-byte random, 7days)
   ↓ 4. 儲存 Refresh Token → RefreshTokens (In-Memory OLTP)
   ↓
[Client] ← 200 OK { accessToken, refreshToken, role, ... }

[Client] → GET /api/v1/orders (Authorization: Bearer {token})
   ↓
[JwtBearerMiddleware] 
   ↓ 1. 驗證 Token 簽章
   ↓ 2. 檢查過期時間
   ↓ 3. 設定 User Claims (NameIdentifier, Role)
   ↓
[OrdersController] [Authorize]
   ↓ 4. 檢查授權（User 只能查自己的單）
   ↓
[OrderService] → Orders_Read
```

### 快取策略

- **TWSE API 快取**: 5 秒記憶體快取 (IMemoryCache)
  - 快取鍵: `StockQuote_{stockCode}`
  - 裝飾器模式: `CachedTwseApiClient` 包裝 `TwseApiClient`
  - 降低外部 API 呼叫次數
- **Database Snapshot**: `StockQuotesSnapshot` (In-Memory OLTP)
  - 每次 API 呼叫更新
  - 可作為備援資料源
效能指標

### 回應時間
- **股票查詢**: < 50ms (資料庫查詢)
- **即時報價**: < 100ms (含快取) / < 500ms (TWSE API)
- **建立委託單**: < 20ms (In-Memory OLTP 寫入)
- **查詢委託單**: < 50ms (Warm Layer 查詢)

### 吞吐量
- **限流保護**: 10 req/s per user (生產環境)
- **In-Memory OLTP**: 支援百萬級 TPS (Transactions Per Second)
- **Connection Pooling**: 自動管理連線池

### 可用性
- **自動重試**: TWSE API 失敗自動重試 2 次
- **快取降級**: 外部 API 失敗時使用快取資料
- **結構化日誌**: 完整追蹤請求流程

## 疑難排解

### 🔴 SQL Server 不支援 In-Memory OLTP

**錯誤**: 
```
Database 'TradingSystemDB_Dev' cannot be started in this edition of SQL Server 
because it contains a MEMORY_OPTIMIZED_DATA filegroup.
```
專案文件

- 📋 [實作狀態報告](IMPLEMENTATION_STATUS.md) - 完整開發進度與任務清單
- 🔐 [JWT 實作完成報告](JWT_IMPLEMENTATION_COMPLETION_REPORT.md) - 認證系統詳細說明
- 📝 [資料庫結構](DATABASE_STRUCTURE.md) - 資料表設計與關聯
- 🚀 [快速啟動指南](tools/QUICKSTART.md) - 5 分鐘快速上手
- 📊 [壓力測試報告](k6-tests/stress-test-report.md) - k6 負載測試結果
- 📐 [OpenAPI 規格](specs/003-securities-trading-api/contracts/openapi.yaml) - 完整 API 規格

## 開發狀態

✅ **100% COMPLETE** - 所有核心功能已實作完成

**已完成**:
- ✅ Phase 1: Setup (7/7 tasks)
- ✅ Phase 2: Foundational Infrastructure (16/16 tasks)
- ✅ Phase 3: User Story 1 - Stock Query (17/17 tasks)
- ✅ Phase 4: User Story 2 - Stock Quote (13/13 tasks)
- ✅ Phase 5: User Story 3 - Create Order (21/21 tasks)
- ✅ Phase 6: User Story 4 - Query Order (6/6 tasks)
- ✅ Phase 7: Polish & Cross-Cutting (11/11 tasks)
- ✅ JWT Authentication & Authorization (25/25 tasks)

**測試狀態**: 
- ✅ 26+ 單元測試通過
- ✅ 整合測試完成
- ✅ k6 壓力測試完成

詳細進度請參考 [`IMPLEMENTATION_STATUS.md`](IMPLEMENTATION_STATUS.md)

## 後續計畫

- [ ] WebSocket 即時推播 (委託單狀態更新)
- [ ] Redis 分散式快取
- [ ] 訂單撮合引擎
- [ ] 歷史交易記錄查詢
- [ ] 使用者資產管理
- [ ] Docker 容器化部署
- [ ] Kubernetes 編排
- [ ] CI/CD Pipeline (GitHub Actions)

## 貢獻指南

歡迎提交 Issue 或 Pull Request！

**開發流程**:
1. Fork 本專案
2. 建立功能分支: `git checkout -b feature/amazing-feature`
3. 提交變更: `git commit -m 'Add amazing feature'`
4. 推送分支: `git push origin feature/amazing-feature`
5. 建立 Pull Request

**程式碼風格**:
- 遵循 C# Coding Conventions
- 使用 `dotnet format` 格式化程式碼
- 新增單元測試涵蓋新功能

## 授權

MIT License

Copyright (c) 2026 Securities Trading API

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

**Made with ❤️ by Securities Trading API Team**錯誤

**錯誤**: 
```
A connection was successfully established with the server, but then an error 
occurred during the login process.
```

**解決方案**: 
1. 加上 `TrustServerCertificate=True` 到連線字串:
   ```json
   "Server=localhost;Database=TradingSystemDB_Dev;Trusted_Connection=True;TrustServerCertificate=True"
   ```
2. 或者配置有效的 SSL 憑證

### 🔴 JWT 驗證失敗

**錯誤**: `401 Unauthorized` 或 `Token validation failed`

**檢查項目**:
1. ✅ Access Token 是否過期？(預設 15 分鐘)
2. ✅ Authorization Header 格式: `Bearer {token}`
3. ✅ `JwtSettings:SecretKey` 是否與簽發 Token 時相同？
4. ✅ Token 簽發者 (Issuer) 與驗證者是否一致？

**解決方案**:
```powershell
# 使用 Refresh Token 更新 Access Token
curl -X POST https://localhost:7001/api/auth/refresh `
  -H "Content-Type: application/json" `
  -d '{\"refreshToken\":\"your_refresh_token_here\"}'
```

### 🟡 TWSE API 503 錯誤

**錯誤**: `503 Service Unavailable` from TWSE API

**原因**: 
- Taiwan Stock Exchange API 暫時無法使用
- 達到 TWSE 的限流閾值

**系統行為**: 
- ✅ 自動重試 2 次 (1秒、2秒指數退避)
- ✅ 超過重試次數回傳 503 給客戶端
- ✅ 使用 `StockQuotesSnapshot` 快取資料作為備援

**客戶端建議**: 
- 實作重試邏輯 (exponential backoff)
- 非交易時段可能無法取得報價

### 🟡 Rate Limiting 429 錯誤

**錯誤**: `429 Too Many Requests`

**原因**: 超過限流閾值 (10 req/s per user)

**解決方案**:
1. 降低請求頻率
2. 實作客戶端節流 (throttling)
3. 開發環境可調整 `appsettings.Development.json`:
   ```json
   "RateLimiting": {
     "PermitLimit": 100,
     "WindowSeconds": 1
   }
   ```

### 🔵 Entity Framework Migration 問題

**錯誤**: `A connection was successfully established...` during migration

**解決方案**:
```powershell
# 確認資料庫已建立
sqlcmd -S localhost -E -Q "SELECT name FROM sys.databases WHERE name = 'TradingSystemDB_Dev'"

# 重新執行 Migration
cd src\SecuritiesTradingApi
dotnet ef database drop --force
dotnet ef database update

# 重新載入種子資料
sqlcmd -S localhost -E -d TradingSystemDB_Dev -i ..\..\scripts\02_SeedData.sql
sqlcmd -S localhost -E -d TradingSystemDB_Dev -i ..\..\scripts\06_SeedUsers.sql
sqlcmd -S localhost -E -d TradingSystemDB_Dev -i ..\..\scripts\07_SeedTestOrders.sql
```優化查詢 (包含 JOIN 後的欄位)

### 快取策略

- TWSE API 回應快取 5 秒 (In-Memory Cache)
- 使用裝飾器模式 (`CachedTwseApiClient`)

### 限流保護

- 預設: 10 requests/second (滑動視窗)
- 超過限制回傳 429 Too Many Requests

## API 文件

詳細 API 規格請參考：
- OpenAPI Spec: `specs/003-securities-trading-api/contracts/openapi.yaml`
- Swagger UI: https://localhost:7001/swagger

## 測試

### 執行單元測試

```bash
cd tests\SecuritiesTradingApi.UnitTests
dotnet test
```

### 執行整合測試

```bash
cd tests\SecuritiesTradingApi.IntegrationTests
dotnet test
```

## 疑難排解

### SQL Server 不支援 In-Memory OLTP

**錯誤**: "Database 'TradingDb' cannot be started in this edition of SQL Server..."

**解決方案**: 
- 使用 SQL Server 2019 Developer Edition (免費) 或 Enterprise Edition
- SQL Server Express **不支援** In-Memory OLTP

### 連線字串錯誤

**錯誤**: "A connection was successfully established with the server, but then an error occurred..."

**解決方案**: 
- 加上 `TrustServerCertificate=True` 到連線字串
- 或者配置有效的 SSL 憑證

### TWSE API 503 錯誤

**原因**: Taiwan Stock Exchange API 暫時無法使用或限流

**解決方案**: 
- API 會自動重試 2 次 (1秒、2秒延遲)
- 超過重試次數會回傳 503 給客戶端
- 正常現象，客戶端應實作重試邏輯

## 開發狀態

請參考 `IMPLEMENTATION_STATUS.md` 了解目前實作進度。

## 授權

MIT License

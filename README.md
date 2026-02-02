# Securities Trading API

證券交易資料查詢系統 - 台灣股票資訊查詢和下單 API

## 功能特色

- **User Story 1**: 查詢股票基本資料 (GET /api/stocks/{stockCode})
- **User Story 2**: 查詢即時報價 (GET /api/stocks/{stockCode}/quote)
- **User Story 3**: 建立委託單 (POST /api/orders)

## 技術棧

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core 8.0.11 with SQL Server
- FluentValidation 11.10.0
- Serilog 8.0.3
- In-Memory OLTP (高效能熱資料層)
- CQRS 模式 (Orders_Write/Orders_Read)

## 快速開始

### 前置需求

- .NET 8 SDK (9.0.305 或更高)
- SQL Server 2019+ (Developer 或 Enterprise Edition - In-Memory OLTP 需求)
- Visual Studio 2022 或 VS Code

### 資料庫設定

1. **建立資料庫和 In-Memory OLTP 檔案群組**:

```bash
sqlcmd -S localhost -E -i scripts\01_CreateDatabase.sql
```

請根據您的 SQL Server 安裝路徑調整腳本中的檔案路徑。

2. **套用 EF Core 遷移**:

```bash
cd src\SecuritiesTradingApi
dotnet ef database update
```

3. **載入種子資料**:

```bash
sqlcmd -S localhost -E -d TradingDb -i scripts\02_SeedData.sql
```

### 執行應用程式

```bash
cd src\SecuritiesTradingApi
dotnet run
```

API 將在以下位址啟動：
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5000
- Swagger UI: https://localhost:7001/swagger

### 測試 API

#### 查詢股票基本資料

```bash
curl https://localhost:7001/api/stocks/2330
```

#### 查詢即時報價

```bash
curl https://localhost:7001/api/stocks/2330/quote
```

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

### 環境變數

開發環境建議使用 User Secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:TradingDb" "Your_Connection_String"
```

## 架構設計

### 資料分層 (Hot/Warm/Cold)

- **Hot Layer**: `Orders_Write`, `StockQuotesSnapshot` (In-Memory OLTP)
- **Warm Layer**: `Orders_Read` (傳統資料表，優化查詢)
- **Cold Layer**: `StockMaster` (主資料)

### CQRS 模式

Orders 使用 CQRS 分離寫入和讀取：
- `Orders_Write`: 快速寫入 (In-Memory OLTP)
- `Orders_Read`: 優化查詢 (包含 JOIN 後的欄位)

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

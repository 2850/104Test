# Quickstart: 證券交易資料查詢系統

**Feature**: 003-securities-trading-api  
**日期**: 2026-02-02

## 目錄

1. [系統需求](#系統需求)
2. [環境設定](#環境設定)
3. [資料庫設定](#資料庫設定)
4. [專案建立與執行](#專案建立與執行)
5. [測試執行](#測試執行)
6. [API 測試](#api-測試)
7. [效能測試](#效能測試)
8. [常見問題](#常見問題)

---

## 系統需求

### 必要條件

- **.NET 8 SDK**: [下載連結](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Microsoft SQL Server 2019+**: 支援 In-Memory OLTP
  - Enterprise Edition 或 Developer Edition（免費）
  - SQL Server Express **不支援** In-Memory OLTP
- **Git**: 版本控制
- **Visual Studio 2022** 或 **VS Code** (推薦安裝 C# Dev Kit)

### 建議工具

- **SQL Server Management Studio (SSMS)**: 資料庫管理
- **Postman** 或 **Swagger UI**: API 測試
- **k6**: 效能測試工具 [安裝說明](https://k6.io/docs/getting-started/installation/)

### 硬體需求

- **記憶體**: 最少 8GB RAM（建議 16GB，In-Memory OLTP 需要）
- **磁碟空間**: 5GB 以上
- **處理器**: 雙核心以上

---

## 環境設定

### 1. 安裝 .NET 8 SDK

```powershell
# 驗證安裝
dotnet --version
# 預期輸出: 8.0.x
```

### 2. 安裝 SQL Server 2019 Developer Edition

**下載**: [SQL Server 2019 Developer Edition](https://www.microsoft.com/zh-tw/sql-server/sql-server-downloads)

**安裝時注意事項**:
- 選擇 **自訂安裝**
- 確認勾選 **Database Engine Services**
- 啟用 **Mixed Mode Authentication**（SQL Server + Windows）
- 記下 `sa` 帳號密碼

### 3. 驗證 In-Memory OLTP 支援

```sql
-- 在 SSMS 執行
SELECT SERVERPROPERTY('IsXTPSupported') AS InMemorySupported;
-- 預期輸出: 1 (支援)，0 (不支援)
```

如果輸出為 0，請確認您使用的是 Enterprise 或 Developer Edition。

### 4. 克隆專案

```powershell
git clone https://github.com/your-org/Stock_2330.git
cd Stock_2330
git checkout 003-securities-trading-api
```

---

## 資料庫設定

### 1. 建立資料庫

```sql
-- 在 SSMS 新查詢視窗執行
CREATE DATABASE TradingSystemDB
ON PRIMARY 
(
    NAME = TradingSystemDB_data,
    FILENAME = 'C:\SQLData\TradingSystemDB_data.mdf',
    SIZE = 500MB,
    FILEGROWTH = 100MB
),
FILEGROUP TradingSystemDB_mod_fg CONTAINS MEMORY_OPTIMIZED_DATA
(
    NAME = TradingSystemDB_mod,
    FILENAME = 'C:\SQLData\TradingSystemDB_mod'
)
LOG ON
(
    NAME = TradingSystemDB_log,
    FILENAME = 'C:\SQLData\TradingSystemDB_log.ldf',
    SIZE = 100MB,
    FILEGROWTH = 50MB
);
GO

ALTER DATABASE TradingSystemDB 
SET MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT = ON;
GO
```

**注意**: 請根據您的環境修改 `FILENAME` 路徑。

### 2. 執行 Migration

```powershell
cd SecuritiesTradingApi/src/SecuritiesTradingApi

# 更新資料庫連線字串
# 編輯 appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TradingSystemDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}

# 執行 Migration
dotnet ef database update
```

### 3. 匯入股票主檔資料

```powershell
# 執行資料初始化程式
dotnet run --project src/SecuritiesTradingApi -- seed-stocks

# 或手動執行 SQL Script
# 匯入 database/seed-data/stocks.csv (從 t187ap03_L.csv 轉換)
```

### 4. 驗證資料

```sql
-- 檢查股票主檔筆數
SELECT COUNT(*) FROM StockMaster;
-- 預期輸出: ~2000 筆（台灣上市櫃股票）

-- 檢查特定股票
SELECT * FROM StockMaster WHERE StockCode = '2330';
-- 預期輸出: 台積電資料
```

---

## 專案建立與執行

### 1. 還原套件

```powershell
cd SecuritiesTradingApi
dotnet restore
```

### 2. 建置專案

```powershell
dotnet build
```

### 3. 執行專案

```powershell
cd src/SecuritiesTradingApi
dotnet run
```

預期輸出:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### 4. 開啟 Swagger UI

瀏覽器開啟: http://localhost:5000/swagger

---

## 測試執行

### 單元測試

```powershell
# 執行所有測試
cd tests/SecuritiesTradingApi.UnitTests
dotnet test

# 執行測試並產生覆蓋率報告
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# 檢視覆蓋率
# 目標: 100% 覆蓋率
```

### 整合測試

```powershell
cd tests/SecuritiesTradingApi.IntegrationTests
dotnet test
```

---

## API 測試

### 使用 Swagger UI

1. 開啟 http://localhost:5000/swagger
2. 展開任一 API 端點
3. 點擊 **Try it out**
4. 輸入參數
5. 點擊 **Execute**

### 使用 cURL

#### 1. 查詢股票資訊

```bash
curl -X GET "http://localhost:5000/api/stocks/2330" -H "accept: application/json"
```

預期回應:
```json
{
  "stockCode": "2330",
  "stockName": "台灣積體電路製造股份有限公司",
  "stockNameShort": "台積電",
  "exchange": "TWSE",
  "industry": "半導體"
}
```

#### 2. 查詢股票即時報價

```bash
curl -X GET "http://localhost:5000/api/stocks/2330/quote" -H "accept: application/json"
```

預期回應:
```json
{
  "stockCode": "2330",
  "stockName": "台積電",
  "currentPrice": 975.00,
  "yesterdayPrice": 950.00,
  "openPrice": 955.00,
  "highPrice": 980.00,
  "lowPrice": 945.00,
  "limitUpPrice": 1045.00,
  "limitDownPrice": 855.00,
  "changeAmount": 25.00,
  "changePercent": 2.63,
  "totalVolume": 52342000,
  "totalValue": 50832615000.00,
  "updateTime": "2026-02-02T13:30:00Z"
}
```

#### 3. 建立委託單

```bash
curl -X POST "http://localhost:5000/api/orders" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "stockCode": "2330",
    "orderType": 1,
    "price": 975.00,
    "quantity": 1000
  }'
```

預期回應:
```json
{
  "orderId": 123456789,
  "orderSeq": 20260202000001,
  "message": "委託單建立成功"
}
```

#### 4. 查詢委託單

```bash
curl -X GET "http://localhost:5000/api/orders/123456789" -H "accept: application/json"
```

預期回應:
```json
{
  "orderId": 123456789,
  "orderSeq": 20260202000001,
  "userId": 1,
  "stockCode": "2330",
  "stockName": "台積電",
  "orderTypeName": "買進",
  "price": 975.00,
  "quantity": 1000,
  "filledQuantity": 0,
  "orderStatusName": "已委託",
  "createdAt": "2026-02-02T09:00:00Z"
}
```

### 使用 Postman

匯入 Postman Collection: `docs/postman/SecuritiesTradingApi.postman_collection.json`

---

## 效能測試

### 安裝 k6

**Windows (Chocolatey)**:
```powershell
choco install k6
```

**macOS (Homebrew)**:
```bash
brew install k6
```

**Linux**:
```bash
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

### 執行負載測試

```powershell
cd k6-tests

# 負載測試（模擬 100 concurrent users, 10 分鐘）
k6 run --out json=load-test-results.json load-test.js
```

### 執行壓力測試

```powershell
# 壓力測試（尋找系統崩潰點）
k6 run --out json=stress-test-results.json stress-test.js
```

### 檢視測試報告

```powershell
# 安裝 k6-reporter (需要 Node.js)
npm install -g k6-to-html

# 產生 HTML 報告
k6-to-html load-test-results.json --output load-test-report.html

# 開啟報告
start load-test-report.html
```

---

## 常見問題

### Q1: Migration 執行失敗 "In-Memory OLTP is not supported"

**原因**: SQL Server 版本不支援 In-Memory OLTP

**解決方案**:
1. 確認使用 SQL Server 2019+ Enterprise 或 Developer Edition
2. SQL Server Express **不支援** In-Memory OLTP
3. 執行 `SELECT SERVERPROPERTY('IsXTPSupported')` 驗證

---

### Q2: API 回傳 503 "無法取得即時資料"

**原因**: 台灣證交所 API 呼叫失敗或逾時

**解決方案**:
1. 檢查網路連線
2. 確認可直接存取 https://mis.twse.com.tw/stock/api/getStockInfo.jsp?ex_ch=tse_2330.tw
3. 檢查 `appsettings.json` 的 `TwseApi.BaseUrl`
4. 查看 Log 檔案: `logs/app.log`
5. 檢查是否在交易時段（平日 09:00-13:30）

---

### Q3: 委託單建立失敗 "委託價格超出漲跌停範圍"

**原因**: 委託價格超過當日漲跌停限制

**解決方案**:
1. 先呼叫 `GET /api/stocks/{stockCode}/quote` 取得漲跌停價格
2. 確認委託價格在 `limitDownPrice` ~ `limitUpPrice` 之間
3. 檢查台灣證交所 API 回傳資料是否正確
4. 確認在交易時段，非交易時段可能無最新資料

---

### Q4: 測試覆蓋率不足 100%

**原因**: 部分程式碼未被測試

**解決方案**:
1. 執行 `dotnet test /p:CollectCoverage=true` 產生覆蓋率報告
2. 查看 `coverage.opencover.xml`
3. 針對未覆蓋的程式碼補充測試案例
4. 確認所有 Service、Controller、Validator 都有對應測試

---

### Q5: 速率限制觸發 HTTP 429

**原因**: 請求次數超過每秒 10 次限制

**解決方案**:
1. 檢查 `Retry-After` Header 取得重試等待時間
2. 實作 Client 端 Backoff 機制
3. 開發環境可暫時調整 `appsettings.Development.json` 的 `RateLimiting.MaxRequestsPerSecond`

---

### Q6: k6 測試失敗 "connection refused"

**原因**: API 服務未啟動或連線錯誤

**解決方案**:
1. 確認 API 服務執行中: `dotnet run`
2. 檢查 k6 腳本的 `BASE_URL` 是否正確
3. 確認防火牆未阻擋 5000 port

---

## 下一步

- **實作指南**: 參閱 [tasks.md](./tasks.md)（由 `/speckit.tasks` 指令產生）
- **API 文件**: 參閱 [contracts/openapi.yaml](./contracts/openapi.yaml)
- **資料模型**: 參閱 [data-model.md](./data-model.md)
- **技術研究**: 參閱 [research.md](./research.md)

---

## 支援

如有問題，請提交 Issue 至專案 GitHub Repository。

---

**Quickstart Complete** ✅

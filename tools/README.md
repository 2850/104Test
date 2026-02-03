# StockMaster CSV 匯入工具

此工具用於將台灣證券交易所的股票資料 CSV 檔案（t187ap03_L.csv）匯入到 StockMaster 資料表。

## 功能特色

- ✅ 自動解析 CSV 檔案（支援 Big5 編碼）
- ✅ 批次處理提升效能（每批 100 筆）
- ✅ 支援新增和更新模式
- ✅ 自動轉換民國年為西元年
- ✅ 完整的錯誤處理和統計報告

## 欄位對應

| CSV 欄位 | 資料表欄位 | 說明 |
|---------|-----------|------|
| 公司代號 | StockCode | 股票代碼（主鍵）|
| 公司名稱 | StockName, StockNameShort | 公司全名和簡稱 |
| 公司簡稱 | StockNameEn | 英文名稱 |
| 產業別 | Industry | 產業類別代碼 |
| 出表日期 | ListedDate | 上市日期（民國年自動轉換）|
| - | IsActive | 固定為 true |
| - | Exchange | 固定為 "TWSE" |
| - | CreatedAt | 建立時間（自動）|
| - | UpdatedAt | 更新時間（自動）|

## 使用方式

### 方法 1：使用 PowerShell 腳本（推薦）

```powershell
# 使用預設路徑的 CSV 檔案
.\tools\ImportStockMaster.ps1

# 指定 CSV 檔案路徑
.\tools\ImportStockMaster.ps1 "D:\path\to\t187ap03_L.csv"
```

### 方法 2：直接執行程式

```powershell
cd tools/StockMasterImporter
dotnet run
```

### 方法 3：編譯後執行

```powershell
cd tools/StockMasterImporter
dotnet build -c Release
.\bin\Release\net9.0\StockMasterImporter.exe
```

## 執行流程

1. 檢查資料庫連線
2. 讀取 CSV 檔案（自動偵測編碼）
3. 解析欄位對應
4. 批次處理資料：
   - 檢查股票代碼是否已存在
   - 存在：更新資料（可選）
   - 不存在：新增資料
5. 顯示統計報告

## 執行結果範例

```
===== StockMaster CSV 匯入工具 =====

✓ 資料庫連線成功

CSV 檔案: D:\Web\Stock_2330\.github\prompts\CreateSystem\t187ap03_L.csv

如果股票代碼已存在，是否要更新資料？(Y/N，預設: Y): Y

開始匯入... (更新模式: 是)

─────────────────────────────────────
開始讀取 CSV 檔案: ...
找到 1077 筆資料準備匯入
已處理 100/1077 筆資料
已處理 200/1077 筆資料
...
已處理 1077/1077 筆資料

匯入完成！
- 新增: 1077 筆
- 更新: 0 筆
- 跳過: 0 筆
- 錯誤: 0 筆
─────────────────────────────────────

✓ 匯入作業完成！
```

## 設定檔

編輯 `tools/StockMasterImporter/appsettings.json` 修改資料庫連線：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TradingSystemDB_Dev;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

## 注意事項

1. **編碼問題**：工具預設使用 Big5 編碼讀取 CSV，如果檔案是 UTF-8 編碼，請修改 `ImportStockMaster.cs` 第 46 行
2. **日期格式**：自動處理民國年格式（如 "1150201" = 2026/02/01）
3. **更新模式**：預設會更新已存在的股票資料，如不需要請選擇 "N"
4. **效能**：批次處理 100 筆一次，處理 1000+ 筆資料約需 5-10 秒

## 程式碼結構

```
tools/
├── ImportStockMaster.cs          # 核心匯入邏輯類別
├── ImportStockMaster.ps1         # PowerShell 執行腳本
└── StockMasterImporter/
    ├── Program.cs                # 主程式進入點
    ├── StockMasterImporter.csproj # 專案檔
    └── appsettings.json          # 設定檔
```

## 疑難排解

### 找不到 CSV 檔案
- 確認檔案路徑正確
- 使用絕對路徑避免路徑問題

### 資料庫連線失敗
- 檢查 appsettings.json 中的連線字串
- 確認 SQL Server 服務正在執行

### 編碼錯誤（亂碼）
- 修改 `ImportStockMaster.cs` 第 46 行的編碼設定
- 常見選項：`Encoding.UTF8` 或 `Encoding.GetEncoding("Big5")`

### 日期解析錯誤
- 檢查 CSV 的日期格式
- 修改 `ParseDate` 方法以符合實際格式

## 未來擴充

可以考慮加入以下功能：
- [ ] 支援更多 CSV 格式和編碼
- [ ] 加入資料驗證規則
- [ ] 支援差異比對（只更新有變動的欄位）
- [ ] 產生匯入報告檔案
- [ ] 支援排程自動執行

## 授權

此工具為專案內部使用工具，請勿外流。

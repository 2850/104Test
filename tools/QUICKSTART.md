# StockMaster CSV 匯入工具 - 快速使用指南

## 一鍵執行（最簡單）

在專案根目錄執行：

```powershell
.\tools\ImportStockMaster.ps1
```

這會自動：
1. 使用預設的 CSV 檔案路徑（`.github/prompts/CreateSystem/t187ap03_L.csv`）
2. 連接資料庫
3. 匯入或更新所有股票資料

## 指定 CSV 檔案

```powershell
.\tools\ImportStockMaster.ps1 "D:\path\to\your\t187ap03_L.csv"
```

## 完整執行流程示範

```powershell
PS D:\Web\Stock_2330> .\tools\ImportStockMaster.ps1

===== StockMaster CSV 匯入工具 =====

使用預設 CSV 檔案: D:\Web\Stock_2330\.github\prompts\CreateSystem\t187ap03_L.csv

✓ 資料庫連線成功

CSV 檔案: D:\Web\Stock_2330\.github\prompts\CreateSystem\t187ap03_L.csv

如果股票代碼已存在，是否要更新資料？(Y/N，預設: Y): Y

開始匯入... (更新模式: 是)

─────────────────────────────────────
開始讀取 CSV 檔案: D:\Web\Stock_2330\.github\prompts\CreateSystem\t187ap03_L.csv
找到 1077 筆資料準備匯入
已處理 100/1077 筆資料
已處理 200/1077 筆資料
已處理 300/1077 筆資料
...
已處理 1077/1077 筆資料

匯入完成！
- 新增: 1077 筆
- 更新: 0 筆
- 跳過: 0 筆
- 錯誤: 0 筆
─────────────────────────────────────

✓ 匯入作業完成！

按任意鍵結束...
```

## 資料驗證

匯入後，可以用以下 SQL 查詢驗證：

```sql
-- 檢查匯入的股票總數
SELECT COUNT(*) AS TotalStocks FROM StockMaster;

-- 查看前 10 筆資料
SELECT TOP 10 * FROM StockMaster ORDER BY StockCode;

-- 按產業統計
SELECT Industry, COUNT(*) AS Count 
FROM StockMaster 
GROUP BY Industry 
ORDER BY Count DESC;

-- 查看特定股票（例如台積電 2330）
SELECT * FROM StockMaster WHERE StockCode = '2330';
```

## 使用 API 查詢

匯入後可以透過 API 查詢股票資料：

```powershell
# 查詢所有股票
Invoke-RestMethod -Uri "http://localhost:5205/api/v1/stocks" -Method GET

# 查詢特定股票
Invoke-RestMethod -Uri "http://localhost:5205/api/v1/stocks/2330" -Method GET
```

## 常見問題

### Q: 需要先清空資料表嗎？
A: 不需要。程式會自動檢查股票代碼是否存在，並根據您的選擇決定是否更新。

### Q: 可以重複執行嗎？
A: 可以。如果選擇更新模式（Y），會更新已存在的資料；選擇不更新（N），則跳過已存在的股票。

### Q: 執行需要多久？
A: 約 5-15 秒（視資料筆數和資料庫效能而定）。批次處理每 100 筆一次。

### Q: 可以用於正式環境嗎？
A: 可以，但請先修改 `appsettings.json` 中的資料庫連線字串指向正式資料庫。

### Q: CSV 檔案格式有要求嗎？
A: 必須包含以下欄位：公司代號、公司名稱、公司簡稱、產業別、出表日期。工具支援 Big5 和 UTF-8 編碼。

## 更新已有資料

如果證交所發布新版 CSV，直接執行工具即可：

```powershell
# 下載新的 t187ap03_L.csv 後
.\tools\ImportStockMaster.ps1 "D:\Downloads\t187ap03_L.csv"
```

選擇更新模式（Y），程式會：
- 更新已存在的股票資訊
- 新增新上市的股票
- 保留其他欄位設定（如 LotSize, AllowOddLot）

## 進階：排程自動更新

可以使用 Windows 工作排程器定期執行：

1. 開啟「工作排程器」
2. 建立基本工作
3. 設定觸發條件（例如每週一早上 9:00）
4. 動作選擇「啟動程式」
5. 程式：`powershell.exe`
6. 引數：`-File "D:\Web\Stock_2330\tools\ImportStockMaster.ps1"`
7. 起始位置：`D:\Web\Stock_2330`

這樣就能自動保持股票資料最新！

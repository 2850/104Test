# 證券交易 API 資料庫結構設計

**日期**: 2026-02-03  
**系統**: SecuritiesTradingApi  
**資料庫**: TradingSystemDB_Dev

---

## 資料庫架構概覽

```
┌─────────────────────────────────────────────────────────────┐
│                    資料庫架構層次                              │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  [主檔資料層]                                                 │
│  - StockMaster: 股票基本資料                                  │
│                                                              │
│              ↓                                               │
│  [即時報價層]                                                 │
│  - StockQuotesSnapshot: 股票報價快照                          │
│                                                              │
│              ↓                                               │
│  [交易委託層 - CQRS 讀寫分離]                                 │
│  - OrdersWrite: 委託寫入表 (寫入優化)                         │
│  - OrdersRead:  委託查詢表 (讀取優化，反正規化)                │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 表結構詳細設計

### 1️⃣ StockMaster (股票主檔)

**用途**: 儲存台灣證券交易所上市櫃股票基本資料

**表名**: `StockMaster`

**欄位設計**:

```
┌─────────────────────────────────────────────────────────────┐
│                      StockMaster                             │
├──────────────────┬──────────────┬──────────┬────────────────┤
│ 欄位名稱          │ 資料型別      │ 限制     │ 說明            │
├──────────────────┼──────────────┼──────────┼────────────────┤
│ StockCode        │ NVARCHAR(10) │ PK, NN  │ 股票代號 "2330" │
│ StockName        │ NVARCHAR(100)│ NOT NULL │ 完整公司名稱    │
│ StockNameShort   │ NVARCHAR(50) │ NOT NULL │ 公司簡稱        │
│ StockNameEn      │ NVARCHAR(200)│ NULL    │ 英文名稱        │
│ Exchange         │ NVARCHAR(10) │ NOT NULL │ TWSE/TPEX      │
│ Industry         │ NVARCHAR(50) │ NULL    │ 產業別          │
│ LotSize          │ INT          │ NOT NULL │ 交易單位 (1000) │
│ AllowOddLot      │ BIT          │ NOT NULL │ 允許零股交易    │
│ IsActive         │ BIT          │ NOT NULL │ 是否啟用        │
│ ListedDate       │ DATE         │ NULL    │ 上市/上櫃日期   │
│ CreatedAt        │ DATETIME2    │ NOT NULL │ 建立時間        │
│ UpdatedAt        │ DATETIME2    │ NOT NULL │ 更新時間        │
└──────────────────┴──────────────┴──────────┴────────────────┘
```

**主鍵與索引**:
- PK: StockCode
- Index: IX_StockMaster_Exchange_IsActive (Exchange, IsActive) INCLUDE (StockName)

**預設值**:
- LotSize = 1000
- AllowOddLot = 0 (false)
- IsActive = 1 (true)
- CreatedAt = SYSUTCDATETIME()
- UpdatedAt = SYSUTCDATETIME()

**驗證規則**:
- StockCode: 必填，4 位數字，正則 ^\d{4}$
- StockName: 必填，最大長度 100
- LotSize: 必須大於 0

---

### 2️⃣ StockQuotesSnapshot (股票報價快照)

**用途**: 儲存股票即時報價資訊，每日多次更新

**表名**: `StockQuotesSnapshot`

**欄位設計**:

```
┌─────────────────────────────────────────────────────────────┐
│                  StockQuotesSnapshot                         │
├──────────────────┬──────────────┬──────────┬────────────────┤
│ 欄位名稱          │ 資料型別      │ 限制     │ 說明            │
├──────────────────┼──────────────┼──────────┼────────────────┤
│ StockCode        │ NVARCHAR(10) │ PK, NN  │ 股票代號        │
│ CurrentPrice     │ DECIMAL(10,2)│ NOT NULL │ 現價            │
│ YesterdayPrice   │ DECIMAL(10,2)│ NOT NULL │ 昨日收盤價      │
│ OpenPrice        │ DECIMAL(10,2)│ NOT NULL │ 開盤價          │
│ HighPrice        │ DECIMAL(10,2)│ NOT NULL │ 最高價          │
│ LowPrice         │ DECIMAL(10,2)│ NOT NULL │ 最低價          │
│ LimitUpPrice     │ DECIMAL(10,2)│ NOT NULL │ 漲停價          │
│ LimitDownPrice   │ DECIMAL(10,2)│ NOT NULL │ 跌停價          │
│ ChangeAmount     │ DECIMAL(10,2)│ NOT NULL │ 漲跌額          │
│ ChangePercent    │ DECIMAL(10,2)│ NOT NULL │ 漲跌百分比      │
│ TotalVolume      │ BIGINT       │ NOT NULL │ 成交量 (股)     │
│ TotalValue       │ DECIMAL(15,2)│ NULL    │ 成交金額 (元)   │
│ UpdateTime       │ DATETIME2    │ NOT NULL │ 更新時間        │
└──────────────────┴──────────────┴──────────┴────────────────┘
```

**主鍵與索引**:
- PK: StockCode
- Index: IX_StockQuotesSnapshot_UpdateTime (UpdateTime DESC)

**關聯**:
- FK: StockCode → StockMaster.StockCode (考慮新增)

**特性**:
- CQRS 架構中的 HOT LAYER（熱層）
- 即時讀取，定期寫入
- 高頻率更新

---

### 3️⃣ OrdersWrite (委託寫入表)

**用途**: 儲存使用者提交的交易委託，針對寫入效能優化

**表名**: `OrdersWrite`

**欄位設計**:

```
┌─────────────────────────────────────────────────────────────┐
│                      OrdersWrite                             │
├──────────────────┬──────────────┬──────────┬────────────────┤
│ 欄位名稱          │ 資料型別      │ 限制     │ 說明            │
├──────────────────┼──────────────┼──────────┼────────────────┤
│ OrderId          │ BIGINT       │ PK, NN  │ 委託單號        │
│ UserId           │ INT          │ NOT NULL │ 使用者ID        │
│ StockCode        │ NVARCHAR(10) │ FK, NN  │ 股票代號        │
│ OrderType        │ TINYINT      │ NOT NULL │ 委託類型 (0-2)  │
│ BuySell          │ TINYINT      │ NOT NULL │ 買賣別 (1=買,2=賣)│
│ Price            │ DECIMAL(10,2)│ NOT NULL │ 委託價格        │
│ Quantity         │ INT          │ NOT NULL │ 委託數量 (股)   │
│ OrderStatus      │ TINYINT      │ NOT NULL │ 委託狀態 (1-6)  │
│ TradeDate        │ DATE         │ NOT NULL │ 交易日期        │
│ CreatedAt        │ DATETIME2    │ NOT NULL │ 建立時間        │
│ OrderSeq         │ BIGINT       │ NOT NULL │ 委託序號        │
└──────────────────┴──────────────┴──────────┴────────────────┘
```

**主鍵與索引**:
- PK: OrderId
- PK: (UserId, OrderSeq) - 複合主鍵選項
- Index: IX_OrdersWrite_UserId_TradeDate (UserId, TradeDate DESC)
- Index: IX_OrdersWrite_StockCode_OrderStatus (StockCode, OrderStatus)

**外鍵**:
- StockCode → StockMaster.StockCode

**預設值**:
- OrderStatus = 1 (待成交)
- CreatedAt = SYSUTCDATETIME()

**欄位編碼**:

*OrderType* (委託類型):
- 0 = 限價委託 (ROL - Rest of Limit)
- 1 = 市價委託 (ROD - Rest of Day)
- 2 = 盤中零股 (Odd Lot)

*BuySell* (買賣別):
- 1 = 買進
- 2 = 賣出

*OrderStatus* (委託狀態):
- 1 = 待成交 (Pending)
- 2 = 部分成交 (Partially Filled)
- 3 = 全部成交 (Filled)
- 4 = 待取消 (Cancel Pending)
- 5 = 已取消 (Cancelled)
- 6 = 已過期/廢單 (Expired)

---

### 4️⃣ OrdersRead (委託查詢表)

**用途**: 儲存使用者查詢用的委託資料，採反正規化設計，針對讀取效能優化

**表名**: `OrdersRead`

**欄位設計**:

```
┌──────────────────────────────────────────────────────────────┐
│                      OrdersRead                               │
├──────────────────┬──────────────┬──────────┬─────────────────┤
│ 欄位名稱          │ 資料型別      │ 限制     │ 說明             │
├──────────────────┼──────────────┼──────────┼─────────────────┤
│ OrderId          │ BIGINT       │ PK, NN  │ 委託單號         │
│ UserId           │ INT          │ NOT NULL │ 使用者ID         │
│ UserName         │ NVARCHAR(100)│ NULL    │ 使用者名稱(反正規化)│
│ StockCode        │ NVARCHAR(10) │ NOT NULL │ 股票代號         │
│ StockName        │ NVARCHAR(100)│ NULL    │ 股票名稱(反正規化)│
│ StockNameShort   │ NVARCHAR(50) │ NULL    │ 股票簡稱(反正規化)│
│ OrderType        │ TINYINT      │ NOT NULL │ 委託類型 (0-2)   │
│ OrderTypeName    │ NVARCHAR(50) │ NULL    │ 委託類型名稱     │
│ BuySell          │ TINYINT      │ NOT NULL │ 買賣別 (1/2)     │
│ BuySellName      │ NVARCHAR(20) │ NULL    │ 買賣別名稱(買/賣)│
│ Price            │ DECIMAL(10,2)│ NOT NULL │ 委託價格         │
│ Quantity         │ INT          │ NOT NULL │ 委託數量 (股)    │
│ FilledQuantity   │ INT          │ NOT NULL │ 成交數量 (股)    │
│ OrderStatus      │ TINYINT      │ NOT NULL │ 委託狀態 (1-6)   │
│ OrderStatusName  │ NVARCHAR(50) │ NULL    │ 狀態名稱         │
│ TradeDate        │ DATE         │ NOT NULL │ 交易日期         │
│ CreatedAt        │ DATETIME2    │ NOT NULL │ 建立時間         │
│ OrderSeq         │ BIGINT       │ NOT NULL │ 委託序號         │
└──────────────────┴──────────────┴──────────┴─────────────────┘
```

**主鍵與索引**:
- PK: OrderId
- Index: IX_OrdersRead_UserId_TradeDate (UserId, TradeDate DESC, OrderStatus)
- Index: IX_OrdersRead_StockCode (StockCode)
- Index: IX_OrdersRead_OrderStatus (OrderStatus)

**反正規化欄位** (來自其他表):
- UserName (來自 UserAccounts)
- StockName, StockNameShort (來自 StockMaster)
- OrderTypeName, BuySellName, OrderStatusName (來自查詢邏輯)
- FilledQuantity (由交易系統維護)

**特性**:
- CQRS 架構中的讀取模型
- 反正規化設計，犧牲寫入一致性換取讀取效能
- 定期同步或事件驅動更新

---

## 資料流與關聯

```
┌──────────────┐
│ StockMaster  │
│ (股票主檔)    │
└────────┬─────┘
         │ StockCode (FK)
         │
    ┌────┴─────┬─────────────┐
    │          │             │
┌───▼──────┐  ┌─▼────────────┐
│OrdersWrite│ │StockQuotesSnapshot│
│(寫入表)   │  │ (報價快照)    │
└───┬──────┘  └───────────────┘
    │
    │ 同步或聚合
    │
┌───▼──────────┐
│ OrdersRead   │
│ (查詢表)      │
└──────────────┘
```

---

## 資料一致性與同步策略

### OrdersWrite → OrdersRead 同步

**方式**: 事件驅動 + 定期清理

```
1. 當 OrdersWrite 有新記錄時
   ↓
2. 發送 OrderCreated 事件
   ↓
3. 事件處理服務讀取 StockMaster 資料
   ↓
4. 組合反正規化資料寫入 OrdersRead
   ↓
5. 後續更新 (OrderUpdated, OrderCancelled) 同步 OrdersRead
```

**特性**:
- 最終一致性 (Eventual Consistency)
- 讀寫分離，互不阻塞
- OrdersWrite 保證正確性
- OrdersRead 保證讀取性能

---

## 索引策略

### StockMaster
```
主鍵索引:
  PK_StockMaster (StockCode)

輔助索引:
  IX_StockMaster_Exchange_IsActive
  - 欄位: (Exchange, IsActive)
  - 包含: (StockName)
  - 用途: 快速查詢指定交易所的啟用股票
```

### StockQuotesSnapshot
```
主鍵索引:
  PK_StockQuotesSnapshot (StockCode)

輔助索引:
  IX_StockQuotesSnapshot_UpdateTime
  - 欄位: (UpdateTime DESC)
  - 用途: 快速查詢最新的報價資料
```

### OrdersWrite
```
主鍵索引:
  PK_OrdersWrite (OrderId)

輔助索引:
  IX_OrdersWrite_UserId_TradeDate
  - 欄位: (UserId, TradeDate DESC)
  - 用途: 使用者查詢特定日期的委託

  IX_OrdersWrite_StockCode_OrderStatus
  - 欄位: (StockCode, OrderStatus)
  - 用途: 統計特定股票的委託狀態
```

### OrdersRead
```
主鍵索引:
  PK_OrdersRead (OrderId)

輔助索引:
  IX_OrdersRead_UserId_TradeDate
  - 欄位: (UserId, TradeDate DESC, OrderStatus)
  - 用途: 使用者快速查詢並過濾委託

  IX_OrdersRead_StockCode
  - 欄位: (StockCode)
  - 用途: 股票維度查詢

  IX_OrdersRead_OrderStatus
  - 欄位: (OrderStatus)
  - 用途: 委託狀態篩選
```

---

## 資料隔離與保留策略

### 熱資料 (Hot Data) - 當月
- **表**: OrdersWrite, OrdersRead, StockQuotesSnapshot
- **保留期**: 無限期
- **操作**: 完整索引，即時查詢

### 溫資料 (Warm Data) - 前 3 個月
- **表**: OrdersRead (歷史存檔)
- **保留期**: 3 個月
- **操作**: 歸檔表，較少查詢

### 冷資料 (Cold Data) - 歷史資料
- **表**: 歷史表 (未來實作)
- **保留期**: 長期保存
- **操作**: 列存儲，分析查詢

---

## 效能最佳實踐

### 1. 查詢最佳化
```
✓ 使用 OrdersRead 進行使用者查詢
✓ 使用參數化查詢避免 SQL injection
✓ 利用索引進行過濾
✓ 避免全表掃描
```

### 2. 寫入最佳化
```
✓ 使用 OrdersWrite 進行委託寫入
✓ 批量插入時使用 BulkInsert
✓ 最小化鎖定時間
✓ 異步更新 OrdersRead
```

### 3. 記憶體最佳化
```
✓ StockQuotesSnapshot 使用列表儲存
✓ 定期清理過期資料
✓ 使用分區表管理大量歷史資料
```

---

## 範例查詢

### 查詢使用者今日委託
```sql
SELECT OrderId, StockCode, StockName, BuySellName, Price, 
       Quantity, FilledQuantity, OrderStatusName, CreatedAt
FROM OrdersRead
WHERE UserId = @UserId 
  AND TradeDate = CAST(GETDATE() AS DATE)
ORDER BY CreatedAt DESC
```

### 查詢股票即時報價
```sql
SELECT StockCode, CurrentPrice, ChangePercent, TotalVolume,
       UpdateTime
FROM StockQuotesSnapshot
WHERE StockCode = @StockCode
```

### 查詢待成交委託
```sql
SELECT OrderId, UserId, StockCode, Price, Quantity,
       OrderStatus, CreatedAt
FROM OrdersWrite
WHERE OrderStatus IN (1, 2)  -- 待成交、部分成交
  AND TradeDate = CAST(GETDATE() AS DATE)
```

---

## 擴展計畫

### Phase 2 (待實作)
- [ ] 使用者帳戶表 (UserAccounts)
- [ ] 成交紀錄表 (Trades)
- [ ] 持倉查詢表 (PositionsRead)
- [ ] 委託簿表 (OrderBookLevels)

### Phase 3 (待實作)
- [ ] 歷史 Tick 資料表 (StockTicksHistory)
- [ ] 分析表 (Analytics)
- [ ] 分區表實作
- [ ] 列存儲優化

---

## 資料庫設定參考

**排序規則**: Chinese_Taiwan_Stroke_CI_AS
- 支援繁體中文排序
- 不區分大小寫 (CI)
- 不區分口音符號 (AS)

**連接字串**: (參考 appsettings.json)
```
Server=(local);Database=TradingSystemDB_Dev;
Integrated Security=true;
```

---

**最後更新**: 2026-02-03  
**維護人員**: Development Team

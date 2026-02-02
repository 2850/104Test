# Data Model: 證券交易資料查詢系統

**日期**: 2026-02-02  
**Feature**: 003-securities-trading-api  
**狀態**: Phase 1 Design

## 資料模型總覽

本系統採用三層式資料架構（Hot/Warm/Cold）+ CQRS 讀寫分離模式，確保高頻交易場景下的效能與資料一致性。

### 資料層架構

```
┌─────────────────────────────────────────────────────────────┐
│  HOT LAYER (In-Memory, Schema-Only, 極速讀寫)                │
│  - StockQuotesSnapshot: 即時報價快照                         │
└─────────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  WARM LAYER (In-Memory, Durable, 當日資料)                   │
│  - OrderBookLevels: 五檔委託簿                               │
└─────────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  COLD LAYER (Disk, Columnstore, 歷史資料)                    │
│  - StockTicksHistory: 歷史 Tick 資料                         │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  CQRS (讀寫分離)                                              │
│  - OrdersWrite: 委託寫入表（寫入優化）                        │
│  - OrdersRead: 委託查詢表（讀取優化，反正規化）               │
│  - PositionsRead: 持倉查詢表                                  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  MASTER DATA (主檔資料)                                       │
│  - StockMaster: 股票主檔                                      │
│  - UserAccounts: 使用者帳戶（MVP 暫不實作）                   │
└─────────────────────────────────────────────────────────────┘
```

---

## 實體定義

### 1. StockMaster (股票主檔)

**用途**: 儲存台灣證券交易所上市櫃股票基本資料

**資料表**: `StockMaster`

**欄位**:

| 欄位名稱 | 資料型別 | 限制 | 說明 |
|---------|---------|------|------|
| StockCode | NVARCHAR(10) | PK, NOT NULL | 股票代號（如 "2330"） |
| StockName | NVARCHAR(100) | NOT NULL | 公司名稱（如 "台灣積體電路製造股份有限公司"） |
| StockNameShort | NVARCHAR(50) | NOT NULL | 公司簡稱（如 "台積電"） |
| StockNameEn | NVARCHAR(200) | NULL | 英文名稱 |
| Exchange | NVARCHAR(10) | NOT NULL | 交易所（TWSE=上市, TPEX=上櫃） |
| Industry | NVARCHAR(50) | NULL | 產業別 |
| LotSize | INT | NOT NULL, DEFAULT 1000 | 交易單位（整股交易單位，預設 1000 股） |
| AllowOddLot | BIT | NOT NULL, DEFAULT 0 | 是否允許零股交易 |
| IsActive | BIT | NOT NULL, DEFAULT 1 | 是否啟用 |
| ListedDate | DATE | NULL | 上市/上櫃日期 |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT SYSUTCDATETIME() | 建立時間 |
| UpdatedAt | DATETIME2 | NOT NULL, DEFAULT SYSUTCDATETIME() | 更新時間 |

**索引**:
- Primary Key: `StockCode`
- Index: `IX_StockMaster_Exchange_IsActive` (Exchange, IsActive) INCLUDE (StockName)

**關聯**:
- 一對多: `OrdersWrite.StockCode` → `StockMaster.StockCode`
- 一對多: `OrdersRead.StockCode` → `StockMaster.StockCode`

**驗證規則**:
- StockCode: 必填，4 位數字，正則表達式 `^\d{4}$`
- StockName: 必填，最大長度 100
- LotSize: 必須大於 0

**EF Core Entity**:
```csharp
public class StockMaster
{
    public string StockCode { get; set; }
    public string StockName { get; set; }
    public string StockNameShort { get; set; }
    public string? StockNameEn { get; set; }
    public string Exchange { get; set; }
    public string? Industry { get; set; }
    public int LotSize { get; set; } = 1000;
    public bool AllowOddLot { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime? ListedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<OrdersWrite> OrdersWrite { get; set; }
    public ICollection<OrdersRead> OrdersRead { get; set; }
}
```

---

### 2. StockQuotesSnapshot (即時報價快照 - HOT LAYER)

**用途**: 儲存股票即時報價資料，用於下單價格驗證與即時查詢

**資料表**: `StockQuotes_Snapshot`

**特性**: 
- In-Memory OLTP
- DURABILITY = SCHEMA_ONLY（不持久化，重啟後遺失）
- NONCLUSTERED HASH Index (BUCKET_COUNT = 4096)

**欄位**:

| 欄位名稱 | 資料型別 | 限制 | 說明 |
|---------|---------|------|------|
| StockCode | NVARCHAR(10) | PK, NOT NULL | 股票代號 |
| CurrentPrice | DECIMAL(18,2) | NOT NULL | 最新成交價 |
| YesterdayPrice | DECIMAL(18,2) | NOT NULL | 昨收價 |
| OpenPrice | DECIMAL(18,2) | NOT NULL | 開盤價 |
| HighPrice | DECIMAL(18,2) | NOT NULL | 最高價 |
| LowPrice | DECIMAL(18,2) | NOT NULL | 最低價 |
| LimitUpPrice | DECIMAL(18,2) | NOT NULL | 漲停價 |
| LimitDownPrice | DECIMAL(18,2) | NOT NULL | 跌停價 |
| ChangeAmount | DECIMAL(18,2) | NOT NULL | 漲跌金額 |
| ChangePercent | DECIMAL(10,4) | NOT NULL | 漲跌幅 (%) |
| TotalVolume | BIGINT | NOT NULL | 當日累積成交量 |
| TotalValue | DECIMAL(20,2) | NULL | 當日累積成交金額 |
| UpdateTime | DATETIME2 | NOT NULL | 最後更新時間 |

**索引**:
- Primary Key: NONCLUSTERED HASH (StockCode) WITH (BUCKET_COUNT = 4096)

**關聯**: 無（快照表，獨立存在）

**驗證規則**:
- CurrentPrice, YesterdayPrice, OpenPrice, HighPrice, LowPrice: 必須 > 0
- LimitUpPrice >= CurrentPrice >= LimitDownPrice
- HighPrice >= LowPrice

**EF Core Entity**:
```csharp
public class StockQuotesSnapshot
{
    public string StockCode { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal YesterdayPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal LimitUpPrice { get; set; }
    public decimal LimitDownPrice { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
    public long TotalVolume { get; set; }
    public decimal? TotalValue { get; set; }
    public DateTime UpdateTime { get; set; }
}
```

**資料來源**: 台灣證券交易所官方即時報價 API (https://mis.twse.com.tw/stock/api/getStockInfo.jsp)

**更新頻率**: 每 5 秒一次（透過 InMemory Cache 快取，配合台灣證交所 userDelay 建議）

---

### 3. OrderBookLevels (五檔委託簿 - WARM LAYER)

**用途**: 儲存股票五檔買賣委託資料

**資料表**: `OrderBook_Levels`

**特性**: 
- In-Memory OLTP
- DURABILITY = SCHEMA_AND_DATA（持久化）

**欄位**:

| 欄位名稱 | 資料型別 | 限制 | 說明 |
|---------|---------|------|------|
| Id | BIGINT | PK, IDENTITY(1,1) | 自增主鍵 |
| StockCode | NVARCHAR(10) | NOT NULL | 股票代號 |
| SequenceNo | BIGINT | NOT NULL | 序列號（用於排序） |
| Level | TINYINT | NOT NULL | 檔位 (1-5) |
| BidPrice | DECIMAL(18,2) | NULL | 買進價格 |
| BidVolume | BIGINT | NULL | 買進量 |
| AskPrice | DECIMAL(18,2) | NULL | 賣出價格 |
| AskVolume | BIGINT | NULL | 賣出量 |
| SnapshotTime | DATETIME2 | NOT NULL | 快照時間 |
| CreatedAt | DATETIME2 | NOT NULL | 建立時間 |

**索引**:
- Primary Key: NONCLUSTERED (Id)
- Index: NONCLUSTERED (StockCode, SequenceNo DESC)

**關聯**: 無（快照表）

**驗證規則**:
- Level: 必須在 1-5 之間
- BidPrice, AskPrice: 若非 NULL，必須 > 0

**EF Core Entity**:
```csharp
public class OrderBookLevels
{
    public long Id { get; set; }
    public string StockCode { get; set; }
    public long SequenceNo { get; set; }
    public byte Level { get; set; }
    public decimal? BidPrice { get; set; }
    public long? BidVolume { get; set; }
    public decimal? AskPrice { get; set; }
    public long? AskVolume { get; set; }
    public DateTime SnapshotTime { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**MVP 範圍**: 本表在 MVP 階段暫不使用，保留供未來功能擴充

---

### 4. OrdersWrite (委託寫入表 - CQRS Write Side)

**用途**: 高頻委託下單寫入，極簡欄位設計

**資料表**: `Orders_Write`

**特性**:
- 寫入優化：極簡索引
- 分區：按 TradeDate 分區
- CQRS Write Side

**欄位**:

| 欄位名稱 | 資料型別 | 限制 | 說明 |
|---------|---------|------|------|
| OrderId | BIGINT | PK, IDENTITY(1,1) | 委託單編號 |
| UserId | INT | NOT NULL | 使用者編號（MVP 固定 1） |
| StockCode | NVARCHAR(10) | NOT NULL, FK | 股票代號 |
| OrderType | TINYINT | NOT NULL | 買賣別（1=買進, 2=賣出，對應 spec.md 中的「買賣別」術語） |
| Price | DECIMAL(18,2) | NOT NULL | 委託價格 |
| Quantity | INT | NOT NULL | 委託數量 |
| OrderStatus | TINYINT | NOT NULL, DEFAULT 1 | 委託狀態（1=已委託, MVP 固定） |
| TradeDate | DATE | NOT NULL | 交易日期（分區鍵） |
| CreatedAt | DATETIME2 | NOT NULL | 建立時間 |
| OrderSeq | BIGINT | NOT NULL, UNIQUE | 委託序號（由 SEQUENCE 產生） |

**索引**:
- Primary Key: CLUSTERED (TradeDate, OrderId)
- Index: NONCLUSTERED (UserId, TradeDate DESC, OrderStatus) INCLUDE (OrderId, StockCode, OrderSeq)
- Index: NONCLUSTERED (StockCode, TradeDate DESC) WHERE OrderStatus IN (1, 2) (Filtered Index)

**關聯**:
- 多對一: `OrdersWrite.StockCode` → `StockMaster.StockCode`

**驗證規則**:
- UserId: 必須 > 0
- StockCode: 必須存在於 StockMaster
- OrderType: 必須為 1 或 2
- Price: 必須 > 0，且在漲跌停範圍內
- Quantity: 必須 > 0，且為 1000 的整數倍（整股交易）
- OrderStatus: MVP 固定為 1

**EF Core Entity**:
```csharp
public class OrdersWrite
{
    public long OrderId { get; set; }
    public int UserId { get; set; }
    public string StockCode { get; set; }
    public byte OrderType { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public byte OrderStatus { get; set; } = 1;
    public DateTime TradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public long OrderSeq { get; set; }

    // Navigation Properties
    public StockMaster Stock { get; set; }
}
```

---

### 5. OrdersRead (委託查詢表 - CQRS Read Side)

**用途**: 使用者委託查詢，反正規化設計，查詢效能最佳化

**資料表**: `Orders_Read`

**特性**:
- 讀取優化：反正規化，包含關聯資料
- 分區：按 TradeDate 分區
- CQRS Read Side

**欄位**:

| 欄位名稱 | 資料型別 | 限制 | 說明 |
|---------|---------|------|------|
| OrderId | BIGINT | PK, NOT NULL | 委託單編號 |
| UserId | INT | NOT NULL | 使用者編號 |
| UserName | NVARCHAR(50) | NULL | 使用者名稱（反正規化） |
| StockCode | NVARCHAR(10) | NOT NULL | 股票代號 |
| StockName | NVARCHAR(100) | NULL | 股票名稱（反正規化） |
| StockNameShort | NVARCHAR(50) | NULL | 股票簡稱（反正規化） |
| OrderType | TINYINT | NOT NULL | 買賣別（1=買進, 2=賣出） |
| OrderTypeName | NVARCHAR(10) | NULL | 買賣別名稱（反正規化：「買進」/「賣出」，對應 spec.md 中的「買賣別」術語） |
| Price | DECIMAL(18,2) | NOT NULL | 委託價格 |
| Quantity | INT | NOT NULL | 委託數量 |
| FilledQuantity | INT | NOT NULL, DEFAULT 0 | 已成交數量（MVP 固定 0） |
| OrderStatus | TINYINT | NOT NULL | 委託狀態 |
| OrderStatusName | NVARCHAR(20) | NULL | 委託狀態名稱（反正規化："已委託"） |
| TradeDate | DATE | NOT NULL | 交易日期（分區鍵） |
| CreatedAt | DATETIME2 | NOT NULL | 建立時間 |
| OrderSeq | BIGINT | NOT NULL | 委託序號 |

**索引**:
- Primary Key: CLUSTERED (TradeDate, UserId, OrderId)
- Index: NONCLUSTERED (StockCode, TradeDate DESC, OrderStatus) INCLUDE (OrderId, Price, Quantity, FilledQuantity)
- Index: NONCLUSTERED (UserId, OrderStatus, TradeDate DESC) WHERE OrderStatus IN (1, 2)

**關聯**: 無（反正規化，獨立查詢）

**驗證規則**: 同 OrdersWrite

**EF Core Entity**:
```csharp
public class OrdersRead
{
    public long OrderId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string StockCode { get; set; }
    public string? StockName { get; set; }
    public string? StockNameShort { get; set; }
    public byte OrderType { get; set; }
    public string? OrderTypeName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int FilledQuantity { get; set; } = 0;
    public byte OrderStatus { get; set; }
    public string? OrderStatusName { get; set; }
    public DateTime TradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public long OrderSeq { get; set; }
}
```

---

### 6. PositionsRead (持倉查詢表)

**用途**: 使用者持倉查詢（MVP 暫不實作）

**資料表**: `Positions_Read`

**欄位**:

| 欄位名稱 | 資料型別 | 限制 | 說明 |
|---------|---------|------|------|
| UserId | INT | PK, NOT NULL | 使用者編號 |
| StockCode | NVARCHAR(10) | PK, NOT NULL | 股票代號 |
| Quantity | INT | NOT NULL, DEFAULT 0 | 持倉數量 |
| AvgCost | DECIMAL(18,2) | NOT NULL, DEFAULT 0 | 平均成本 |
| MarketValue | DECIMAL(20,2) | NULL | 市值 |
| UnrealizedPL | DECIMAL(20,2) | NULL | 未實現損益 |
| UpdatedAt | DATETIME2 | NOT NULL | 更新時間 |

**索引**:
- Primary Key: CLUSTERED (UserId, StockCode)
- Index: NONCLUSTERED (UserId) WHERE Quantity > 0

**MVP 範圍**: 本表在 MVP 階段暫不使用，保留供未來功能擴充

---

### 7. UserAccounts (使用者帳戶)

**用途**: 使用者帳戶資訊（MVP 暫不實作會員系統）

**資料表**: `UserAccounts`

**欄位**:

| 欄位名稱 | 資料型別 | 限制 | 說明 |
|---------|---------|------|------|
| UserId | INT | PK, IDENTITY(1,1) | 使用者編號 |
| UserName | NVARCHAR(50) | NOT NULL | 使用者名稱 |
| AvailableBalance | DECIMAL(20,2) | NOT NULL, DEFAULT 0 | 可用餘額 |
| TotalBalance | DECIMAL(20,2) | NOT NULL, DEFAULT 0 | 總餘額 |
| MaxOrderAmount | DECIMAL(20,2) | NULL | 單筆委託上限 |
| IsActive | BIT | NOT NULL, DEFAULT 1 | 是否啟用 |
| CreatedAt | DATETIME2 | NOT NULL | 建立時間 |
| UpdatedAt | DATETIME2 | NOT NULL | 更新時間 |

**索引**:
- Primary Key: CLUSTERED (UserId)

**MVP 範圍**: 本表在 MVP 階段僅建立結構，不實作會員系統邏輯，所有委託使用固定 UserId = 1

---

## 資料傳輸物件 (DTOs)

### StockQueryDto (股票查詢請求)

```csharp
public class StockQueryDto
{
    public string StockCode { get; set; }
}
```

### StockInfoDto (股票資訊回應)

```csharp
public class StockInfoDto
{
    public string StockCode { get; set; }
    public string StockName { get; set; }
    public string StockNameShort { get; set; }
    public string Exchange { get; set; }
    public string Industry { get; set; }
}
```

### StockQuoteDto (股票報價回應)

```csharp
public class StockQuoteDto
{
    public string StockCode { get; set; }
    public string StockName { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal YesterdayPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal LimitUpPrice { get; set; }
    public decimal LimitDownPrice { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
    public long TotalVolume { get; set; }
    public decimal? TotalValue { get; set; }
    public DateTime UpdateTime { get; set; }
}
```

### CreateOrderDto (建立委託請求)

```csharp
public class CreateOrderDto
{
    public int UserId { get; set; } = 1;  // MVP 固定 1
    public string StockCode { get; set; }
    public byte OrderType { get; set; }  // 1=買進, 2=賣出
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
```

### OrderDto (委託單回應)

```csharp
public class OrderDto
{
    public long OrderId { get; set; }
    public long OrderSeq { get; set; }
    public int UserId { get; set; }
    public string StockCode { get; set; }
    public string StockName { get; set; }
    public string OrderTypeName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int FilledQuantity { get; set; }
    public string OrderStatusName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### CreateOrderResultDto (建立委託結果)

```csharp
public class CreateOrderResultDto
{
    public long OrderId { get; set; }
    public long OrderSeq { get; set; }
    public string Message { get; set; } = "委託單建立成功";
}
```

---

## 資料庫序列 (Sequences)

### seq_OrderBookSequence

**用途**: 產生五檔委託簿序列號

```sql
CREATE SEQUENCE seq_OrderBookSequence
    START WITH 1
    INCREMENT BY 1
    CACHE 10000;
```

### seq_OrderSequence

**用途**: 產生委託單序號（OrderSeq）

```sql
CREATE SEQUENCE seq_OrderSequence
    START WITH 1
    INCREMENT BY 1
    CACHE 10000;
```

---

## 實體關係圖 (ER Diagram)

```
┌─────────────────┐
│  StockMaster    │ (主檔)
├─────────────────┤
│ PK StockCode    │
│    StockName    │
│    ...          │
└────────┬────────┘
         │
         │ 1:N
         ▼
┌─────────────────┐          ┌─────────────────┐
│  OrdersWrite    │ (CQRS)   │  OrdersRead     │ (CQRS)
├─────────────────┤  同步寫入 ├─────────────────┤
│ PK OrderId      │ ───────> │ PK OrderId      │
│ FK StockCode    │          │    StockCode    │
│    OrderType    │          │    StockName    │ (反正規化)
│    Price        │          │    OrderTypeName│ (反正規化)
│    ...          │          │    ...          │
└─────────────────┘          └─────────────────┘

┌─────────────────────┐
│ StockQuotesSnapshot │ (HOT LAYER, 獨立)
├─────────────────────┤
│ PK StockCode        │
│    CurrentPrice     │
│    LimitUpPrice     │
│    LimitDownPrice   │
│    ...              │
└─────────────────────┘
```

---

## 資料初始化

### 股票主檔初始化

**資料來源**: `t187ap03_L.csv`（台灣證券交易所公開資訊）

**初始化腳本**:
```csharp
public async Task SeedStockMasterAsync()
{
    var csvPath = "database/seed-data/t187ap03_L.csv";
    var lines = await File.ReadAllLinesAsync(csvPath);
    
    foreach (var line in lines.Skip(1))  // 跳過標題列
    {
        var fields = line.Split(',');
        var stock = new StockMaster
        {
            StockCode = fields[1].Trim('"'),
            StockName = fields[2].Trim('"'),
            StockNameShort = fields[3].Trim('"'),
            StockNameEn = fields[12].Trim('"'),
            Exchange = "TWSE",
            Industry = fields[5].Trim('"'),
            LotSize = 1000,
            AllowOddLot = false,
            IsActive = true,
            ListedDate = DateTime.ParseExact(fields[9].Trim('"'), "yyyyMMdd", null)
        };
        
        _dbContext.StockMaster.Add(stock);
    }
    
    await _dbContext.SaveChangesAsync();
}
```

### 測試使用者初始化

```sql
INSERT INTO UserAccounts (UserId, UserName, AvailableBalance, TotalBalance, MaxOrderAmount, IsActive)
VALUES (1, N'測試使用者', 1000000, 1000000, 500000, 1);
```

---

## 資料驗證摘要

| 實體 | 關鍵驗證規則 |
|------|-------------|
| StockMaster | StockCode 必須為 4 位數字；LotSize > 0 |
| StockQuotesSnapshot | 價格必須 > 0；漲跌停範圍驗證 |
| OrdersWrite | StockCode 必須存在；Price 在漲跌停範圍；Quantity 必須為 1000 整數倍 |
| OrdersRead | 同 OrdersWrite |
| CreateOrderDto | 所有欄位必填；OrderType 必須為 1 或 2 |

---

## Phase 1 Data Model Complete ✅

**下一步**: 產生 contracts/openapi.yaml

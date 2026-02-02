-- ============================================
-- 高頻交易系統 - 完整三表分離架構（含下單系統）
-- 設計理念: Hot/Warm/Cold Data Tiering + CQRS
-- 建立日期: 2026-02-02
-- 版本: v1.1 (含下單驗證與手續費計算)
-- 適用於: Microsoft SQL Server 2019+ (支援 In-Memory OLTP)
-- ============================================

USE [master];
GO

-- ============================================
-- 前置檢查: 確認 In-Memory OLTP 功能
-- ============================================
IF SERVERPROPERTY('IsXTPSupported') = 0
BEGIN
    RAISERROR('此伺服器不支援 In-Memory OLTP 功能，請升級至 SQL Server 2014+ Enterprise/Developer 版本', 16, 1);
    RETURN;
END
GO

-- ============================================
-- 步驟 0: 建立資料庫（如需要）
-- ============================================
/*
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'TradingSystemDB')
BEGIN
    CREATE DATABASE [TradingSystemDB]
    CONTAINMENT = NONE
    ON PRIMARY 
    ( 
        NAME = N'TradingSystemDB_Data', 
        FILENAME = N'D:\SQLData\TradingSystemDB.mdf',  -- ← 請修改為您的路徑
        SIZE = 2048MB,
        MAXSIZE = UNLIMITED,
        FILEGROWTH = 512MB
    ),
    FILEGROUP [TradingSystemDB_MemoryOptimized] CONTAINS MEMORY_OPTIMIZED_DATA
    (
        NAME = N'TradingSystemDB_MemOptimized',
        FILENAME = N'D:\SQLData\TradingSystemDB_MemOptimized'  -- ← 請修改為您的路徑
    )
    LOG ON 
    ( 
        NAME = N'TradingSystemDB_Log',
        FILENAME = N'D:\SQLData\TradingSystemDB_Log.ldf',  -- ← 請修改為您的路徑
        SIZE = 1024MB,
        MAXSIZE = 10240MB,
        FILEGROWTH = 256MB
    );

    ALTER DATABASE [TradingSystemDB] SET RECOVERY SIMPLE;
    ALTER DATABASE [TradingSystemDB] SET MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT = ON;
END
GO
*/

-- 切換到目標資料庫
-- USE [TradingSystemDB];  -- ← 請取消註解並修改為您的資料庫名稱
-- GO

PRINT '';
PRINT '========================================';
PRINT '開始建立三表分離架構（含下單系統）...';
PRINT '========================================';
PRINT '';
GO

-- ============================================
-- 【第一層：HOT DATA】即時報價快照表
-- 特性: In-Memory OLTP, 極速讀寫, 不持久化
-- 用途: 下單價格驗證、即時報價查詢
-- ============================================

PRINT '>>> 第一層 (HOT): 建立即時報價快照表...';
GO

IF OBJECT_ID('dbo.StockQuotes_Snapshot', 'U') IS NOT NULL
    DROP TABLE dbo.StockQuotes_Snapshot;
GO

CREATE TABLE dbo.StockQuotes_Snapshot (
    StockCode NVARCHAR(10) COLLATE Latin1_General_100_BIN2 NOT NULL,

    -- 基本報價資訊
    CurrentPrice DECIMAL(18, 2) NOT NULL,
    YesterdayPrice DECIMAL(18, 2) NULL,
    OpenPrice DECIMAL(18, 2) NULL,
    HighPrice DECIMAL(18, 2) NULL,
    LowPrice DECIMAL(18, 2) NULL,

    -- 漲跌停價
    LimitUpPrice DECIMAL(18, 2) NULL,
    LimitDownPrice DECIMAL(18, 2) NULL,

    -- 第一檔買賣資訊（供下單驗證）
    BestBidPrice DECIMAL(18, 2) NULL,
    BestBidVolume BIGINT NULL,
    BestAskPrice DECIMAL(18, 2) NULL,
    BestAskVolume BIGINT NULL,

    -- 成交量
    TotalVolume BIGINT NULL,
    TotalValue DECIMAL(20, 2) NULL,

    -- 時間戳
    UpdateTime DATETIME2(3) NOT NULL,

    CONSTRAINT PK_StockQuotes_Snapshot PRIMARY KEY NONCLUSTERED HASH (StockCode)
    WITH (BUCKET_COUNT = 4096)
) WITH (
    MEMORY_OPTIMIZED = ON,
    DURABILITY = SCHEMA_ONLY
);
GO

PRINT '✓ 即時報價快照表建立完成';
GO

-- ============================================
-- 【第二層：WARM DATA】五檔委託簿表
-- 特性: In-Memory OLTP, 持久化, 當日資料
-- 用途: 五檔報價查詢、委託簿分析
-- ============================================

PRINT '>>> 第二層 (WARM): 建立五檔委託簿表...';
GO

IF OBJECT_ID('dbo.OrderBook_Levels', 'U') IS NOT NULL
    DROP TABLE dbo.OrderBook_Levels;
GO

CREATE TABLE dbo.OrderBook_Levels (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    StockCode NVARCHAR(10) COLLATE Latin1_General_100_BIN2 NOT NULL,

    -- 五檔資訊
    [Level] TINYINT NOT NULL,
    BidPrice DECIMAL(18, 2) NULL,
    BidVolume BIGINT NULL,
    AskPrice DECIMAL(18, 2) NULL,
    AskVolume BIGINT NULL,

    -- 序號與時間
    SequenceNo BIGINT NOT NULL,
    UpdateTime DATETIME2(6) NOT NULL,
    TradeDate DATE NOT NULL,

    CONSTRAINT PK_OrderBook_Levels PRIMARY KEY NONCLUSTERED HASH (Id)
    WITH (BUCKET_COUNT = 1048576),

    INDEX IX_OrderBook_StockCode NONCLUSTERED (StockCode, SequenceNo DESC)
) WITH (
    MEMORY_OPTIMIZED = ON,
    DURABILITY = SCHEMA_AND_DATA
);
GO

PRINT '✓ 五檔委託簿表建立完成';
GO

-- ============================================
-- 【第三層：COLD DATA】歷史Tick資料表
-- 特性: 磁碟儲存, 列式索引, 分區, 壓縮
-- 用途: 歷史分析、回測、合規稽核
-- ============================================

PRINT '>>> 第三層 (COLD): 建立歷史Tick資料表...';
GO

-- 建立分區函數
IF NOT EXISTS (SELECT * FROM sys.partition_functions WHERE name = 'PF_TicksByDate')
BEGIN
    DECLARE @StartDate DATE = CAST(DATEADD(DAY, -30, GETDATE()) AS DATE);
    DECLARE @EndDate DATE = CAST(DATEADD(DAY, 60, GETDATE()) AS DATE);
    DECLARE @CurrentDate DATE = @StartDate;
    DECLARE @SQL NVARCHAR(MAX) = N'CREATE PARTITION FUNCTION PF_TicksByDate (DATE) AS RANGE RIGHT FOR VALUES (';
    DECLARE @Values NVARCHAR(MAX) = N'';

    WHILE @CurrentDate <= @EndDate
    BEGIN
        IF @Values <> N'' SET @Values = @Values + N', ';
        SET @Values = @Values + N'''' + CONVERT(NVARCHAR(10), @CurrentDate, 120) + N'''';
        SET @CurrentDate = DATEADD(DAY, 1, @CurrentDate);
    END

    SET @SQL = @SQL + @Values + N');';
    EXEC sp_executesql @SQL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.partition_schemes WHERE name = 'PS_TicksByDate')
BEGIN
    CREATE PARTITION SCHEME PS_TicksByDate
    AS PARTITION PF_TicksByDate
    ALL TO ([PRIMARY]);
END
GO

IF OBJECT_ID('dbo.StockTicks_History', 'U') IS NOT NULL
    DROP TABLE dbo.StockTicks_History;
GO

CREATE TABLE dbo.StockTicks_History (
    TickId BIGINT IDENTITY(1,1) NOT NULL,
    StockCode NVARCHAR(10) NOT NULL,

    -- Tick 資料
    TickPrice DECIMAL(18, 2) NOT NULL,
    TickVolume BIGINT NOT NULL,
    TickType TINYINT NOT NULL,  -- 1:成交 2:委買 3:委賣

    -- 時間與分區鍵
    TickTime DATETIME2(6) NOT NULL,
    TradeDate DATE NOT NULL,

    CONSTRAINT PK_StockTicks_History PRIMARY KEY CLUSTERED (TradeDate, StockCode, TickId)
) ON PS_TicksByDate(TradeDate);
GO

CREATE NONCLUSTERED COLUMNSTORE INDEX IX_StockTicks_Columnstore
ON dbo.StockTicks_History (StockCode, TradeDate, TickTime, TickPrice, TickVolume, TickType);
GO

CREATE NONCLUSTERED INDEX IX_StockTicks_Query
ON dbo.StockTicks_History(StockCode, TradeDate, TickTime)
WITH (DATA_COMPRESSION = PAGE);
GO

PRINT '✓ 歷史Tick資料表建立完成';
GO

-- ============================================
-- 【下單系統 - 寫入端】委託下單表
-- 特性: 寫入優化, 極簡索引, 分區
-- 用途: 高頻下單寫入
-- ============================================

PRINT '>>> 建立下單系統: 委託下單表（寫入端）...';
GO

-- 建立分區函數（與 Tick 共用）
IF NOT EXISTS (SELECT * FROM sys.partition_schemes WHERE name = 'PS_OrdersByDate')
BEGIN
    CREATE PARTITION SCHEME PS_OrdersByDate
    AS PARTITION PF_TicksByDate
    ALL TO ([PRIMARY]);
END
GO

IF OBJECT_ID('dbo.Orders_Write', 'U') IS NOT NULL
    DROP TABLE dbo.Orders_Write;
GO

CREATE TABLE dbo.Orders_Write (
    OrderId BIGINT IDENTITY(1,1) NOT NULL,
    OrderSeq BIGINT NOT NULL,

    -- 基本資訊
    UserId INT NOT NULL,
    StockCode NVARCHAR(10) NOT NULL,
    OrderType TINYINT NOT NULL,  -- 1:限價 2:市價
    BuySell TINYINT NOT NULL,    -- 1:買 2:賣
    Price DECIMAL(18, 2) NULL,
    Quantity INT NOT NULL,

    -- 狀態
    OrderStatus TINYINT NOT NULL DEFAULT 1,  -- 1:新單 2:部分成交 3:完全成交 4:已取消
    CreateTime DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),

    -- 分區鍵
    TradeDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),

    CONSTRAINT PK_Orders_Write PRIMARY KEY CLUSTERED (TradeDate, OrderId)
) ON PS_OrdersByDate(TradeDate);
GO

-- 極簡索引
CREATE NONCLUSTERED INDEX IX_Orders_Write_UserId 
ON dbo.Orders_Write(UserId, TradeDate DESC, OrderStatus)
INCLUDE (OrderId, StockCode, OrderSeq)
WITH (FILLFACTOR = 70);
GO

CREATE NONCLUSTERED INDEX IX_Orders_Write_StockCode 
ON dbo.Orders_Write(StockCode, TradeDate DESC)
WHERE OrderStatus IN (1, 2)  -- Filtered Index: 只索引未完成訂單
WITH (FILLFACTOR = 70);
GO

PRINT '✓ 委託下單表建立完成';
GO

-- ============================================
-- 【下單系統 - 寫入端】成交明細表
-- 特性: 寫入優化, 分區
-- 用途: 記錄成交回報
-- ============================================

PRINT '>>> 建立下單系統: 成交明細表（寫入端）...';
GO

IF OBJECT_ID('dbo.Trades_Write', 'U') IS NOT NULL
    DROP TABLE dbo.Trades_Write;
GO

CREATE TABLE dbo.Trades_Write (
    TradeId BIGINT IDENTITY(1,1) NOT NULL,
    OrderId BIGINT NOT NULL,

    -- 成交資訊
    TradePrice DECIMAL(18, 2) NOT NULL,
    TradeQuantity INT NOT NULL,
    TradeTime DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),

    -- 手續費與稅（自動計算）
    Fee DECIMAL(18, 2) NULL,
    Tax DECIMAL(18, 2) NULL,

    -- 分區鍵
    TradeDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),

    CONSTRAINT PK_Trades_Write PRIMARY KEY CLUSTERED (TradeDate, TradeId)
) ON PS_OrdersByDate(TradeDate);
GO

CREATE NONCLUSTERED INDEX IX_Trades_Write_OrderId 
ON dbo.Trades_Write(OrderId, TradeDate)
INCLUDE (TradePrice, TradeQuantity, Fee, Tax)
WITH (FILLFACTOR = 70);
GO

PRINT '✓ 成交明細表建立完成';
GO

-- ============================================
-- 【下單系統 - 讀取端】委託查詢表
-- 特性: 讀取優化, 反正規化
-- 用途: 快速查詢使用者委託
-- ============================================

PRINT '>>> 建立下單系統: 委託查詢表（讀取端）...';
GO

IF OBJECT_ID('dbo.Orders_Read', 'U') IS NOT NULL
    DROP TABLE dbo.Orders_Read;
GO

CREATE TABLE dbo.Orders_Read (
    OrderId BIGINT NOT NULL,
    OrderSeq BIGINT NOT NULL,

    -- 完整資訊（反正規化）
    UserId INT NOT NULL,
    StockCode NVARCHAR(10) NOT NULL,
    StockName NVARCHAR(50) NULL,
    OrderType TINYINT NOT NULL,
    BuySell TINYINT NOT NULL,
    Price DECIMAL(18, 2) NULL,
    Quantity INT NOT NULL,

    -- 成交統計
    FilledQuantity INT NOT NULL DEFAULT 0,
    AvgTradePrice DECIMAL(18, 2) NULL,
    TotalTradeAmount DECIMAL(20, 2) NULL,
    TotalFee DECIMAL(18, 2) NULL,
    TotalTax DECIMAL(18, 2) NULL,
    TradeCount INT NOT NULL DEFAULT 0,

    -- 狀態與時間
    OrderStatus TINYINT NOT NULL,
    CreateTime DATETIME2(3) NOT NULL,
    UpdateTime DATETIME2(3) NOT NULL,

    -- 分區鍵
    TradeDate DATE NOT NULL,

    CONSTRAINT PK_Orders_Read PRIMARY KEY CLUSTERED (TradeDate, UserId, OrderId)
) ON PS_OrdersByDate(TradeDate);
GO

CREATE NONCLUSTERED INDEX IX_Orders_Read_StockCode 
ON dbo.Orders_Read(StockCode, TradeDate DESC, OrderStatus)
INCLUDE (OrderId, Price, Quantity, FilledQuantity)
WITH (FILLFACTOR = 90);
GO

CREATE NONCLUSTERED INDEX IX_Orders_Read_Status 
ON dbo.Orders_Read(UserId, OrderStatus, TradeDate DESC)
WHERE OrderStatus IN (1, 2)
WITH (FILLFACTOR = 90);
GO

PRINT '✓ 委託查詢表建立完成';
GO

-- ============================================
-- 【下單系統 - 讀取端】使用者持倉表
-- 特性: 讀取優化, 反正規化
-- 用途: 即時持倉查詢
-- ============================================

PRINT '>>> 建立下單系統: 使用者持倉表（讀取端）...';
GO

IF OBJECT_ID('dbo.Positions_Read', 'U') IS NOT NULL
    DROP TABLE dbo.Positions_Read;
GO

CREATE TABLE dbo.Positions_Read (
    UserId INT NOT NULL,
    StockCode NVARCHAR(10) NOT NULL,

    -- 持倉資訊
    Quantity BIGINT NOT NULL DEFAULT 0,
    AvgCost DECIMAL(18, 2) NOT NULL DEFAULT 0,
    TotalCost DECIMAL(20, 2) NOT NULL DEFAULT 0,

    -- 即時損益（定期更新）
    CurrentPrice DECIMAL(18, 2) NULL,
    MarketValue DECIMAL(20, 2) NULL,
    UnrealizedPL DECIMAL(20, 2) NULL,
    UnrealizedPLPercent DECIMAL(10, 4) NULL,

    -- 時間戳
    UpdateTime DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Positions_Read PRIMARY KEY CLUSTERED (UserId, StockCode)
);
GO

CREATE NONCLUSTERED INDEX IX_Positions_UserId
ON dbo.Positions_Read(UserId)
WHERE Quantity > 0;
GO

PRINT '✓ 使用者持倉表建立完成';
GO

-- ============================================
-- 【輔助表】股票主檔
-- ============================================

PRINT '>>> 建立輔助表: 股票主檔...';
GO

IF OBJECT_ID('dbo.StockMaster', 'U') IS NOT NULL
    DROP TABLE dbo.StockMaster;
GO

CREATE TABLE dbo.StockMaster (
    StockCode NVARCHAR(10) NOT NULL,
    StockName NVARCHAR(50) NOT NULL,
    StockNameEn NVARCHAR(100) NULL,
    Exchange NVARCHAR(10) NOT NULL,
    Industry NVARCHAR(50) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ListedDate DATE NULL,

    -- 交易設定
    LotSize INT NOT NULL DEFAULT 1000,  -- 交易單位（台股1張=1000股）
    AllowOddLot BIT NOT NULL DEFAULT 1,  -- 是否允許零股交易

    CONSTRAINT PK_StockMaster PRIMARY KEY CLUSTERED (StockCode)
);
GO

CREATE NONCLUSTERED INDEX IX_StockMaster_Exchange
ON dbo.StockMaster(Exchange, IsActive)
INCLUDE (StockName);
GO

PRINT '✓ 股票主檔建立完成';
GO

-- ============================================
-- 【輔助表】使用者帳戶資訊
-- ============================================

PRINT '>>> 建立輔助表: 使用者帳戶...';
GO

IF OBJECT_ID('dbo.UserAccounts', 'U') IS NOT NULL
    DROP TABLE dbo.UserAccounts;
GO

CREATE TABLE dbo.UserAccounts (
    UserId INT NOT NULL,
    UserName NVARCHAR(50) NOT NULL,

    -- 資金資訊
    AvailableBalance DECIMAL(20, 2) NOT NULL DEFAULT 0,  -- 可用餘額
    TotalBalance DECIMAL(20, 2) NOT NULL DEFAULT 0,      -- 總資產

    -- 風控設定
    MaxOrderAmount DECIMAL(20, 2) NULL,   -- 單筆最大金額
    MaxDailyAmount DECIMAL(20, 2) NULL,   -- 單日最大金額

    -- 狀態
    IsActive BIT NOT NULL DEFAULT 1,
    UpdateTime DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_UserAccounts PRIMARY KEY CLUSTERED (UserId)
);
GO

PRINT '✓ 使用者帳戶表建立完成';
GO

-- ============================================
-- 【輔助物件】序號產生器
-- ============================================

PRINT '>>> 建立序號產生器...';
GO

IF EXISTS (SELECT * FROM sys.sequences WHERE name = 'seq_OrderBookSequence')
    DROP SEQUENCE seq_OrderBookSequence;
GO

CREATE SEQUENCE seq_OrderBookSequence
    START WITH 1
    INCREMENT BY 1
    CACHE 10000;
GO

IF EXISTS (SELECT * FROM sys.sequences WHERE name = 'seq_OrderSequence')
    DROP SEQUENCE seq_OrderSequence;
GO

CREATE SEQUENCE seq_OrderSequence
    START WITH 1
    INCREMENT BY 1
    CACHE 10000;
GO

PRINT '✓ 序號產生器建立完成';
GO

-- ============================================
-- 【預存程序】報價系統
-- ============================================

PRINT '>>> 建立報價系統預存程序...';
GO

-- SP1: 初始化快照表
IF OBJECT_ID('dbo.sp_InitializeStockQuotes', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_InitializeStockQuotes;
GO

CREATE PROCEDURE dbo.sp_InitializeStockQuotes
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DELETE FROM dbo.StockQuotes_Snapshot;

        INSERT INTO dbo.StockQuotes_Snapshot (
            StockCode, CurrentPrice, YesterdayPrice, OpenPrice, 
            HighPrice, LowPrice, LimitUpPrice, LimitDownPrice,
            BestBidPrice, BestBidVolume, BestAskPrice, BestAskVolume,
            TotalVolume, TotalValue, UpdateTime
        )
        SELECT 
            StockCode, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, SYSDATETIME()
        FROM dbo.StockMaster WITH (NOLOCK)
        WHERE IsActive = 1;

        SELECT @@ROWCOUNT AS InitializedRows, 'SUCCESS' AS Status;
    END TRY
    BEGIN CATCH
        SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage, 'FAILED' AS Status;
    END CATCH
END;
GO

-- SP2: 高頻更新報價
IF OBJECT_ID('dbo.sp_UpdateStockQuote_Fast', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateStockQuote_Fast;
GO

CREATE PROCEDURE dbo.sp_UpdateStockQuote_Fast
    @StockCode NVARCHAR(10),
    @CurrentPrice DECIMAL(18, 2),
    @YesterdayPrice DECIMAL(18, 2) = NULL,
    @OpenPrice DECIMAL(18, 2) = NULL,
    @HighPrice DECIMAL(18, 2) = NULL,
    @LowPrice DECIMAL(18, 2) = NULL,
    @BestBidPrice DECIMAL(18, 2) = NULL,
    @BestBidVolume BIGINT = NULL,
    @BestAskPrice DECIMAL(18, 2) = NULL,
    @BestAskVolume BIGINT = NULL,
    @TotalVolume BIGINT = NULL,
    @TotalValue DECIMAL(20, 2) = NULL
WITH NATIVE_COMPILATION, SCHEMABINDING
AS
BEGIN ATOMIC WITH (
    TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    LANGUAGE = N'us_english'
)
    UPDATE dbo.StockQuotes_Snapshot
    SET 
        CurrentPrice = @CurrentPrice,
        YesterdayPrice = ISNULL(@YesterdayPrice, YesterdayPrice),
        OpenPrice = ISNULL(@OpenPrice, OpenPrice),
        HighPrice = CASE WHEN @HighPrice > HighPrice THEN @HighPrice ELSE HighPrice END,
        LowPrice = CASE WHEN @LowPrice < LowPrice OR LowPrice = 0 THEN @LowPrice ELSE LowPrice END,
        BestBidPrice = ISNULL(@BestBidPrice, BestBidPrice),
        BestBidVolume = ISNULL(@BestBidVolume, BestBidVolume),
        BestAskPrice = ISNULL(@BestAskPrice, BestAskPrice),
        BestAskVolume = ISNULL(@BestAskVolume, BestAskVolume),
        TotalVolume = ISNULL(@TotalVolume, TotalVolume),
        TotalValue = ISNULL(@TotalValue, TotalValue),
        UpdateTime = SYSDATETIME()
    WHERE StockCode = @StockCode;
END;
GO

-- SP3: 查詢報價
IF OBJECT_ID('dbo.sp_GetStockQuote', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStockQuote;
GO

CREATE PROCEDURE dbo.sp_GetStockQuote
    @StockCode NVARCHAR(10)
WITH NATIVE_COMPILATION, SCHEMABINDING
AS
BEGIN ATOMIC WITH (
    TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    LANGUAGE = N'us_english'
)
    SELECT 
        StockCode, CurrentPrice, YesterdayPrice, OpenPrice, HighPrice, LowPrice,
        LimitUpPrice, LimitDownPrice,
        BestBidPrice, BestBidVolume, BestAskPrice, BestAskVolume,
        TotalVolume, TotalValue, UpdateTime
    FROM dbo.StockQuotes_Snapshot
    WHERE StockCode = @StockCode;
END;
GO

-- SP4: 更新五檔
IF OBJECT_ID('dbo.sp_UpdateOrderBookLevels', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateOrderBookLevels;
GO

CREATE PROCEDURE dbo.sp_UpdateOrderBookLevels
    @StockCode NVARCHAR(10),
    @TradeDate DATE,
    @BidPrice1 DECIMAL(18,2), @BidVolume1 BIGINT, @AskPrice1 DECIMAL(18,2), @AskVolume1 BIGINT,
    @BidPrice2 DECIMAL(18,2), @BidVolume2 BIGINT, @AskPrice2 DECIMAL(18,2), @AskVolume2 BIGINT,
    @BidPrice3 DECIMAL(18,2), @BidVolume3 BIGINT, @AskPrice3 DECIMAL(18,2), @AskVolume3 BIGINT,
    @BidPrice4 DECIMAL(18,2), @BidVolume4 BIGINT, @AskPrice4 DECIMAL(18,2), @AskVolume4 BIGINT,
    @BidPrice5 DECIMAL(18,2), @BidVolume5 BIGINT, @AskPrice5 DECIMAL(18,2), @AskVolume5 BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SequenceNo BIGINT = NEXT VALUE FOR seq_OrderBookSequence;
    DECLARE @UpdateTime DATETIME2(6) = SYSDATETIME();

    BEGIN TRY
        INSERT INTO dbo.OrderBook_Levels (StockCode, [Level], BidPrice, BidVolume, AskPrice, AskVolume, SequenceNo, UpdateTime, TradeDate)
        VALUES 
            (@StockCode, 1, @BidPrice1, @BidVolume1, @AskPrice1, @AskVolume1, @SequenceNo, @UpdateTime, @TradeDate),
            (@StockCode, 2, @BidPrice2, @BidVolume2, @AskPrice2, @AskVolume2, @SequenceNo, @UpdateTime, @TradeDate),
            (@StockCode, 3, @BidPrice3, @BidVolume3, @AskPrice3, @AskVolume3, @SequenceNo, @UpdateTime, @TradeDate),
            (@StockCode, 4, @BidPrice4, @BidVolume4, @AskPrice4, @AskVolume4, @SequenceNo, @UpdateTime, @TradeDate),
            (@StockCode, 5, @BidPrice5, @BidVolume5, @AskPrice5, @AskVolume5, @SequenceNo, @UpdateTime, @TradeDate);

        SELECT @SequenceNo AS SequenceNo, 'SUCCESS' AS Status;
    END TRY
    BEGIN CATCH
        SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage, 'FAILED' AS Status;
    END CATCH
END;
GO

-- SP5: 查詢五檔
IF OBJECT_ID('dbo.sp_GetLatestOrderBook', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetLatestOrderBook;
GO

CREATE PROCEDURE dbo.sp_GetLatestOrderBook
    @StockCode NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    WITH LatestSequence AS (
        SELECT TOP 1 SequenceNo
        FROM dbo.OrderBook_Levels WITH (SNAPSHOT)
        WHERE StockCode = @StockCode
        ORDER BY SequenceNo DESC
    )
    SELECT ob.[Level], ob.BidPrice, ob.BidVolume, ob.AskPrice, ob.AskVolume, ob.UpdateTime
    FROM dbo.OrderBook_Levels ob WITH (SNAPSHOT)
    INNER JOIN LatestSequence ls ON ob.SequenceNo = ls.SequenceNo
    WHERE ob.StockCode = @StockCode
    ORDER BY ob.[Level];
END;
GO

PRINT '✓ 報價系統預存程序建立完成';
GO

-- ============================================
-- 【預存程序】下單系統（含驗證與手續費）
-- ============================================

PRINT '>>> 建立下單系統預存程序...';
GO

-- SP6: 下單（含完整驗證）
IF OBJECT_ID('dbo.sp_PlaceOrder', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PlaceOrder;
GO

CREATE PROCEDURE dbo.sp_PlaceOrder
    @UserId INT,
    @StockCode NVARCHAR(10),
    @OrderType TINYINT,
    @BuySell TINYINT,
    @Price DECIMAL(18, 2),
    @Quantity INT,
    @OrderId BIGINT OUTPUT,
    @OrderSeq BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- ========================================
        -- 1. 驗證股票是否存在且可交易
        -- ========================================
        DECLARE @IsActive BIT, @LotSize INT, @AllowOddLot BIT;

        SELECT 
            @IsActive = IsActive,
            @LotSize = LotSize,
            @AllowOddLot = AllowOddLot
        FROM dbo.StockMaster WITH (NOLOCK)
        WHERE StockCode = @StockCode;

        IF @IsActive IS NULL OR @IsActive = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS OrderId, 0 AS OrderSeq, 'STOCK_NOT_ACTIVE' AS Status, N'股票不存在或已停止交易' AS Message;
            RETURN;
        END

        -- ========================================
        -- 2. 驗證交易單位（整股/零股）
        -- ========================================
        IF @Quantity % @LotSize <> 0 AND @AllowOddLot = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS OrderId, 0 AS OrderSeq, 'INVALID_LOT_SIZE' AS Status, 
                   N'此股票不允許零股交易，數量必須為 ' + CAST(@LotSize AS NVARCHAR) + N' 的倍數' AS Message;
            RETURN;
        END

        -- ========================================
        -- 3. 查詢報價並驗證價格
        -- ========================================
        DECLARE @LimitUpPrice DECIMAL(18,2), @LimitDownPrice DECIMAL(18,2);
        DECLARE @BestBidPrice DECIMAL(18,2), @BestAskPrice DECIMAL(18,2);

        SELECT 
            @LimitUpPrice = LimitUpPrice,
            @LimitDownPrice = LimitDownPrice,
            @BestBidPrice = BestBidPrice,
            @BestAskPrice = BestAskPrice
        FROM dbo.StockQuotes_Snapshot WITH (SNAPSHOT)
        WHERE StockCode = @StockCode;

        -- 限價單驗證價格區間
        IF @OrderType = 1
        BEGIN
            IF @Price > @LimitUpPrice OR @Price < @LimitDownPrice
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT 0 AS OrderId, 0 AS OrderSeq, 'PRICE_OUT_OF_RANGE' AS Status,
                       N'委託價格超出漲跌停範圍 (' + CAST(@LimitDownPrice AS NVARCHAR) + ' ~ ' + CAST(@LimitUpPrice AS NVARCHAR) + ')' AS Message;
                RETURN;
            END
        END

        -- ========================================
        -- 4. 驗證使用者資金（買入）
        -- ========================================
        IF @BuySell = 1
        BEGIN
            DECLARE @AvailableBalance DECIMAL(20,2), @MaxOrderAmount DECIMAL(20,2);
            DECLARE @RequiredAmount DECIMAL(20,2);

            SELECT 
                @AvailableBalance = AvailableBalance,
                @MaxOrderAmount = MaxOrderAmount
            FROM dbo.UserAccounts WITH (NOLOCK)
            WHERE UserId = @UserId AND IsActive = 1;

            -- 計算所需金額（含預估手續費 0.1425%）
            SET @RequiredAmount = (@Price * @Quantity) * 1.001425;

            IF @AvailableBalance < @RequiredAmount
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT 0 AS OrderId, 0 AS OrderSeq, 'INSUFFICIENT_BALANCE' AS Status,
                       N'可用餘額不足。需要: ' + CAST(@RequiredAmount AS NVARCHAR) + N'，可用: ' + CAST(@AvailableBalance AS NVARCHAR) AS Message;
                RETURN;
            END

            -- 檢查單筆限額
            IF @MaxOrderAmount IS NOT NULL AND (@Price * @Quantity) > @MaxOrderAmount
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT 0 AS OrderId, 0 AS OrderSeq, 'EXCEED_MAX_ORDER_AMOUNT' AS Status,
                       N'超過單筆最大委託金額: ' + CAST(@MaxOrderAmount AS NVARCHAR) AS Message;
                RETURN;
            END
        END

        -- ========================================
        -- 5. 驗證庫存（賣出）
        -- ========================================
        IF @BuySell = 2
        BEGIN
            DECLARE @AvailableQty BIGINT;

            SELECT @AvailableQty = Quantity 
            FROM dbo.Positions_Read WITH (NOLOCK)
            WHERE UserId = @UserId AND StockCode = @StockCode;

            IF ISNULL(@AvailableQty, 0) < @Quantity
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT 0 AS OrderId, 0 AS OrderSeq, 'INSUFFICIENT_STOCK' AS Status,
                       N'持股不足。需要: ' + CAST(@Quantity AS NVARCHAR) + N'，可用: ' + CAST(ISNULL(@AvailableQty, 0) AS NVARCHAR) AS Message;
                RETURN;
            END
        END

        -- ========================================
        -- 6. 通過所有驗證，建立委託
        -- ========================================
        SET @OrderSeq = NEXT VALUE FOR seq_OrderSequence;

        INSERT INTO dbo.Orders_Write (OrderSeq, UserId, StockCode, OrderType, BuySell, Price, Quantity, OrderStatus, TradeDate)
        VALUES (@OrderSeq, @UserId, @StockCode, @OrderType, @BuySell, @Price, @Quantity, 1, CAST(GETDATE() AS DATE));

        SET @OrderId = SCOPE_IDENTITY();

        -- 同步到讀取表
        INSERT INTO dbo.Orders_Read (
            OrderId, OrderSeq, UserId, StockCode, StockName, OrderType, BuySell, 
            Price, Quantity, FilledQuantity, OrderStatus, CreateTime, UpdateTime, TradeDate
        )
        SELECT 
            @OrderId, @OrderSeq, @UserId, @StockCode, m.StockName, @OrderType, @BuySell,
            @Price, @Quantity, 0, 1, SYSDATETIME(), SYSDATETIME(), CAST(GETDATE() AS DATE)
        FROM dbo.StockMaster m WITH (NOLOCK)
        WHERE m.StockCode = @StockCode;

        COMMIT TRANSACTION;

        SELECT @OrderId AS OrderId, @OrderSeq AS OrderSeq, 'SUCCESS' AS Status, N'委託成功' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS OrderId, 0 AS OrderSeq, 'FAILED' AS Status, ERROR_MESSAGE() AS Message;
    END CATCH
END;
GO

-- SP7: 成交回報（含自動計算手續費）
IF OBJECT_ID('dbo.sp_ReportTrade', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ReportTrade;
GO

CREATE PROCEDURE dbo.sp_ReportTrade
    @OrderId BIGINT,
    @TradePrice DECIMAL(18, 2),
    @TradeQuantity INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 取得委託資訊
        DECLARE @UserId INT, @StockCode NVARCHAR(10), @BuySell TINYINT;
        DECLARE @Quantity INT, @FilledQuantity INT, @NewFilledQty INT;
        DECLARE @TotalAmount DECIMAL(20,2), @AvgPrice DECIMAL(18,2), @TradeCount INT;
        DECLARE @TotalFee DECIMAL(18,2), @TotalTax DECIMAL(18,2);

        SELECT 
            @UserId = UserId,
            @StockCode = StockCode,
            @BuySell = BuySell,
            @Quantity = Quantity,
            @FilledQuantity = FilledQuantity,
            @TotalAmount = ISNULL(TotalTradeAmount, 0),
            @TotalFee = ISNULL(TotalFee, 0),
            @TotalTax = ISNULL(TotalTax, 0),
            @TradeCount = TradeCount
        FROM dbo.Orders_Read WITH (NOLOCK)
        WHERE OrderId = @OrderId AND TradeDate = CAST(GETDATE() AS DATE);

        IF @UserId IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS TradeId, 'ORDER_NOT_FOUND' AS Status, N'找不到委託單' AS Message;
            RETURN;
        END

        -- ========================================
        -- 計算手續費與稅（台灣證券市場規則）
        -- ========================================
        DECLARE @Fee DECIMAL(18,2), @Tax DECIMAL(18,2);
        DECLARE @TradeAmount DECIMAL(20,2) = @TradePrice * @TradeQuantity;

        -- 手續費 = 成交金額 × 0.1425% (最低 20 元)
        SET @Fee = @TradeAmount * 0.001425;
        IF @Fee < 20 SET @Fee = 20;

        -- 證交稅 = 成交金額 × 0.3% (僅賣出收取)
        IF @BuySell = 2  -- 賣出
            SET @Tax = @TradeAmount * 0.003;
        ELSE
            SET @Tax = 0;

        -- ========================================
        -- 記錄成交明細
        -- ========================================
        INSERT INTO dbo.Trades_Write (OrderId, TradePrice, TradeQuantity, Fee, Tax, TradeDate)
        VALUES (@OrderId, @TradePrice, @TradeQuantity, @Fee, @Tax, CAST(GETDATE() AS DATE));

        DECLARE @TradeId BIGINT = SCOPE_IDENTITY();

        -- ========================================
        -- 更新委託狀態
        -- ========================================
        SET @NewFilledQty = @FilledQuantity + @TradeQuantity;
        SET @TotalAmount = @TotalAmount + @TradeAmount;
        SET @TotalFee = @TotalFee + @Fee;
        SET @TotalTax = @TotalTax + @Tax;
        SET @AvgPrice = @TotalAmount / @NewFilledQty;
        SET @TradeCount = @TradeCount + 1;

        UPDATE dbo.Orders_Read
        SET 
            FilledQuantity = @NewFilledQty,
            AvgTradePrice = @AvgPrice,
            TotalTradeAmount = @TotalAmount,
            TotalFee = @TotalFee,
            TotalTax = @TotalTax,
            TradeCount = @TradeCount,
            OrderStatus = CASE WHEN @NewFilledQty >= @Quantity THEN 3 ELSE 2 END,
            UpdateTime = SYSDATETIME()
        WHERE OrderId = @OrderId AND TradeDate = CAST(GETDATE() AS DATE);

        -- ========================================
        -- 更新持倉
        -- ========================================
        IF @BuySell = 1  -- 買入
        BEGIN
            MERGE dbo.Positions_Read AS target
            USING (SELECT @UserId AS UserId, @StockCode AS StockCode) AS source
            ON target.UserId = source.UserId AND target.StockCode = source.StockCode
            WHEN MATCHED THEN
                UPDATE SET 
                    Quantity = Quantity + @TradeQuantity,
                    TotalCost = TotalCost + @TradeAmount + @Fee + @Tax,
                    AvgCost = (TotalCost + @TradeAmount + @Fee + @Tax) / (Quantity + @TradeQuantity),
                    UpdateTime = SYSDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (UserId, StockCode, Quantity, AvgCost, TotalCost, UpdateTime)
                VALUES (@UserId, @StockCode, @TradeQuantity, 
                        (@TradeAmount + @Fee + @Tax) / @TradeQuantity,
                        @TradeAmount + @Fee + @Tax,
                        SYSDATETIME());
        END
        ELSE  -- 賣出
        BEGIN
            UPDATE dbo.Positions_Read
            SET 
                Quantity = Quantity - @TradeQuantity,
                UpdateTime = SYSDATETIME()
            WHERE UserId = @UserId AND StockCode = @StockCode;
        END

        COMMIT TRANSACTION;

        SELECT 
            @TradeId AS TradeId, 
            'SUCCESS' AS Status,
            N'成交記錄完成。手續費: ' + CAST(@Fee AS NVARCHAR) + N'，證交稅: ' + CAST(@Tax AS NVARCHAR) AS Message,
            @Fee AS Fee,
            @Tax AS Tax;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS TradeId, 'FAILED' AS Status, ERROR_MESSAGE() AS Message;
    END CATCH
END;
GO

-- SP8: 查詢使用者委託
IF OBJECT_ID('dbo.sp_GetUserOrders', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetUserOrders;
GO

CREATE PROCEDURE dbo.sp_GetUserOrders
    @UserId INT,
    @TradeDate DATE = NULL,
    @OrderStatus TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TradeDate IS NULL SET @TradeDate = CAST(GETDATE() AS DATE);

    SELECT 
        OrderId, OrderSeq, StockCode, StockName,
        OrderType, BuySell, Price, Quantity,
        FilledQuantity, AvgTradePrice, TotalTradeAmount,
        TotalFee, TotalTax,
        OrderStatus, CreateTime, UpdateTime
    FROM dbo.Orders_Read WITH (NOLOCK)
    WHERE UserId = @UserId
        AND TradeDate = @TradeDate
        AND (@OrderStatus IS NULL OR OrderStatus = @OrderStatus)
    ORDER BY OrderSeq DESC;
END;
GO

-- SP9: 查詢使用者持倉
IF OBJECT_ID('dbo.sp_GetUserPositions', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetUserPositions;
GO

CREATE PROCEDURE dbo.sp_GetUserPositions
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.StockCode,
        m.StockName,
        p.Quantity,
        p.AvgCost,
        p.TotalCost,
        q.CurrentPrice,
        p.Quantity * q.CurrentPrice AS MarketValue,
        (p.Quantity * q.CurrentPrice) - p.TotalCost AS UnrealizedPL,
        CASE WHEN p.TotalCost > 0 
            THEN (((p.Quantity * q.CurrentPrice) - p.TotalCost) / p.TotalCost) * 100
            ELSE 0 END AS UnrealizedPLPercent,
        p.UpdateTime
    FROM dbo.Positions_Read p WITH (NOLOCK)
    LEFT JOIN dbo.StockQuotes_Snapshot q WITH (SNAPSHOT) ON p.StockCode = q.StockCode
    LEFT JOIN dbo.StockMaster m WITH (NOLOCK) ON p.StockCode = m.StockCode
    WHERE p.UserId = @UserId AND p.Quantity > 0;
END;
GO

PRINT '✓ 下單系統預存程序建立完成';
GO

-- ============================================
-- 【預存程序】資料維護
-- ============================================

PRINT '>>> 建立資料維護預存程序...';
GO

-- SP10: 歸檔歷史資料
IF OBJECT_ID('dbo.sp_ArchiveToHistory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ArchiveToHistory;
GO

CREATE PROCEDURE dbo.sp_ArchiveToHistory
    @ArchiveDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RowCount INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.StockTicks_History (StockCode, TickPrice, TickVolume, TickType, TickTime, TradeDate)
        SELECT StockCode, BidPrice, BidVolume, 2, UpdateTime, TradeDate
        FROM dbo.OrderBook_Levels WITH (SNAPSHOT)
        WHERE TradeDate = @ArchiveDate AND BidPrice IS NOT NULL
        UNION ALL
        SELECT StockCode, AskPrice, AskVolume, 3, UpdateTime, TradeDate
        FROM dbo.OrderBook_Levels WITH (SNAPSHOT)
        WHERE TradeDate = @ArchiveDate AND AskPrice IS NOT NULL;

        SET @RowCount = @@ROWCOUNT;

        DELETE FROM dbo.OrderBook_Levels WHERE TradeDate = @ArchiveDate;

        COMMIT TRANSACTION;

        SELECT @RowCount AS ArchivedRows, @ArchiveDate AS ArchiveDate, 'SUCCESS' AS Status;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage, 'FAILED' AS Status;
    END CATCH
END;
GO

-- SP11: 清理舊資料（7年保留政策）
IF OBJECT_ID('dbo.sp_PurgeOldHistory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PurgeOldHistory;
GO

CREATE PROCEDURE dbo.sp_PurgeOldHistory
    @RetentionYears INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CutoffDate DATE = CAST(DATEADD(YEAR, -@RetentionYears, GETDATE()) AS DATE);
    DECLARE @DeletedRows INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.StockTicks_History WHERE TradeDate < @CutoffDate;
        SET @DeletedRows = @@ROWCOUNT;

        DELETE FROM dbo.Orders_Write WHERE TradeDate < @CutoffDate;
        DELETE FROM dbo.Orders_Read WHERE TradeDate < @CutoffDate;
        DELETE FROM dbo.Trades_Write WHERE TradeDate < @CutoffDate;

        COMMIT TRANSACTION;

        SELECT @DeletedRows AS DeletedRows, @CutoffDate AS CutoffDate, 'SUCCESS' AS Status;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage, 'FAILED' AS Status;
    END CATCH
END;
GO

-- SP12: 分區管理
IF OBJECT_ID('dbo.sp_ManagePartitions', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ManagePartitions;
GO

CREATE PROCEDURE dbo.sp_ManagePartitions
    @DaysAhead INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FutureDate DATE = DATEADD(DAY, @DaysAhead, GETDATE());
    DECLARE @SQL NVARCHAR(MAX);

    IF NOT EXISTS (
        SELECT 1 FROM sys.partition_range_values prv
        INNER JOIN sys.partition_functions pf ON prv.function_id = pf.function_id
        WHERE pf.name = 'PF_TicksByDate' AND CAST(prv.value AS DATE) = @FutureDate
    )
    BEGIN
        SET @SQL = N'ALTER PARTITION SCHEME PS_TicksByDate NEXT USED [PRIMARY]; 
                     ALTER PARTITION FUNCTION PF_TicksByDate() SPLIT RANGE (''' + 
                     CONVERT(NVARCHAR(10), @FutureDate, 120) + ''');';
        EXEC sp_executesql @SQL;
        PRINT '✓ 已新增分區: ' + CONVERT(NVARCHAR(10), @FutureDate, 120);
    END
    ELSE
        PRINT '✓ 分區已存在: ' + CONVERT(NVARCHAR(10), @FutureDate, 120);
END;
GO

PRINT '✓ 資料維護預存程序建立完成';
GO

-- ============================================
-- 【視圖】
-- ============================================

PRINT '>>> 建立視圖...';
GO

-- 視圖1: 即時報價總覽
IF OBJECT_ID('dbo.vw_StockQuotes_RealTime', 'V') IS NOT NULL
    DROP VIEW dbo.vw_StockQuotes_RealTime;
GO

CREATE VIEW dbo.vw_StockQuotes_RealTime
AS
SELECT 
    s.StockCode, m.StockName, s.CurrentPrice, s.YesterdayPrice,
    s.CurrentPrice - s.YesterdayPrice AS PriceChange,
    CASE WHEN s.YesterdayPrice > 0 
        THEN ((s.CurrentPrice - s.YesterdayPrice) / s.YesterdayPrice) * 100 
        ELSE 0 END AS PriceChangePercent,
    s.OpenPrice, s.HighPrice, s.LowPrice,
    s.BestBidPrice, s.BestBidVolume, s.BestAskPrice, s.BestAskVolume,
    s.BestAskPrice - s.BestBidPrice AS Spread,
    s.TotalVolume, s.TotalValue, s.UpdateTime
FROM dbo.StockQuotes_Snapshot s
LEFT JOIN dbo.StockMaster m ON s.StockCode = m.StockCode;
GO

-- 視圖2: 使用者持倉總覽
IF OBJECT_ID('dbo.vw_UserPositions_Summary', 'V') IS NOT NULL
    DROP VIEW dbo.vw_UserPositions_Summary;
GO

CREATE VIEW dbo.vw_UserPositions_Summary
AS
SELECT 
    p.UserId,
    u.UserName,
    COUNT(*) AS PositionCount,
    SUM(p.TotalCost) AS TotalInvestment,
    SUM(p.Quantity * q.CurrentPrice) AS TotalMarketValue,
    SUM((p.Quantity * q.CurrentPrice) - p.TotalCost) AS TotalUnrealizedPL,
    CASE WHEN SUM(p.TotalCost) > 0
        THEN (SUM((p.Quantity * q.CurrentPrice) - p.TotalCost) / SUM(p.TotalCost)) * 100
        ELSE 0 END AS TotalReturnPercent
FROM dbo.Positions_Read p
LEFT JOIN dbo.StockQuotes_Snapshot q ON p.StockCode = q.StockCode
LEFT JOIN dbo.UserAccounts u ON p.UserId = u.UserId
WHERE p.Quantity > 0
GROUP BY p.UserId, u.UserName;
GO

PRINT '✓ 視圖建立完成';
GO

-- ============================================
-- 完成建置
-- ============================================

PRINT '';
PRINT '========================================';
PRINT '✓ 三表分離架構（含下單系統）建立完成！';
PRINT '========================================';
PRINT '';
PRINT '已建立物件摘要:';
PRINT '  【報價系統 - 三層分離】';
PRINT '    HOT:  StockQuotes_Snapshot (記憶體優化)';
PRINT '    WARM: OrderBook_Levels (記憶體優化)';
PRINT '    COLD: StockTicks_History (分區 + Columnstore)';
PRINT '  【下單系統 - CQRS + 完整驗證】';
PRINT '    寫入: Orders_Write, Trades_Write';
PRINT '    讀取: Orders_Read, Positions_Read';
PRINT '    驗證: 價格/庫存/資金/交易單位';
PRINT '    手續費: 自動計算（0.1425% + 證交稅0.3%）';
PRINT '  【輔助物件】';
PRINT '    - StockMaster (股票主檔)';
PRINT '    - UserAccounts (使用者帳戶)';
PRINT '    - 2個序號產生器';
PRINT '  【預存程序】12個';
PRINT '  【視圖】2個';
PRINT '';
PRINT '初始化範例:';
PRINT '  -- 1. 建立測試使用者';
PRINT '  INSERT INTO UserAccounts (UserId, UserName, AvailableBalance, TotalBalance, MaxOrderAmount)';
PRINT '  VALUES (1, N''測試使用者'', 1000000, 1000000, 500000);';
PRINT '';
PRINT '  -- 2. 建立股票主檔';
PRINT '  INSERT INTO StockMaster (StockCode, StockName, StockNameEn, Exchange, Industry, LotSize, AllowOddLot)';
PRINT '  VALUES (''2330'', N''台積電'', ''TSMC'', ''TWSE'', N''半導體'', 1000, 1);';
PRINT '';
PRINT '  -- 3. 初始化報價';
PRINT '  EXEC sp_InitializeStockQuotes;';
PRINT '';
PRINT '  -- 4. 更新報價';
PRINT '  EXEC sp_UpdateStockQuote_Fast @StockCode=''2330'', @CurrentPrice=585, @YesterdayPrice=580,';
PRINT '       @LimitUpPrice=638, @LimitDownPrice=522, @BestBidPrice=584.5, @BestAskPrice=585.5;';
PRINT '';
PRINT '  -- 5. 測試下單';
PRINT '  DECLARE @OrderId BIGINT, @OrderSeq BIGINT;';
PRINT '  EXEC sp_PlaceOrder @UserId=1, @StockCode=''2330'', @OrderType=1, @BuySell=1,';
PRINT '       @Price=585, @Quantity=1000, @OrderId=@OrderId OUTPUT, @OrderSeq=@OrderSeq OUTPUT;';
PRINT '';
PRINT '  -- 6. 測試成交（手續費自動計算）';
PRINT '  EXEC sp_ReportTrade @OrderId=1, @TradePrice=585, @TradeQuantity=1000;';
PRINT '';
PRINT '========================================';
GO

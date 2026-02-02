-- Performance Optimization: Database Indexes
-- Run this script after initial database setup to optimize query performance

USE TradingDb;
GO

-- StockMaster indexes (already created via EF Core, but included for reference)
-- PRIMARY KEY: StockCode (Clustered)
-- Non-Clustered: Exchange, IsActive

-- StockQuotesSnapshot indexes (In-Memory OLTP table)
-- PRIMARY KEY: StockCode (Non-Clustered Hash)
-- Non-Clustered: UpdateTime

-- OrdersWrite indexes (In-Memory OLTP table)
-- PRIMARY KEY: OrderId (Non-Clustered Hash)
-- Non-Clustered: (UserId, TradeDate), StockCode

-- OrdersRead indexes (Traditional table)
-- PRIMARY KEY: OrderId (Clustered)

-- Additional performance indexes for OrdersRead
CREATE NONCLUSTERED INDEX IX_OrdersRead_UserId_TradeDate_Include
ON Orders_Read (UserId, TradeDate DESC)
INCLUDE (StockCode, OrderType, Price, Quantity, OrderStatus, CreatedAt);
GO

CREATE NONCLUSTERED INDEX IX_OrdersRead_StockCode_Include
ON Orders_Read (StockCode)
INCLUDE (OrderType, Price, Quantity, OrderStatus, TradeDate, CreatedAt);
GO

CREATE NONCLUSTERED INDEX IX_OrdersRead_CreatedAt
ON Orders_Read (CreatedAt DESC);
GO

-- Statistics update
UPDATE STATISTICS StockMaster WITH FULLSCAN;
UPDATE STATISTICS Orders_Read WITH FULLSCAN;
GO

PRINT 'Performance indexes created successfully';

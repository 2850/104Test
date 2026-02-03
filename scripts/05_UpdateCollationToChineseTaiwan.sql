-- Update database collation to Chinese_Taiwan_Stroke_CI_AS
-- This script updates the database default collation and all nvarchar columns to use Traditional Chinese collation

-- First, set the database to single user mode to change collation
USE master;
GO

ALTER DATABASE [TradingSystemDB_Dev] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

-- Change database collation
ALTER DATABASE [TradingSystemDB_Dev] COLLATE Chinese_Taiwan_Stroke_CI_AS;
GO

-- Set back to multi-user mode
ALTER DATABASE [TradingSystemDB_Dev] SET MULTI_USER;
GO

USE [TradingSystemDB_Dev];
GO

-- Drop foreign key constraints
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Orders_Write_StockMaster_StockCode')
    ALTER TABLE [Orders_Write] DROP CONSTRAINT [FK_Orders_Write_StockMaster_StockCode];
GO

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Orders_Read_StockMaster_StockMasterStockCode')
    ALTER TABLE [Orders_Read] DROP CONSTRAINT [FK_Orders_Read_StockMaster_StockMasterStockCode];
GO

-- Drop primary key on StockMaster
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_StockMaster')
    ALTER TABLE [StockMaster] DROP CONSTRAINT [PK_StockMaster];
GO

-- Drop indexes that depend on columns we're changing
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMaster_Exchange')
    DROP INDEX [IX_StockMaster_Exchange] ON [StockMaster];
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Write_StockCode')
    DROP INDEX [IX_Orders_Write_StockCode] ON [Orders_Write];
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Read_StockCode')
    DROP INDEX [IX_Orders_Read_StockCode] ON [Orders_Read];
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrdersRead_UserId_TradeDate_Include')
    DROP INDEX [IX_OrdersRead_UserId_TradeDate_Include] ON [Orders_Read];
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrdersRead_StockCode_Include')
    DROP INDEX [IX_OrdersRead_StockCode_Include] ON [Orders_Read];
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Read_StockMasterStockCode')
    DROP INDEX [IX_Orders_Read_StockMasterStockCode] ON [Orders_Read];
GO

-- Update StockMaster columns
ALTER TABLE [StockMaster] ALTER COLUMN [StockCode] nvarchar(10) COLLATE Chinese_Taiwan_Stroke_CI_AS NOT NULL;
ALTER TABLE [StockMaster] ALTER COLUMN [StockName] nvarchar(100) COLLATE Chinese_Taiwan_Stroke_CI_AS NOT NULL;
ALTER TABLE [StockMaster] ALTER COLUMN [StockNameShort] nvarchar(50) COLLATE Chinese_Taiwan_Stroke_CI_AS NOT NULL;
ALTER TABLE [StockMaster] ALTER COLUMN [StockNameEn] nvarchar(200) COLLATE Chinese_Taiwan_Stroke_CI_AS NULL;
ALTER TABLE [StockMaster] ALTER COLUMN [Exchange] nvarchar(10) COLLATE Chinese_Taiwan_Stroke_CI_AS NOT NULL;
ALTER TABLE [StockMaster] ALTER COLUMN [Industry] nvarchar(50) COLLATE Chinese_Taiwan_Stroke_CI_AS NULL;
GO

-- Recreate primary key on StockMaster
ALTER TABLE [StockMaster] ADD CONSTRAINT [PK_StockMaster] PRIMARY KEY ([StockCode]);
GO

-- Update Orders_Write columns
ALTER TABLE [Orders_Write] ALTER COLUMN [StockCode] nvarchar(10) COLLATE Chinese_Taiwan_Stroke_CI_AS NOT NULL;
GO

-- Update Orders_Read columns
ALTER TABLE [Orders_Read] ALTER COLUMN [StockCode] nvarchar(10) COLLATE Chinese_Taiwan_Stroke_CI_AS NOT NULL;
ALTER TABLE [Orders_Read] ALTER COLUMN [StockName] nvarchar(100) COLLATE Chinese_Taiwan_Stroke_CI_AS NULL;
ALTER TABLE [Orders_Read] ALTER COLUMN [StockNameShort] nvarchar(50) COLLATE Chinese_Taiwan_Stroke_CI_AS NULL;
ALTER TABLE [Orders_Read] ALTER COLUMN [UserName] nvarchar(100) COLLATE Chinese_Taiwan_Stroke_CI_AS NULL;
ALTER TABLE [Orders_Read] ALTER COLUMN [OrderTypeName] nvarchar(20) COLLATE Chinese_Taiwan_Stroke_CI_AS NULL;
ALTER TABLE [Orders_Read] ALTER COLUMN [OrderStatusName] nvarchar(20) COLLATE Chinese_Taiwan_Stroke_CI_AS NULL;
ALTER TABLE [Orders_Read] ALTER COLUMN [StockMasterStockCode] nvarchar(10) COLLATE Chinese_Taiwan_Stroke_CI_AS NULL;
GO

-- Recreate foreign key constraints
ALTER TABLE [Orders_Write] ADD CONSTRAINT [FK_Orders_Write_StockMaster_StockCode] 
    FOREIGN KEY ([StockCode]) REFERENCES [StockMaster] ([StockCode]) ON DELETE NO ACTION;
GO

ALTER TABLE [Orders_Read] ADD CONSTRAINT [FK_Orders_Read_StockMaster_StockMasterStockCode] 
    FOREIGN KEY ([StockMasterStockCode]) REFERENCES [StockMaster] ([StockCode]);
GO

-- Recreate indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMaster_Exchange')
    CREATE INDEX [IX_StockMaster_Exchange] ON [StockMaster] ([Exchange]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Write_StockCode')
    CREATE INDEX [IX_Orders_Write_StockCode] ON [Orders_Write] ([StockCode]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Read_StockCode')
    CREATE INDEX [IX_Orders_Read_StockCode] ON [Orders_Read] ([StockCode]);
GO

PRINT 'Database collation updated to Chinese_Taiwan_Stroke_CI_AS successfully!';
GO

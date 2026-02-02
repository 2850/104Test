-- Create sequence for OrderId generation
CREATE SEQUENCE seq_OrderSequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    NO CACHE;
GO

-- Add memory-optimized filegroup (required for In-Memory OLTP)
-- Note: This script assumes the database already exists
-- Run this before applying EF Core migrations

ALTER DATABASE [TradingDb] ADD FILEGROUP [MemoryOptimizedData] CONTAINS MEMORY_OPTIMIZED_DATA;
GO

-- Add a file to the memory-optimized filegroup
-- Adjust the path as needed for your environment
ALTER DATABASE [TradingDb] ADD FILE (
    NAME = N'TradingDb_MemoryOptimizedData',
    FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\TradingDb_MemoryOptimizedData'
) TO FILEGROUP [MemoryOptimizedData];
GO

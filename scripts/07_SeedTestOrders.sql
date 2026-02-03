-- ========================================
-- 07_SeedTestOrders.sql
-- 建立測試訂單資料（關聯到測試使用者）
-- ========================================

USE TradingSystemDB_Dev;
GO

-- 確保 Users 表格有資料
IF NOT EXISTS (SELECT * FROM Users WHERE UserId IN (1, 2, 3))
BEGIN
    PRINT 'Error: Please run 06_SeedUsers.sql first to create test users.';
    RETURN;
END
GO

-- 清除現有測試訂單（保留可能的真實資料）
-- 這裡假設 UserId NULL 的是舊資料，不刪除
DELETE FROM Orders_Read WHERE UserId IN (1, 2, 3);
DELETE FROM Orders_Write WHERE UserId IN (1, 2, 3);
GO



PRINT 'Inserting test orders for admin (UserId=1)...';

-- Admin 使用者的訂單 (3-5 筆)
INSERT INTO Orders_Write (UserId, StockCode, BuySell, Quantity, Price, OrderStatus, CreatedAt,OrderId,OrderType,TradeDate,OrderSeq)
VALUES 
    (1, '2317', 2, 500, 125.50, 1, DATEADD(day, -3, GETUTCDATE()),'1000000572',1,GETDATE(),'1000000572'),
    (1, '2454', 1, 2000, 92.30, 1, DATEADD(day, -1, GETUTCDATE()),'1000000573',1,GETDATE(),'1000000573'),
    (1, '2881', 1, 5000, 23.45,1, GETUTCDATE(),'1000000574',1,GETDATE(),'1000000574');

PRINT 'Inserting test orders for user1 (UserId=2)...';

-- User1 的訂單 (5-8 筆)
INSERT INTO Orders_Write (UserId, StockCode, BuySell, Quantity, Price, OrderStatus, CreatedAt,OrderId,OrderType,TradeDate,OrderSeq)
VALUES 
    (2, '2330', 1, 500, 582.00, 1, DATEADD(day, -7, GETUTCDATE()),'1000000575',1,GETDATE(),'1000000575'),
    (2, '2317', 1, 300, 124.00, 1, DATEADD(day, -6, GETUTCDATE()),'1000000576',1,GETDATE(),'1000000576'),
    (2, '2454', 2, 1000, 93.50, 1, DATEADD(day, -4, GETUTCDATE()),'1000000577',1,GETDATE(),'1000000577'),
    (2, '2881', 1, 3000, 23.20, 1, DATEADD(day, -2, GETUTCDATE()),'1000000578',1,GETDATE(),'1000000578'),
    (2, '2330', 2, 200, 590.00, 1, DATEADD(hour, -12, GETUTCDATE()),'1000000579',1,GETDATE(),'1000000579'),
    (2, '2317', 1, 400, 126.00, 1, DATEADD(hour, -6, GETUTCDATE()),'1000000580',1,GETDATE(),'1000000580'),
    (2, '2454', 1, 1500, 91.80, 1, DATEADD(hour, -3, GETUTCDATE()),'1000000581',1,GETDATE(),'1000000581');

PRINT 'Inserting test orders for user2 (UserId=3)...';

-- User2 的訂單 (3-5 筆)
INSERT INTO Orders_Write (UserId, StockCode, BuySell, Quantity, Price, OrderStatus, CreatedAt,OrderId,OrderType,TradeDate,OrderSeq)
VALUES 
    (3, '2330', 1, 800, 583.50,1, DATEADD(day, -8, GETUTCDATE()),'1000000582',1,GETDATE(),'1000000582'),
    (3, '2454', 1, 1200, 92.80, 1, DATEADD(day, -5, GETUTCDATE()),'1000000583',1,GETDATE(),'1000000583'),
    (3, '2881', 2, 4000, 23.60, 1, DATEADD(day, -2, GETUTCDATE()),'1000000584',1,GETDATE(),'1000000584'),
    (3, '2317', 1, 600, 125.00, 1, DATEADD(hour, -8, GETUTCDATE()),'1000000585',1,GETDATE(),'1000000585');

GO

-- 同步到 Orders_Read（觸發器應該會自動處理，但這裡手動同步確保一致性）
-- 如果有觸發器，這段可以省略
INSERT INTO Orders_Read (OrderId, UserId, StockCode, BuySell, Quantity, Price, OrderStatus, CreatedAt,OrderType,TradeDate,OrderSeq)
SELECT OrderId, UserId, StockCode, BuySell, Quantity, Price, OrderStatus, CreatedAt,OrderType,TradeDate,OrderSeq
FROM Orders_Write
WHERE UserId IN (1, 2, 3)
AND OrderId NOT IN (SELECT OrderId FROM Orders_Read WHERE UserId IN (1, 2, 3));
GO

-- 顯示統計
PRINT '';
PRINT 'Test orders inserted successfully!';
PRINT '';
PRINT 'Orders by User:';
SELECT 
    u.Username,
    COUNT(o.OrderId) AS TotalOrders,
    SUM(CASE WHEN o.OrderStatus = 1 THEN 1 ELSE 0 END) AS FilledOrders,
    SUM(CASE WHEN o.OrderStatus = 1 THEN 1 ELSE 0 END) AS PendingOrders,
    SUM(CASE WHEN o.OrderStatus = 1 THEN 1 ELSE 0 END) AS PartiallyFilledOrders,
    SUM(CASE WHEN o.OrderStatus = 1 THEN 1 ELSE 0 END) AS CancelledOrders
FROM Users u
LEFT JOIN Orders_Read o ON u.UserId = o.UserId
WHERE u.UserId IN (1, 2, 3)
GROUP BY u.Username
ORDER BY u.Username;
GO

PRINT '';
PRINT 'Sample orders:';
SELECT TOP 5
    o.OrderId,
    u.Username,
    o.StockCode,
    o.BuySell,
    o.Quantity,
    o.Price,
    o.OrderStatus,
    o.CreatedAt
FROM Orders_Read o
INNER JOIN Users u ON o.UserId = u.UserId
WHERE u.UserId IN (1, 2, 3)
ORDER BY o.CreatedAt DESC;
GO

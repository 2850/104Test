-- ========================================
-- 06_SeedUsers.sql
-- 建立測試使用者資料
-- ========================================
-- 
-- 密碼說明：
-- admin: Admin@123
-- user1: User1@123  
-- user2: User2@123
--
-- 注意：以下的 PasswordHash 是使用 PasswordHasher.HashPassword() 預先產生的
-- 格式：Base64Salt:Base64Hash
-- ========================================

USE TradingSystemDB_Dev;
GO

-- 確保表格存在
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    PRINT 'Error: Users table does not exist. Please run the migration first.';
    RETURN;
END
GO

-- 清除現有測試資料
DELETE FROM RefreshTokens;
DELETE FROM Users;
GO

-- 重置自動編號（如果需要）
DBCC CHECKIDENT ('Users', RESEED, 0);
GO

-- 插入測試使用者
-- Note: PasswordHash 需要在實際執行前使用 PasswordHasher.HashPassword() 產生
-- 這裡使用固定的 hash 值作為範例，實際環境中應該動態產生

SET IDENTITY_INSERT Users ON;
GO

INSERT INTO Users (UserId, Username, PasswordHash, Role, CreatedAt, UpdatedAt)
VALUES 
(
    1, 
    'admin', 
    'RdUlcSjy4ATMtcnSQYJ/uCNs2kInipcZeNvvCR0KFzg=:GHD06d4gR/1Ggn8e4eh7DL/qjFSQdcfjSygWvZ6qH/g=',
    1,
    GETUTCDATE(),
    NULL
),
(
    2,
    'user1',
    'SZjEmmGSCk4bdaE+0ZHdFvSBY/1s1lGHGIw0/KOpz4w=:WJDM4ZuLiaQjhkW2e5ES2G7dLGn5CKpu8oAKky+OAYs=',
    2,
    GETUTCDATE(),
    NULL
),
(
    3,
    'user2',
    '7BAWmJ3TJhZO7m6px71kWDb502Bbcrh57NxVXzvRJpk=:WRLlwxWLJikwWTf6EY/NCkT2fMOe33MC5sbFyDPpIfI=',
    2,
    GETUTCDATE(),
    NULL
);

SET IDENTITY_INSERT Users OFF;
GO

-- 驗證資料
SELECT 
    UserId,
    Username,
    LEFT(PasswordHash, 20) + '...' AS PasswordHash,
    Role,
    CASE Role
        WHEN 1 THEN 'Admin'
        WHEN 2 THEN 'User'
        ELSE 'Unknown'
    END AS RoleName,
    CreatedAt
FROM Users
ORDER BY UserId;
GO

PRINT 'Successfully seeded 3 test users.';
PRINT '';
PRINT 'Test Users:';
PRINT '  Username: admin  | Password: Admin@123 | Role: Admin';
PRINT '  Username: user1  | Password: User1@123 | Role: User';
PRINT '  Username: user2  | Password: User2@123 | Role: User';
PRINT '';
GO

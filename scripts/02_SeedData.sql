-- Seed StockMaster data from Taiwan Stock Exchange
-- This is sample data for common Taiwan stocks
-- In production, this should be loaded from t187ap03_L.csv

INSERT INTO StockMaster (StockCode, StockName, StockNameShort, StockNameEn, Exchange, Industry, LotSize, AllowOddLot, IsActive, ListedDate, CreatedAt, UpdatedAt)
VALUES
    ('2330', '台積電', '台積電', 'Taiwan Semiconductor Manufacturing Company', 'TWSE', '半導體業', 1000, 1, 1, '1994-09-05', GETUTCDATE(), GETUTCDATE()),
    ('2317', '鴻海', '鴻海', 'Hon Hai Precision Industry', 'TWSE', '電子業', 1000, 1, 1, '1991-06-15', GETUTCDATE(), GETUTCDATE()),
    ('2454', '聯發科', '聯發科', 'MediaTek Inc.', 'TWSE', '半導體業', 1000, 1, 1, '2001-07-23', GETUTCDATE(), GETUTCDATE()),
    ('2882', '國泰金', '國泰金', 'Cathay Financial Holding', 'TWSE', '金融業', 1000, 1, 1, '2001-12-28', GETUTCDATE(), GETUTCDATE()),
    ('2881', '富邦金', '富邦金', 'Fubon Financial Holding', 'TWSE', '金融業', 1000, 1, 1, '2001-12-19', GETUTCDATE(), GETUTCDATE()),
    ('2412', '中華電', '中華電', 'Chunghwa Telecom', 'TWSE', '通信網路業', 1000, 1, 1, '2000-10-23', GETUTCDATE(), GETUTCDATE()),
    ('2303', '聯電', '聯電', 'United Microelectronics Corporation', 'TWSE', '半導體業', 1000, 1, 1, '1985-08-22', GETUTCDATE(), GETUTCDATE()),
    ('2886', '兆豐金', '兆豐金', 'Mega Financial Holding', 'TWSE', '金融業', 1000, 1, 1, '2002-02-04', GETUTCDATE(), GETUTCDATE()),
    ('1301', '台塑', '台塑', 'Formosa Plastics Corporation', 'TWSE', '塑膠工業', 1000, 1, 1, '1962-02-09', GETUTCDATE(), GETUTCDATE()),
    ('2891', '中信金', '中信金', 'CTBC Financial Holding', 'TWSE', '金融業', 1000, 1, 1, '2002-05-17', GETUTCDATE(), GETUTCDATE());
GO

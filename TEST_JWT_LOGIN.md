# JWT 登入測試指南

## 問題已修復 ✅

原本的錯誤：
```
System.FormatException: The input is not a valid Base-64 string
```

**原因**：資料庫中的密碼雜湊格式不正確

**解決方案**：
1. 使用 `GeneratePasswordHashes.ps1` 產生正確的密碼雜湊
2. 更新 `06_SeedUsers.sql` 使用正確的雜湊值
3. 重新執行 seed script 更新資料庫

## 測試步驟

### 1. 啟動 API
```powershell
cd D:\Web\Stock_2330\src\SecuritiesTradingApi
dotnet run
```

### 2. 使用 SecuritiesTradingApi.http 測試登入

開啟 `src/SecuritiesTradingApi/SecuritiesTradingApi.http` 檔案，執行以下測試：

#### JWT-1: 登入 Admin 使用者
```http
POST http://localhost:5205/api/Auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin@123"
}
```

**預期回應** (HTTP 200):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123...",
  "expiresIn": 900
}
```

#### JWT-2: 登入 User1 使用者
```http
POST http://localhost:5205/api/Auth/login
Content-Type: application/json

{
  "username": "user1",
  "password": "User1@123"
}
```

#### JWT-3: 登入 User2 使用者
```http
POST http://localhost:5205/api/Auth/login
Content-Type: application/json

{
  "username": "user2",
  "password": "User2@123"
}
```

### 3. 使用 Access Token 呼叫受保護的 API

取得 `accessToken` 後，在 Authorization header 中使用：

```http
GET http://localhost:5205/api/v1/orders
Authorization: Bearer YOUR_ACCESS_TOKEN
```

## 測試帳號資訊

| 使用者名稱 | 密碼 | 角色 | 權限 |
|-----------|------|------|------|
| admin | Admin@123 | Admin | 可查看/建立所有訂單 |
| user1 | User1@123 | User | 只能查看/建立自己的訂單 |
| user2 | User2@123 | User | 只能查看/建立自己的訂單 |

## 密碼雜湊格式

正確的格式：`Base64Salt:Base64Hash`

範例：
```
RdUlcSjy4ATMtcnSQYJ/uCNs2kInipcZeNvvCR0KFzg=:GHD06d4gR/1Ggn8e4eh7DL/qjFSQdcfjSygWvZ6qH/g=
```

- Salt: 32 bytes (隨機產生)
- Hash: 32 bytes (SHA256)
- 分隔符號: `:` (冒號)

## 重新產生密碼雜湊

如需更新密碼或新增使用者，使用以下 PowerShell 腳本：

```powershell
cd D:\Web\Stock_2330\scripts
.\GeneratePasswordHashes.ps1
```

然後將產生的雜湊值更新到 SQL script 或直接更新資料庫。

## 驗證資料庫

檢查資料庫中的使用者：

```sql
USE TradingSystemDB_Dev;

SELECT 
    UserId,
    Username,
    LEFT(PasswordHash, 50) + '...' AS PasswordHashPreview,
    Role,
    CASE Role
        WHEN 1 THEN 'Admin'
        WHEN 2 THEN 'User'
    END AS RoleName,
    CreatedAt
FROM Users;
```

## 常見問題

### Q: 登入失敗，顯示 "Invalid credentials"
A: 確認密碼是否正確，區分大小寫

### Q: 登入成功但無法存取 API
A: 檢查是否正確設定 Authorization header，格式為 `Bearer YOUR_TOKEN`

### Q: Token 過期
A: Access Token 有效期為 15 分鐘，使用 JWT-4 refresh token 端點取得新的 token

### Q: 需要重置所有使用者密碼
A: 重新執行 `06_SeedUsers.sql` 即可

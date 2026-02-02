# Quickstart Validation Guide

## ✅ Prerequisites Check

Before starting, verify you have:

- [ ] .NET 8 SDK (9.0.305 or higher)
  ```bash
  dotnet --version
  ```

- [ ] SQL Server 2019+ (Developer or Enterprise Edition)
  ```bash
  sqlcmd -S localhost -Q "SELECT @@VERSION"
  ```

- [ ] Git
  ```bash
  git --version
  ```

## ✅ Step 1: Clone and Setup

```bash
# Navigate to workspace
cd D:\Web\Stock_2330

# Verify repository structure
dir specs\003-securities-trading-api
dir src\SecuritiesTradingApi
```

**Expected:** You should see:
- specs/003-securities-trading-api/ with spec.md, plan.md, tasks.md
- src/SecuritiesTradingApi/ with Program.cs, Controllers/, etc.

## ✅ Step 2: Database Setup

### 2.1 Create Database with In-Memory OLTP

```bash
sqlcmd -S localhost -E -i scripts\01_CreateDatabase.sql
```

**Expected Output:** 
```
Changed database context to 'master'.
ALTER DATABASE statement processed successfully.
ALTER DATABASE statement processed successfully.
```

**Verification:**
```sql
sqlcmd -S localhost -E -Q "SELECT name FROM sys.databases WHERE name = 'TradingDb'"
```

### 2.2 Apply EF Core Migrations

```bash
cd src\SecuritiesTradingApi
dotnet ef database update
```

**Expected Output:**
```
Build succeeded.
Applying migration '20260202_InitialCreate'.
Done.
```

**Verification:**
```sql
sqlcmd -S localhost -E -d TradingDb -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES"
```

**Expected Tables:**
- StockMaster
- StockQuotesSnapshot
- Orders_Write
- Orders_Read
- __EFMigrationsHistory

### 2.3 Load Seed Data

```bash
cd ..\..
sqlcmd -S localhost -E -d TradingDb -i scripts\02_SeedData.sql
```

**Expected Output:**
```
(10 rows affected)
```

**Verification:**
```sql
sqlcmd -S localhost -E -d TradingDb -Q "SELECT COUNT(*) AS StockCount FROM StockMaster"
```

**Expected Result:** StockCount = 10

### 2.4 (Optional) Apply Performance Indexes

```bash
sqlcmd -S localhost -E -d TradingDb -i scripts\03_PerformanceIndexes.sql
```

## ✅ Step 3: Build Application

```bash
cd src\SecuritiesTradingApi
dotnet build
```

**Expected Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## ✅ Step 4: Run Application

```bash
dotnet run
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

## ✅ Step 5: Verify API Endpoints

### 5.1 Open Swagger UI

Navigate to: https://localhost:7001/swagger

**Expected:** Swagger UI showing 3 endpoints:
- GET /api/stocks/{stockCode}
- GET /api/stocks/{stockCode}/quote
- POST /api/orders
- GET /api/orders/{orderId}

### 5.2 Test Stock Info API

```bash
curl https://localhost:7001/api/stocks/2330
```

**Expected Response (200 OK):**
```json
{
  "stockCode": "2330",
  "stockName": "台積電",
  "stockNameShort": "台積電",
  "stockNameEn": "Taiwan Semiconductor Manufacturing Company",
  "exchange": "TWSE",
  "industry": "半導體業",
  "lotSize": 1000,
  "allowOddLot": true,
  "isActive": true,
  "listedDate": "1994-09-05"
}
```

### 5.3 Test Stock Quote API

```bash
curl https://localhost:7001/api/stocks/2330/quote
```

**Expected Response:** 
- **200 OK** with quote data (if TWSE API available)
- **503 Service Unavailable** (if TWSE API down - this is normal)

### 5.4 Test Create Order API

```bash
curl -X POST https://localhost:7001/api/orders ^
  -H "Content-Type: application/json" ^
  -d "{\"userId\":1,\"stockCode\":\"2330\",\"orderType\":1,\"price\":580.00,\"quantity\":1000}"
```

**Expected Response (201 Created):**
```json
{
  "orderId": 1,
  "stockCode": "2330",
  "stockName": "台積電",
  "orderType": 1,
  "orderTypeName": "Buy",
  "price": 580.00,
  "quantity": 1000,
  "orderStatus": 1,
  "orderStatusName": "Pending",
  "tradeDate": "2026-02-02",
  "createdAt": "2026-02-02T10:00:00Z"
}
```

### 5.5 Test Query Order API

```bash
curl https://localhost:7001/api/orders/1
```

**Expected Response (200 OK):**
```json
{
  "orderId": 1,
  "userId": 1,
  "stockCode": "2330",
  "stockName": "台積電",
  "orderType": 1,
  "orderTypeName": "Buy",
  "price": 580.00,
  "quantity": 1000,
  "filledQuantity": 0,
  "orderStatus": 1,
  "orderStatusName": "Pending",
  "tradeDate": "2026-02-02",
  "createdAt": "2026-02-02T10:00:00Z"
}
```

## ✅ Step 6: Run Tests

### 6.1 Unit Tests

```bash
cd tests\SecuritiesTradingApi.UnitTests
dotnet test
```

**Expected Output:**
```
Passed!  - Failed:     0, Passed:    26, Skipped:     0, Total:    26
```

### 6.2 Integration Tests

```bash
cd ..\SecuritiesTradingApi.IntegrationTests
dotnet test
```

**Expected Output:**
```
Passed!  - Failed:     0, Passed:     X, Skipped:     0, Total:     X
```

## ✅ Step 7: Configuration Validation

### 7.1 Check Connection String

Open `src\SecuritiesTradingApi\appsettings.json`:

```json
{
  "ConnectionStrings": {
    "TradingDb": "Server=localhost;Database=TradingDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**Verify:**
- Server name matches your SQL Server instance
- TrustServerCertificate=True is present

### 7.2 Check Rate Limiting

```json
{
  "RateLimiting": {
    "PermitLimit": 10,
    "WindowSeconds": 1
  }
}
```

**Test:** Make 11 requests within 1 second - 11th should return 429

### 7.3 Check TWSE API Configuration

```json
{
  "TwseApi": {
    "BaseUrl": "https://mis.twse.com.tw",
    "TimeoutSeconds": 2,
    "MaxRetries": 2,
    "CacheSeconds": 5
  }
}
```

## 🔧 Troubleshooting

### Issue: "Cannot connect to SQL Server"

**Solution:**
```bash
# Check SQL Server is running
sqlcmd -S localhost -Q "SELECT @@VERSION"

# If fails, start SQL Server service
net start MSSQLSERVER
```

### Issue: "In-Memory OLTP not supported"

**Cause:** Using SQL Server Express

**Solution:** Install SQL Server 2019 Developer Edition (free):
https://www.microsoft.com/sql-server/sql-server-downloads

### Issue: Migration fails

**Solution:**
```bash
# Remove existing database
sqlcmd -S localhost -E -Q "DROP DATABASE TradingDb"

# Start from Step 2 again
```

### Issue: TWSE API returns 503

**Cause:** Taiwan Stock Exchange API temporarily unavailable

**Solution:** This is normal. API has retry logic (2 attempts). Client should handle 503 gracefully.

### Issue: Tests fail

**Solution:**
```bash
# Rebuild solution
cd src\SecuritiesTradingApi
dotnet clean
dotnet build

# Run tests again
cd tests\SecuritiesTradingApi.UnitTests
dotnet test
```

## ✅ Success Criteria

You have successfully completed quickstart if:

- [X] Database created with In-Memory OLTP
- [X] 10 stocks seeded in StockMaster
- [X] Application builds without errors
- [X] Application runs on https://localhost:7001
- [X] Swagger UI accessible
- [X] All 4 API endpoints return expected responses
- [X] Unit tests pass (26 tests)
- [X] Integration tests pass

## 📝 Next Steps

- Review API documentation in Swagger UI
- Check logs in `logs/` directory
- Run load tests with k6 (see k6-tests/README.md)
- Review IMPLEMENTATION_STATUS.md for architecture details

## 🆘 Support

If you encounter issues not covered here:
1. Check logs in `logs/` directory
2. Review IMPLEMENTATION_STATUS.md troubleshooting section
3. Check GitHub issues

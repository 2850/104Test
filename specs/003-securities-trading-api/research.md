# Research: 證券交易資料查詢系統技術研究

**日期**: 2026-02-02  
**Feature**: 003-securities-trading-api  
**狀態**: Phase 0 Complete

## 研究總覽

本文件記錄實作證券交易資料查詢系統時，針對技術選型、整合模式、效能優化的研究決策與理由。

---

## 1. FinMind API 整合最佳實務

### Decision: 使用 FinMind API 作為台灣證券交易資料來源

### Rationale
- FinMind 提供免費的台灣證券交易所即時報價 API
- 支援 RESTful 介面，易於整合
- 提供完整的股票資訊（價格、成交量、五檔買賣）
- 社群活躍，文件完整

### Alternatives Considered
1. **直接串接證交所公開資訊觀測站**
   - 拒絕原因：無官方 API，需爬蟲，維護成本高，不穩定
   
2. **使用付費金融資料服務商（如 XQ 全球贏家）**
   - 拒絕原因：MVP 階段預算限制，FinMind 免費方案已足夠

3. **自建爬蟲系統**
   - 拒絕原因：開發時間長，法律風險，不符合 MVP 原則

### Implementation Details

#### API 認證機制
```csharp
// appsettings.json
{
  "FinMindApi": {
    "BaseUrl": "https://api.finmindtrade.com/api/v4",
    "ApiToken": "YOUR_API_TOKEN",  // 免費用戶需註冊取得
    "Timeout": 5000,                // 5秒逾時
    "RetryCount": 2,
    "RetryDelays": [1000, 2000]     // 指數退避：1s, 2s
  }
}
```

#### 速率限制
- **免費方案**: 600 requests/hour
- **應對策略**: 
  - 使用 InMemory Cache 快取查詢結果（TTL: 10 秒）
  - 避免重複查詢相同股票
  - 實作本地速率限制保護

#### 資料格式範例
```json
{
  "msg": "success",
  "status": 200,
  "data": [
    {
      "date": "2026-02-02",
      "stock_id": "2330",
      "Trading_Volume": 52342,
      "Trading_money": 9876543210,
      "open": 950.0,
      "max": 980.0,
      "min": 945.0,
      "close": 975.0,
      "spread": 25.0,
      "Trading_turnover": 98765
    }
  ]
}
```

#### 錯誤處理與重試策略
```csharp
public class FinMindApiClient : IFinMindApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FinMindApiClient> _logger;
    private readonly FinMindApiSettings _settings;

    public async Task<StockQuoteDto> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        Exception lastException = null;

        while (retryCount <= _settings.RetryCount)
        {
            try
            {
                var response = await _httpClient.GetAsync($"data/Taiwan/StockPrice/{stockCode}", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseStockQuote(content);
            }
            catch (HttpRequestException ex) when (retryCount < _settings.RetryCount)
            {
                lastException = ex;
                var delay = _settings.RetryDelays[retryCount];
                _logger.LogWarning($"FinMind API call failed, retrying in {delay}ms... (Attempt {retryCount + 1})");
                await Task.Delay(delay, cancellationToken);
                retryCount++;
            }
        }

        _logger.LogError(lastException, $"FinMind API call failed after {_settings.RetryCount} retries");
        throw new ExternalApiException("無法取得即時資料，請稍後再試", lastException);
    }
}
```

#### API 回應快取策略
```csharp
public class CachedFinMindApiClient : IFinMindApiClient
{
    private readonly IFinMindApiClient _innerClient;
    private readonly IMemoryCache _cache;

    public async Task<StockQuoteDto> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"stock_quote_{stockCode}";
        
        if (_cache.TryGetValue(cacheKey, out StockQuoteDto cachedQuote))
        {
            return cachedQuote;
        }

        var quote = await _innerClient.GetStockQuoteAsync(stockCode, cancellationToken);
        
        _cache.Set(cacheKey, quote, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
        });

        return quote;
    }
}
```

---

## 2. EF Core Code First + SQL Server In-Memory OLTP

### Decision: 使用 EF Core Code First，但 In-Memory Tables 需手動建立

### Rationale
- EF Core 8 對 In-Memory OLTP 支援有限
- Memory-Optimized Tables 需特定語法（BUCKET_COUNT, DURABILITY）
- Native Compilation Stored Procedures 無法透過 EF Core 產生
- 採用混合模式：EF Core Migrations + 手動 SQL Script

### Alternatives Considered
1. **完全使用 EF Core Migrations**
   - 拒絕原因：EF Core 不支援 `WITH (MEMORY_OPTIMIZED = ON)` 等語法
   
2. **完全使用手動 SQL Scripts**
   - 拒絕原因：失去 Code First 便利性，維護困難

3. **使用 Dapper 取代 EF Core**
   - 拒絕原因：需手寫大量 SQL，開發效率低，不符合專案要求

### Implementation Details

#### In-Memory Table 的 EF Core 配置

**StockQuotesSnapshot (Hot Layer - Schema Only)**
```csharp
// Entity
public class StockQuotesSnapshot
{
    public string StockCode { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal YesterdayPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public long TotalVolume { get; set; }
    public DateTime UpdateTime { get; set; }
}

// Configuration
public class StockQuotesSnapshotConfiguration : IEntityTypeConfiguration<StockQuotesSnapshot>
{
    public void Configure(EntityTypeBuilder<StockQuotesSnapshot> builder)
    {
        builder.ToTable("StockQuotes_Snapshot");
        builder.HasKey(e => e.StockCode);
        
        // ⚠️ EF Core 無法設定 MEMORY_OPTIMIZED，需手動建表
        builder.ToTable(tb => tb.ExcludeFromMigrations());
    }
}
```

**手動建表 SQL (Initial Migration 的 Up 方法)**
```sql
IF OBJECT_ID('dbo.StockQuotes_Snapshot', 'U') IS NOT NULL
    DROP TABLE dbo.StockQuotes_Snapshot;
GO

CREATE TABLE dbo.StockQuotes_Snapshot (
    StockCode NVARCHAR(10) COLLATE Latin1_General_100_BIN2 NOT NULL,
    CurrentPrice DECIMAL(18,2) NOT NULL,
    YesterdayPrice DECIMAL(18,2) NOT NULL,
    OpenPrice DECIMAL(18,2) NOT NULL,
    HighPrice DECIMAL(18,2) NOT NULL,
    LowPrice DECIMAL(18,2) NOT NULL,
    TotalVolume BIGINT NOT NULL,
    UpdateTime DATETIME2 NOT NULL,
    CONSTRAINT PK_StockQuotes_Snapshot PRIMARY KEY NONCLUSTERED HASH (StockCode)
    WITH (BUCKET_COUNT = 4096)
) WITH (
    MEMORY_OPTIMIZED = ON,
    DURABILITY = SCHEMA_ONLY  -- Hot Layer: 不持久化，重啟後消失
);
```

#### Memory-Optimized Table 限制與最佳實務

**限制**:
1. 不支援 Foreign Key Constraints
2. 不支援 IDENTITY 欄位（需使用 SEQUENCE）
3. 主鍵必須是 NONCLUSTERED HASH 或 NONCLUSTERED
4. SCHEMA_ONLY tables 資料不持久化

**最佳實務**:
1. BUCKET_COUNT 設為預估資料筆數的 1.5-2 倍
2. 避免大型 BLOB/CLOB 欄位
3. 定期監控 Hash Collision

#### DURABILITY 選擇

| 類型 | SCHEMA_ONLY | SCHEMA_AND_DATA |
|------|-------------|-----------------|
| 資料持久化 | ❌ 重啟後遺失 | ✅ 持久化 |
| 寫入效能 | ⚡ 極快 | ⚡ 快 |
| 適用場景 | Hot Layer (即時報價) | Warm Layer (五檔委託簿) |
| 範例 | StockQuotes_Snapshot | OrderBook_Levels |

#### Native Compilation Stored Procedure 與 EF Core 整合

EF Core 可呼叫 Native Compiled SP，但無法產生：

```csharp
// 手動建立 SP (Migration Up 方法)
CREATE PROCEDURE dbo.sp_UpdateStockQuote_Fast
    @StockCode NVARCHAR(10),
    @CurrentPrice DECIMAL(18,2),
    @OpenPrice DECIMAL(18,2)
WITH NATIVE_COMPILATION, SCHEMABINDING
AS
BEGIN ATOMIC WITH (
    TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    LANGUAGE = N'us_english'
)
    UPDATE dbo.StockQuotes_Snapshot
    SET CurrentPrice = @CurrentPrice,
        OpenPrice = @OpenPrice,
        UpdateTime = SYSUTCDATETIME()
    WHERE StockCode = @StockCode;
END;
GO

// EF Core 呼叫
public class TradingDbContext : DbContext
{
    public async Task UpdateStockQuoteFastAsync(string stockCode, decimal currentPrice, decimal openPrice)
    {
        await Database.ExecuteSqlRawAsync(
            "EXEC sp_UpdateStockQuote_Fast @StockCode, @CurrentPrice, @OpenPrice",
            new SqlParameter("@StockCode", stockCode),
            new SqlParameter("@CurrentPrice", currentPrice),
            new SqlParameter("@OpenPrice", openPrice)
        );
    }
}
```

---

## 3. CQRS 讀寫分離實作模式

### Decision: 採用簡化 CQRS，寫入後立即同步更新讀取表

### Rationale
- MVP 階段不需複雜的 Event Sourcing
- 讀寫分離主要目的：優化查詢效能
- 寫入端專注插入，讀取端反正規化設計
- 避免 Eventual Consistency 複雜度

### Alternatives Considered
1. **完整 CQRS + Event Sourcing + Message Queue**
   - 拒絕原因：過度設計，MVP 不需要，違反 Constitution Principle VI
   
2. **單一表格，不分離讀寫**
   - 拒絕原因：查詢效能較差，無法針對讀取優化索引

3. **使用資料庫 Trigger 同步**
   - 拒絕原因：Trigger 難以測試，除錯困難，違反 Code Quality 原則

### Implementation Details

#### Orders_Write / Orders_Read 資料同步機制

**寫入端 (Orders_Write) - 最小化寫入欄位**
```csharp
public class OrdersWrite
{
    public long OrderId { get; set; }
    public int UserId { get; set; }
    public string StockCode { get; set; }
    public byte OrderType { get; set; }  // 1=買進, 2=賣出
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public byte OrderStatus { get; set; }  // 1=已委託
    public DateTime CreatedAt { get; set; }
    public long OrderSeq { get; set; }
}
```

**讀取端 (Orders_Read) - 反正規化，包含關聯資料**
```csharp
public class OrdersRead
{
    public long OrderId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }         // 反正規化：使用者名稱
    public string StockCode { get; set; }
    public string StockName { get; set; }        // 反正規化：股票名稱
    public string StockNameShort { get; set; }   // 反正規化：股票簡稱
    public byte OrderType { get; set; }
    public string OrderTypeName { get; set; }    // 反正規化：買進/賣出
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public byte OrderStatus { get; set; }
    public string OrderStatusName { get; set; }  // 反正規化：已委託
    public DateTime CreatedAt { get; set; }
    public long OrderSeq { get; set; }
}
```

#### 即時同步更新（非 Eventual Consistency）

```csharp
public class OrderService : IOrderService
{
    private readonly TradingDbContext _dbContext;

    public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // 1. 驗證股票存在，取得股票資訊
            var stock = await _dbContext.StockMaster.FindAsync(dto.StockCode);
            if (stock == null)
                throw new ValidationException("股票代號不存在");

            // 2. 寫入 Orders_Write (CQRS Write Side)
            var orderWrite = new OrdersWrite
            {
                UserId = dto.UserId,
                StockCode = dto.StockCode,
                OrderType = dto.OrderType,
                Price = dto.Price,
                Quantity = dto.Quantity,
                OrderStatus = 1,  // 已委託
                CreatedAt = DateTime.UtcNow,
                OrderSeq = await GenerateOrderSeqAsync()
            };
            
            _dbContext.OrdersWrite.Add(orderWrite);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 3. 立即同步到 Orders_Read (CQRS Read Side)
            var orderRead = new OrdersRead
            {
                OrderId = orderWrite.OrderId,
                UserId = orderWrite.UserId,
                UserName = "測試使用者",  // TODO: 實作使用者系統後從 UserAccounts 取得
                StockCode = orderWrite.StockCode,
                StockName = stock.StockName,
                StockNameShort = stock.StockNameShort,
                OrderType = orderWrite.OrderType,
                OrderTypeName = orderWrite.OrderType == 1 ? "買進" : "賣出",
                Price = orderWrite.Price,
                Quantity = orderWrite.Quantity,
                OrderStatus = orderWrite.OrderStatus,
                OrderStatusName = "已委託",
                CreatedAt = orderWrite.CreatedAt,
                OrderSeq = orderWrite.OrderSeq
            };

            _dbContext.OrdersRead.Add(orderRead);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new CreateOrderResult
            {
                OrderId = orderWrite.OrderId,
                OrderSeq = orderWrite.OrderSeq
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

#### CQRS 優勢分析

| 面向 | 單表設計 | CQRS 設計 |
|------|---------|-----------|
| 寫入效能 | 中等（需更新索引） | ⚡ 高（極簡欄位，少索引） |
| 查詢效能 | 需 JOIN | ⚡ 高（反正規化，無 JOIN） |
| 索引策略 | 衝突（寫入/查詢需求不同） | ✅ 獨立優化 |
| 資料一致性 | ✅ 強一致 | ✅ 強一致（同步更新） |
| 複雜度 | 低 | 中等 |
| 適用場景 | 低流量 | 高頻交易 ✅ |

---

## 4. FluentValidation 非同步驗證

### Decision: 使用 FluentValidation 11.x，註冊為 Singleton，支援非同步驗證

### Rationale
- FluentValidation 提供宣告式驗證語法，易於維護
- MustAsync 支援非同步資料庫查詢
- 註冊為 Singleton 避免重複建立驗證器實例
- 版本選擇 11.x 避免 8.x+ FluentAssertions 付費限制

### Alternatives Considered
1. **Data Annotations**
   - 拒絕原因：不支援非同步驗證，無法查詢資料庫

2. **手動驗證邏輯**
   - 拒絕原因：程式碼分散，難以維護，違反 DRY 原則

3. **自訂驗證框架**
   - 拒絕原因：重複造輪子，違反 Constitution Principle VII (不修改第三方套件)

### Implementation Details

#### 股票代號驗證器 (非同步驗證)

```csharp
public class StockQueryValidator : AbstractValidator<StockQueryDto>
{
    private readonly TradingDbContext _dbContext;

    public StockQueryValidator(TradingDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.StockCode)
            .NotEmpty()
            .WithMessage("股票代號不得為空")
            .Matches(@"^\d{4}$")
            .WithMessage("股票代號格式錯誤，必須為 4 位數字")
            .MustAsync(StockExistsAsync)
            .WithMessage("股票代號不存在");
    }

    private async Task<bool> StockExistsAsync(string stockCode, CancellationToken cancellationToken)
    {
        return await _dbContext.StockMaster.AnyAsync(s => s.StockCode == stockCode, cancellationToken);
    }
}
```

#### 委託單驗證器 (複雜驗證邏輯)

```csharp
public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    private readonly TradingDbContext _dbContext;
    private readonly IFinMindApiClient _finMindApi;

    public CreateOrderValidator(TradingDbContext dbContext, IFinMindApiClient finMindApi)
    {
        _dbContext = dbContext;
        _finMindApi = finMindApi;

        RuleFor(x => x.StockCode)
            .NotEmpty().WithMessage("股票代號不得為空")
            .Matches(@"^\d{4}$").WithMessage("股票代號格式錯誤，必須為 4 位數字")
            .MustAsync(StockExistsAsync).WithMessage("股票代號不存在");

        RuleFor(x => x.OrderType)
            .Must(type => type == 1 || type == 2)
            .WithMessage("買賣別錯誤，1=買進, 2=賣出");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("委託數量必須大於 0")
            .Must(qty => qty % 1000 == 0).WithMessage("委託數量必須為 1000 股的整數倍（整股交易）");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("委託價格必須大於 0")
            .MustAsync(PriceWithinLimitAsync).WithMessage("委託價格超出漲跌停範圍");
    }

    private async Task<bool> StockExistsAsync(string stockCode, CancellationToken cancellationToken)
    {
        return await _dbContext.StockMaster.AnyAsync(s => s.StockCode == stockCode, cancellationToken);
    }

    private async Task<bool> PriceWithinLimitAsync(CreateOrderDto dto, decimal price, ValidationContext<CreateOrderDto> context, CancellationToken cancellationToken)
    {
        try
        {
            var quote = await _finMindApi.GetStockQuoteAsync(dto.StockCode, cancellationToken);
            return price >= quote.LimitDownPrice && price <= quote.LimitUpPrice;
        }
        catch
        {
            // 若外部 API 失敗，暫時允許通過（避免阻擋使用者下單）
            return true;
        }
    }
}
```

#### 效能優化：驗證器註冊為 Singleton

```csharp
// Program.cs
builder.Services.AddSingleton<IValidator<StockQueryDto>, StockQueryValidator>();
builder.Services.AddSingleton<IValidator<CreateOrderDto>, CreateOrderValidator>();

// ⚠️ 注意：驗證器內的 DbContext 需透過建構函式注入，不可直接使用
// 因 Singleton 生命週期比 Scoped 的 DbContext 長，需特殊處理

// 正確作法：使用 IServiceProvider 動態解析 DbContext
public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    private readonly IServiceProvider _serviceProvider;

    public CreateOrderValidator(IServiceProvider serviceProvider, IFinMindApiClient finMindApi)
    {
        _serviceProvider = serviceProvider;

        RuleFor(x => x.StockCode)
            .MustAsync(async (stockCode, cancellationToken) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
                return await dbContext.StockMaster.AnyAsync(s => s.StockCode == stockCode, cancellationToken);
            })
            .WithMessage("股票代號不存在");
    }
}
```

#### 自訂錯誤訊息本地化（繁體中文）

```csharp
public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        // 覆寫預設錯誤訊息模板
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("委託數量必須大於 0，您輸入的數量為 {PropertyValue}")
            .Must(qty => qty % 1000 == 0)
            .WithMessage("委託數量必須為 1000 股的整數倍（整股交易），您輸入的數量為 {PropertyValue} 股");
    }
}
```

#### 驗證錯誤回傳格式標準化

```csharp
// Middleware 統一處理驗證錯誤
public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";

            var errors = ex.Errors.Select(e => new
            {
                Field = e.PropertyName,
                Message = e.ErrorMessage
            });

            var response = new
            {
                Status = 400,
                Title = "驗證錯誤",
                Errors = errors
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
```

---

## 5. API 速率限制實作

### Decision: 使用 ASP.NET Core Middleware 實作 IP-based Sliding Window 速率限制

### Rationale
- Sliding Window 演算法比 Fixed Window 更平滑，避免邊界突發流量
- 基於 IP 位址限制，簡單有效
- 使用 InMemory Cache 儲存請求計數
- 符合 FR-024 需求：每秒 10 次請求限制

### Alternatives Considered
1. **Fixed Window 演算法**
   - 拒絕原因：邊界問題，可能瞬間超過限制兩倍

2. **Token Bucket 演算法**
   - 拒絕原因：實作複雜度高，MVP 階段不需要

3. **使用第三方套件（AspNetCoreRateLimit）**
   - 拒絕原因：依賴外部套件，簡單功能自行實作即可

### Implementation Details

#### Sliding Window 演算法實作

```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private const int MaxRequestsPerSecond = 10;
    private const int WindowSizeInSeconds = 1;

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"rate_limit_{clientIp}";

        var requestTimestamps = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromSeconds(WindowSizeInSeconds);
            return new List<DateTime>();
        });

        lock (requestTimestamps)
        {
            var now = DateTime.UtcNow;
            var windowStart = now.AddSeconds(-WindowSizeInSeconds);

            // 移除超出時間視窗的請求記錄
            requestTimestamps.RemoveAll(t => t < windowStart);

            if (requestTimestamps.Count >= MaxRequestsPerSecond)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.Add("Retry-After", WindowSizeInSeconds.ToString());
                
                var response = new
                {
                    Status = 429,
                    Title = "請求次數過多",
                    Message = $"您已超過速率限制（每秒 {MaxRequestsPerSecond} 次請求），請稍後再試",
                    RetryAfter = $"{WindowSizeInSeconds} 秒"
                };

                await context.Response.WriteAsJsonAsync(response);
                
                _logger.LogWarning($"Rate limit exceeded for IP: {clientIp}");
                return;
            }

            requestTimestamps.Add(now);
        }

        await _next(context);
    }
}

// Program.cs
app.UseMiddleware<RateLimitingMiddleware>();
```

#### Sliding Window vs Fixed Window 比較

```
Fixed Window (固定視窗):
Time: 0s----1s----2s----3s
Req:  [  10  ][  10  ]
      Window1  Window2
問題：在 0.9s-1.1s 可能瞬間 20 次請求

Sliding Window (滑動視窗):
Time: 0s----0.5s----1s----1.5s----2s
Req:  [    10 within 1s    ]
               [    10 within 1s    ]
優點：任意 1 秒內最多 10 次，無邊界問題
```

#### 速率限制快取儲存（InMemory）

```csharp
// appsettings.json
{
  "RateLimiting": {
    "MaxRequestsPerSecond": 10,
    "WindowSizeInSeconds": 1,
    "CacheSizeLimit": 1024  // 最多儲存 1024 個 IP 的記錄
  }
}

// Program.cs
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;  // 限制快取大小
});
```

#### IP 位址取得（考慮 Reverse Proxy）

```csharp
private string GetClientIp(HttpContext context)
{
    // 優先從 X-Forwarded-For 取得真實 IP（如使用 Nginx, CloudFlare）
    if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
    {
        var ip = forwardedFor.ToString().Split(',').FirstOrDefault();
        if (!string.IsNullOrEmpty(ip))
            return ip.Trim();
    }

    // 其次從 X-Real-IP 取得
    if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
    {
        return realIp.ToString();
    }

    // 最後使用直連 IP
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
```

---

## 6. 統一錯誤回傳格式

### Decision: 採用 ProblemDetails (RFC 7807) 標準，透過 Middleware 統一處理

### Rationale
- RFC 7807 是業界標準，大多數 API 客戶端已支援
- ASP.NET Core 內建支援
- 統一錯誤格式提升開發者體驗
- 避免在每個 Controller 重複錯誤處理邏輯

### Alternatives Considered
1. **自訂錯誤回應結構**
   - 拒絕原因：不符合業界標準，增加客戶端整合成本

2. **在每個 Controller Action 處理錯誤**
   - 拒絕原因：程式碼重複，違反 DRY 原則

### Implementation Details

#### ProblemDetails 標準格式

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "驗證錯誤",
  "status": 400,
  "detail": "請求包含無效的參數",
  "instance": "/api/orders",
  "traceId": "00-1234567890abcdef-1234567890abcdef-00",
  "errors": {
    "StockCode": ["股票代號不存在"],
    "Quantity": ["委託數量必須為 1000 股的整數倍"]
  }
}
```

#### 錯誤處理 Middleware

```csharp
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            ValidationException validationEx => CreateValidationProblemDetails(context, validationEx),
            NotFoundException notFoundEx => CreateNotFoundProblemDetails(context, notFoundEx),
            ExternalApiException apiEx => CreateExternalApiProblemDetails(context, apiEx),
            _ => CreateInternalServerErrorProblemDetails(context, exception)
        };

        context.Response.StatusCode = problemDetails.Status ?? 500;
        context.Response.ContentType = "application/problem+json; charset=utf-8";

        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private ProblemDetails CreateValidationProblemDetails(HttpContext context, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "驗證錯誤",
            Status = StatusCodes.Status400BadRequest,
            Detail = "請求包含無效的參數",
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier }
        };
    }

    private ProblemDetails CreateNotFoundProblemDetails(HttpContext context, NotFoundException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "資源不存在",
            Status = StatusCodes.Status404NotFound,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier }
        };
    }

    private ProblemDetails CreateExternalApiProblemDetails(HttpContext context, ExternalApiException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            Title = "外部服務錯誤",
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier }
        };
    }

    private ProblemDetails CreateInternalServerErrorProblemDetails(HttpContext context, Exception exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "伺服器錯誤",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "系統發生錯誤，請稍後再試",
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier }
        };
    }
}

// Program.cs
app.UseMiddleware<ErrorHandlingMiddleware>();
```

#### 自訂例外類別

```csharp
public class ValidationException : Exception
{
    public IEnumerable<ValidationFailure> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> errors)
        : base("驗證錯誤")
    {
        Errors = errors;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ExternalApiException : Exception
{
    public ExternalApiException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

#### 驗證錯誤 (400) 與資源不存在 (404) 處理

```csharp
// Controller 範例
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        // FluentValidation 自動驗證，失敗則拋出 ValidationException
        var result = await _orderService.CreateOrderAsync(dto);
        return CreatedAtAction(nameof(GetOrder), new { orderId = result.OrderId }, result);
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(long orderId)
    {
        var order = await _orderService.GetOrderAsync(orderId);
        
        if (order == null)
            throw new NotFoundException($"委託單不存在：{orderId}");

        return Ok(order);
    }
}
```

---

## 7. k6 壓力測試腳本設計

### Decision: 使用 k6 進行負載測試與壓力測試，模擬真實交易場景

### Rationale
- k6 是現代效能測試工具，語法簡潔（JavaScript）
- 支援分階段流量增長（Ramping VUs）
- 內建 Threshold 斷言，測試失敗自動報警
- 輸出格式豐富（JSON, InfluxDB, HTML Report）

### Alternatives Considered
1. **JMeter**
   - 拒絕原因：GUI 工具，不適合 CI/CD 整合

2. **Locust (Python)**
   - 拒絕原因：需額外 Python 環境，團隊不熟悉

3. **Apache Bench (ab)**
   - 拒絕原因：功能簡陋，無法模擬複雜場景

### Implementation Details

#### 負載測試腳本 (load-test.js)

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// 自訂指標
const errorRate = new Rate('errors');

// 負載測試配置
export const options = {
  scenarios: {
    // 情境 1: 股票查詢
    stock_query: {
      executor: 'ramping-vus',
      exec: 'queryStock',
      startVUs: 3,
      stages: [
        { target: 20, duration: '30s' },   // 30 秒內增加到 20 個使用者
        { target: 100, duration: '0' },    // 立即跳到 100 個使用者
        { target: 100, duration: '10m' },  // 維持 100 個使用者 10 分鐘
      ],
    },
    // 情境 2: 委託下單
    create_order: {
      executor: 'ramping-vus',
      exec: 'createOrder',
      startVUs: 1,
      stages: [
        { target: 10, duration: '30s' },
        { target: 50, duration: '10m' },
      ],
    },
  },
  thresholds: {
    // 錯誤率必須低於 1%
    'errors': ['rate<0.01'],
    // 平均回應時間必須在 500ms 內
    'http_req_duration': ['avg<500', 'p(95)<1000'],
    // 90% 的股票查詢必須在 800ms 內完成
    'http_req_duration{scenario:stock_query}': ['p(90)<800'],
    // 95% 的委託下單必須在 2000ms 內完成
    'http_req_duration{scenario:create_order}': ['p(95)<2000'],
  },
};

const BASE_URL = 'http://localhost:5000/api';
const STOCK_CODES = ['2330', '2317', '2454', '2308', '6505'];

// 情境 1: 股票查詢
export function queryStock() {
  const stockCode = STOCK_CODES[Math.floor(Math.random() * STOCK_CODES.length)];
  
  // 1. 查詢股票資訊
  const stockRes = http.get(`${BASE_URL}/stocks/${stockCode}`);
  check(stockRes, {
    'stock query status 200': (r) => r.status === 200,
    'stock query has data': (r) => r.json('stockCode') === stockCode,
  }) || errorRate.add(1);

  sleep(1);

  // 2. 查詢即時報價
  const quoteRes = http.get(`${BASE_URL}/stocks/${stockCode}/quote`);
  check(quoteRes, {
    'quote query status 200': (r) => r.status === 200,
    'quote has current price': (r) => r.json('currentPrice') > 0,
  }) || errorRate.add(1);

  sleep(2);
}

// 情境 2: 委託下單
export function createOrder() {
  const stockCode = STOCK_CODES[Math.floor(Math.random() * STOCK_CODES.length)];
  
  // 1. 先查詢報價取得漲跌停價
  const quoteRes = http.get(`${BASE_URL}/stocks/${stockCode}/quote`);
  if (quoteRes.status !== 200) {
    errorRate.add(1);
    return;
  }

  const quote = quoteRes.json();
  const price = quote.currentPrice;

  // 2. 建立委託單
  const orderPayload = JSON.stringify({
    userId: 1,
    stockCode: stockCode,
    orderType: Math.random() > 0.5 ? 1 : 2,  // 隨機買進/賣出
    price: price,
    quantity: 1000,
  });

  const orderRes = http.post(`${BASE_URL}/orders`, orderPayload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(orderRes, {
    'order creation status 201': (r) => r.status === 201,
    'order has orderId': (r) => r.json('orderId') > 0,
  }) || errorRate.add(1);

  sleep(3);
}
```

#### 壓力測試腳本 (stress-test.js)

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

// 壓力測試配置
export const options = {
  stages: [
    { duration: '30s', target: 20 },   // 30 秒內逐步增加到 20 個使用者
    { duration: '1m', target: 50 },    // 1 分鐘內增加到 50 個使用者
    { duration: '30s', target: 100 },  // 30 秒內增加到 100 個使用者
    { duration: '2m', target: 100 },   // 維持 100 個使用者 2 分鐘（尋找系統瓶頸）
    { duration: '30s', target: 200 },  // 衝高到 200 個使用者
    { duration: '1m', target: 200 },   // 維持 200 個使用者 1 分鐘（驗證崩潰點）
    { duration: '30s', target: 0 },    // 30 秒內降到 0（觀察系統恢復）
  ],
  thresholds: {
    // 壓力測試目標：觀察系統在高負載下的表現
    'http_req_failed': ['rate<0.05'],  // 錯誤率低於 5%
    'http_req_duration': ['p(95)<3000'],  // 95% 請求在 3 秒內完成
  },
};

const BASE_URL = 'http://localhost:5000/api';
const STOCK_CODES = ['2330', '2317', '2454', '2308', '6505'];

export default function () {
  const stockCode = STOCK_CODES[Math.floor(Math.random() * STOCK_CODES.length)];

  // 混合不同 API 呼叫
  const actions = [
    () => http.get(`${BASE_URL}/stocks/${stockCode}`),
    () => http.get(`${BASE_URL}/stocks/${stockCode}/quote`),
    () => {
      const payload = JSON.stringify({
        userId: 1,
        stockCode: stockCode,
        orderType: 1,
        price: 100,
        quantity: 1000,
      });
      return http.post(`${BASE_URL}/orders`, payload, {
        headers: { 'Content-Type': 'application/json' },
      });
    },
  ];

  const randomAction = actions[Math.floor(Math.random() * actions.length)];
  const res = randomAction();

  check(res, {
    'status is 2xx or 3xx': (r) => r.status >= 200 && r.status < 400,
  });

  sleep(1);
}
```

#### 執行指令與報告

```bash
# 負載測試
k6 run --out json=load-test-results.json load-test.js

# 壓力測試
k6 run --out json=stress-test-results.json stress-test.js

# 產生 HTML 報告 (需安裝 k6-reporter)
k6 run --out json=results.json load-test.js
k6-reporter results.json --output report.html
```

#### Threshold 設定與斷言

| Threshold | 說明 | 目標值 |
|-----------|------|--------|
| `http_req_failed` | HTTP 請求失敗率 | <1% (負載), <5% (壓力) |
| `http_req_duration` | 請求回應時間 | avg<500ms, p95<1000ms |
| `http_req_duration{scenario:X}` | 特定情境回應時間 | 依情境設定 |
| `errors` | 自訂錯誤率 | <1% |

---

## 研究總結

所有技術決策已完成，無 NEEDS CLARIFICATION 項目。主要技術選型：

1. **外部 API**: FinMind API（免費方案，600 req/hour，支援重試機制）
2. **資料庫**: SQL Server In-Memory OLTP（混合 EF Core + 手動 SQL）
3. **讀寫分離**: CQRS（即時同步，強一致性）
4. **驗證框架**: FluentValidation 11.x（Singleton 註冊，支援非同步驗證）
5. **速率限制**: Sliding Window 演算法（InMemory Cache，10 req/s per IP）
6. **錯誤處理**: ProblemDetails (RFC 7807) + Middleware 統一處理
7. **效能測試**: k6（負載測試 + 壓力測試，內建 Threshold 斷言）

---

**Phase 0 Complete** ✅  
**下一步**: Phase 1 - 產生 data-model.md, contracts/openapi.yaml, quickstart.md

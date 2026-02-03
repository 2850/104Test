using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Serilog;
using SecuritiesTradingApi.Data;
using SecuritiesTradingApi.Services;
using SecuritiesTradingApi.Infrastructure.Middleware;
using SecuritiesTradingApi.Infrastructure.Cache;
using SecuritiesTradingApi.Infrastructure.ExternalApis;
using SecuritiesTradingApi.Infrastructure.Validators;
using SecuritiesTradingApi.Models.Dtos;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add controllers
builder.Services.AddControllers();

// Add DbContext
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions
            .EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null)));

// Add Memory Cache
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();

// Add HTTP Client for TWSE API
builder.Services.AddHttpClient<TwseApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["TwseApi:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("TwseApi:TimeoutSeconds", 2));
})
.AddTypedClient<TwseApiClient>();

// Register API clients
builder.Services.AddScoped<ITwseApiClient, CachedTwseApiClient>();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();

// Add Services
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 10),
                Window = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 1)),
                SegmentsPerWindow = builder.Configuration.GetValue<int>("RateLimiting:SegmentsPerWindow", 2),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = 429,
            title = "Too Many Requests",
            detail = "Rate limit exceeded. Please try again later."
        }, cancellationToken);
    };
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Securities Trading API",
        Version = "v1",
        Description = "證券交易資料查詢系統 API - 提供台灣股票資訊查詢和委託下單功能",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Securities Trading API Team",
            Email = "support@example.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add examples
    options.EnableAnnotations();
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.MapControllers();

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }

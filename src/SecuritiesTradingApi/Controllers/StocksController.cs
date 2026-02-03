using Microsoft.AspNetCore.Mvc;
using SecuritiesTradingApi.Services;

namespace SecuritiesTradingApi.Controllers;

/// <summary>
/// 股票資訊查詢控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class StocksController : ControllerBase
{
    private readonly IStockService _stockService;
    private readonly ILogger<StocksController> _logger;

    public StocksController(IStockService stockService, ILogger<StocksController> logger)
    {
        _stockService = stockService;
        _logger = logger;
    }

    /// <summary>
    /// 查詢股票基本資料
    /// </summary>
    /// <param name="stockCode">股票代號（例如：2330）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>股票基本資料</returns>
    /// <response code="200">成功返回股票資料</response>
    /// <response code="404">股票代號不存在</response>
    [HttpGet("{stockCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockInfo(string stockCode, CancellationToken cancellationToken)
    {
        var stock = await _stockService.GetStockInfoAsync(stockCode, cancellationToken);
        
        if (stock == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = $"Stock {stockCode} not found"
            });
        }
        
        return Ok(stock);
    }

    /// <summary>
    /// 查詢股票即時報價
    /// </summary>
    /// <param name="stockCode">股票代號（例如：2330）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>即時報價資料</returns>
    /// <response code="200">成功返回報價資料</response>
    /// <response code="503">證交所 API 暫時無法使用</response>
    [HttpGet("{stockCode}/quote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetStockQuote(string stockCode, CancellationToken cancellationToken)
    {
        var quote = await _stockService.GetStockQuoteAsync(stockCode, cancellationToken);
        
        if (quote == null)
        {
            return StatusCode(503, new ProblemDetails
            {
                Status = 503,
                Title = "Service Unavailable",
                Detail = "Unable to fetch stock quote from external API"
            });
        }
        
        return Ok(quote);
    }
}

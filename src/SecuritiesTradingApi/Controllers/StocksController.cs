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
    /// 查詢股票列表（支援symbol或keyword查詢）
    /// </summary>
    /// <param name="symbol">股票代號精確查詢（例如：2330）</param>
    /// <param name="keyword">關鍵字查詢，搜尋股票名稱和簡稱</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合條件的股票列表</returns>
    /// <response code="200">成功返回股票列表</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchStocks([FromQuery] string? symbol, [FromQuery] string? keyword, CancellationToken cancellationToken)
    {
        var stocks = await _stockService.SearchStocksAsync(symbol, keyword, cancellationToken);
        return Ok(stocks);
    }

    /// <summary>
    /// 查詢股票基本資料
    /// </summary>
    /// <param name="symbol">股票代號（例如：2330）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>股票基本資料</returns>
    /// <response code="200">成功返回股票資料</response>
    /// <response code="404">股票代號不存在</response>
    [HttpGet("{symbol}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockInfo(string symbol, CancellationToken cancellationToken)
    {
        var stock = await _stockService.GetStockInfoAsync(symbol, cancellationToken);
        
        if (stock == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = $"Stock {symbol} not found"
            });
        }
        
        return Ok(stock);
    }

    /// <summary>
    /// 查詢股票即時報價
    /// </summary>
    /// <param name="symbol">股票代號（例如：2330）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>即時報價資料</returns>
    /// <response code="200">成功返回報價資料</response>
    /// <response code="503">證交所 API 暫時無法使用</response>
    [HttpGet("{symbol}/Info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetStockQuote(string symbol, CancellationToken cancellationToken)
    {
        var quote = await _stockService.GetStockQuoteAsync(symbol, cancellationToken);
        
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

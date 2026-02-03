using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SecuritiesTradingApi.Models.Dtos;
using SecuritiesTradingApi.Services;

namespace SecuritiesTradingApi.Controllers;

/// <summary>
/// 委託單管理控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IValidator<CreateOrderDto> _validator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        IValidator<CreateOrderDto> validator,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// 查詢委託單列表
    /// </summary>
    /// <param name="userId">使用者ID (選填，用於篩選特定使用者的委託單)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>委託單列表</returns>
    /// <response code="200">成功返回委託單列表</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders([FromQuery] long? userId, CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersAsync(userId, cancellationToken);
        return Ok(orders);
    }

    /// <summary>
    /// 建立股票委託單
    /// </summary>
    /// <param name="orderDto">委託單資料</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>委託單建立結果</returns>
    /// <response code="201">成功建立委託單</response>
    /// <response code="400">輸入資料驗證失敗</response>
    /// <response code="404">股票代號不存在</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto orderDto, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(orderDto, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Validation Failed",
                Detail = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))
            });
        }

        try
        {
            var result = await _orderService.CreateOrderAsync(orderDto, cancellationToken);
            return CreatedAtAction(nameof(CreateOrder), new { id = result.OrderId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// 查詢委託單詳細資料
    /// </summary>
    /// <param name="orderId">委託單編號</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>委託單詳細資料</returns>
    /// <response code="200">成功返回委託單資料</response>
    /// <response code="404">委託單不存在</response>
    [HttpGet("{orderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(long orderId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
        
        if (order == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = $"Order {orderId} not found"
            });
        }
        
        return Ok(order);
    }
}

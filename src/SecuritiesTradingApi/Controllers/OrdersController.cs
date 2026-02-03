using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SecuritiesTradingApi.Models.Dtos;
using SecuritiesTradingApi.Services;
using SecuritiesTradingApi.Models;
using System.Security.Claims;

namespace SecuritiesTradingApi.Controllers;

/// <summary>
/// 委託單管理控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
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
    /// <param name="userId">使用者ID (選填，管理員可查詢所有使用者，一般使用者只能查詢自己的委託單)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>委託單列表</returns>
    /// <response code="200">成功返回委託單列表</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders([FromQuery] long? userId, CancellationToken cancellationToken)
    {
        // 取得目前登入使用者的資訊
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId))
        {
            return Unauthorized(new ErrorResponseDto(ErrorCodes.TOKEN_INVALID, "無效的使用者 Token"));
        }

        var role = roleClaim?.Value;

        // 一般使用者只能查詢自己的委託單
        if (role != "Admin")
        {
            userId = authenticatedUserId;
        }

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
        // 取得目前登入使用者的 ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId))
        {
            return Unauthorized(new ErrorResponseDto(ErrorCodes.TOKEN_INVALID, "無效的使用者 Token"));
        }

        // 強制使用 authenticated user 的 UserId
        orderDto.UserId = authenticatedUserId;

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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        // 檢查權限：一般使用者只能查看自己的委託單
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId))
        {
            return Unauthorized(new ErrorResponseDto(ErrorCodes.TOKEN_INVALID, "無效的使用者 Token"));
        }

        // 一般使用者只能查看自己的委託單
        if (roleClaim?.Value != "Admin" && order.UserId != authenticatedUserId)
        {
            return StatusCode(403, new ErrorResponseDto(ErrorCodes.FORBIDDEN, "您沒有權限查看此委託單"));
        }
        
        return Ok(order);
    }
}

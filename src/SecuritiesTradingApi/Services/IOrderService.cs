using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.Services;

public interface IOrderService
{
    Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto orderDto, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default);
    Task<List<OrderDto>> GetOrdersAsync(long? userId = null, CancellationToken cancellationToken = default);
}

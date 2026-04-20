using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders
{
    public interface IOrderRepository
    {
        Task<OrderResponseDto?> GetByIdAsync(Guid id, string tenantId);
        Task<PagedResult<OrderResponseDto>> GetFilteredAsync(
    string? search,
    int pageNumber,
    int pageSize,
     string userId,
    int? statusFilter = null,
    DateTime? from = null,
    DateTime? to = null);
        Task<OrderResponseDto> AddOrUpdateOrderAsync(OrderDto dto, string? userId);
        Task<bool> SoftDeleteAsync(Guid id);

    }


}
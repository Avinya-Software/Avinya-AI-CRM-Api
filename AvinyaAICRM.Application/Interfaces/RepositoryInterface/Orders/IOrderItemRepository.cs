using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Domain.Entities.Orders;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders
{
    public interface IOrderItemRepository
    {
        Task<IEnumerable<OrderItemReponceDto>> GetAllAsync();
        Task<OrderItemReponceDto?> GetByIdAsync(Guid id);
        Task<OrderItem> CreateAsync(OrderItem item);
        Task<OrderItem?> UpdateAsync(OrderItem dto);
        Task<bool> DeleteAsync(Guid id);
        Task<PagedResult<OrderItemReponceDto>> GetFilteredAsync(
       string? search,
       int pageNumber,
       int pageSize);
    }
}

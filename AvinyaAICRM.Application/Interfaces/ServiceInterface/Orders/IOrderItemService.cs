using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders
{
    public interface IOrderItemService
    {
        Task<ResponseModel> GetAllAsync();
        Task<ResponseModel> GetByIdAsync(Guid id);
        Task<ResponseModel> CreateAsync(OrderItemDto dto);
        Task<ResponseModel> UpdateAsync(Guid id, OrderItemDto dto);
        Task<ResponseModel> DeleteAsync(Guid id);
        Task<ResponseModel> GetFilteredAsync(string? search, int page, int pageSize);
    }
}

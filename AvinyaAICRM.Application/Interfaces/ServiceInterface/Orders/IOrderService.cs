using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders
{
    public interface IOrderService
    {
        Task<ResponseModel> GetByIdAsync(Guid id, string tenantId);
        Task<ResponseModel> GetFilteredAsync(string? search, int page, int pageSize, string userId, int? statusFilter = null, DateTime? from = null, DateTime? to = null);
        Task<ResponseModel> AddOrUpdateAsync(OrderDto dto, string userId);
        Task<ResponseModel> DeleteAsync(Guid id);
    }
}
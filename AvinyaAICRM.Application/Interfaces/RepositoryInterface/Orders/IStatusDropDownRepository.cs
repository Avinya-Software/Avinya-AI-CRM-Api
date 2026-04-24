using AvinyaAICRM.Domain.Entities.Orders;
using AvinyaAICRM.Domain.Entities.Product;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders
{
    public interface IStatusDropDownRepository
    {
        Task<IEnumerable<DesignStatusMaster>> GetAllDesignStatusAsync();
        Task<IEnumerable<OrderStatusMaster>> GetAllOrderStatusAsync();
        Task<IEnumerable<object>> GetAllProjectStatusAsync();
        Task<IEnumerable<object>> GetAllProjectPriorityAsync();
    }
}

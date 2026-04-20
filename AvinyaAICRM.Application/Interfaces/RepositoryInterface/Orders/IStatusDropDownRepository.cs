using AvinyaAICRM.Domain.Entities.Orders;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders
{
    public interface IStatusDropDownRepository
    {
        Task<IEnumerable<DesignStatusMaster>> GetAllDesignStatusAsync();
        Task<IEnumerable<OrderStatusMaster>> GetAllOrderStatusAsync();
    }
}

using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders
{
    public interface IStatusDropDownServices
    {
        Task<ResponseModel> GetAllDesignStatusAsync();
        Task<ResponseModel> GetAllOrderStatusAsync();
    }
}

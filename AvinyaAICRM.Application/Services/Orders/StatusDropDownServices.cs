using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Services.Orders
{
    public class StatusDropDownServices : IStatusDropDownServices
    {
        private readonly IStatusDropDownRepository _statusDropDownRepository;

        public StatusDropDownServices(IStatusDropDownRepository statusDropDownRepository)
        {
            _statusDropDownRepository = statusDropDownRepository;
        }

        public async Task<ResponseModel> GetAllDesignStatusAsync()
        {
            var result = await _statusDropDownRepository.GetAllDesignStatusAsync();
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetAllOrderStatusAsync()
        {
            var result = await _statusDropDownRepository.GetAllOrderStatusAsync();
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetAllProjectStatusAsync()
        {
            var result = await _statusDropDownRepository.GetAllProjectStatusAsync();
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetAllProjectPriorityAsync()
        {
            var result = await _statusDropDownRepository.GetAllProjectPriorityAsync();
            return CommonHelper.GetResponseMessage(result);
        }
    }
}

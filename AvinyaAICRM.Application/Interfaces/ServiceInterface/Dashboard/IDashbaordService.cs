using AvinyaAICRM.Application.DTOs.Dashboard;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Dashboard
{
    public interface IDashbaordService
    {
        Task<ResponseModel> GetDashboardAsync();

    }
}

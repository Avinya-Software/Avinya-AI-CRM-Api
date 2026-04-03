using AvinyaAICRM.Application.DTOs.Dashboard;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Dashboard;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Dashboard;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Services.Dashboard
{

    public class DashbaordService : IDashbaordService
    {
        private readonly IDashboardRepository _repo;

        public DashbaordService(IDashboardRepository repo)
        {
            _repo = repo;
        }

        public async Task<ResponseModel> GetDashboardAsync(string tenantId, string? role, string? userId)
        {
            var data = await _repo.GetDashboardAsync(tenantId, role, userId);
            return CommonHelper.GetResponseMessage(data);
        }
    }
}

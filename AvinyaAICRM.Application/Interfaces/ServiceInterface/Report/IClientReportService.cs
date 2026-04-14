using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Report
{
    public interface IClientReportService
    {
        Task<ResponseModel> GetClientReportAsync(ClientReportFilterDto filter);
        Task<ResponseModel> GetClientDrillDownAsync(Guid clientId, Guid tenantId);
    }
}

using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Report
{
    public interface ILeadReportService
    {
        Task<ResponseModel> GetLeadPipelineReportAsync(LeadPipelineFilterDto filter);
        Task<ResponseModel> GetLeadLifecycleReportAsync(LeadPipelineFilterDto filter);
    }
}

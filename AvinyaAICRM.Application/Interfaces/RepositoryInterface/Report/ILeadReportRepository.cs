using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report
{
    public interface ILeadReportRepository
    {
        Task<LeadPipelineReportDto> GetLeadPipelineReportAsync(LeadPipelineFilterDto filter);
        Task<PagedResult<LeadLifecycleReportDto>> GetLeadLifecycleReportAsync(LeadPipelineFilterDto filter);
    }
}

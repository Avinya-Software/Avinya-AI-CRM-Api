using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Shared.Model;


namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Report
{
    public interface ITaskProjectReportService
    {
        Task<ResponseModel> GetTaskProjectReportAsync(TaskProjectReportFilterDto filter);
    }
}

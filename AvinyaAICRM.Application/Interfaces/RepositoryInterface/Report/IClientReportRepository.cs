using AvinyaAICRM.Application.DTOs.Report;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report
{
    public interface IClientReportRepository
    {
        Task<ClientReportDto> GetClientReportAsync(ClientReportFilterDto filter);
    }
}

using AvinyaAICRM.Application.DTOs.Dashboard;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Dashboard
{
    public interface IDashboardRepository
    {
        Task<DashboardDto> GetDashboardAsync(
     string tenantId,
     string? role,
     string? userId,
     DateTime? fromDate,
     DateTime? toDate);

    }
}

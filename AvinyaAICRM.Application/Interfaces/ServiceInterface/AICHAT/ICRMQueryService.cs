using AvinyaAICRM.Application.DTOs.AICHATS;


namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT
{
    public interface ICRMQueryService
    {
        Task<List<Dictionary<string, object>>> ExecuteRawSqlAsync(string sql, Guid tenantId, bool isSuperAdmin);
        Task<SummaryDto> GetSummaryAsync(string dateRange);
    }
}

using AvinyaAICRM.Shared.AI;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT
{
    public interface ICRMQueryService
    {
        Task<List<Dictionary<string, object>>> ExecuteRawSqlAsync(string sql, Guid tenantId, string userId, bool isAdmin, Dictionary<string, object>? parameters = null);
        Task<AIResponse> ProcessCommandAsync(string message, Guid tenantId, string userId, bool isAdmin, List<string> allowedModules);
        Task<List<string>> GetUserAllowedModulesAsync(string userId);
    }
}

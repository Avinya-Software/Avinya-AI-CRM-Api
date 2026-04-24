using AvinyaAICRM.Shared.AI;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT
{
    public interface ICRMQueryService
    {
        Task<List<Dictionary<string, object>>> ExecuteRawSqlAsync(string sql, Guid tenantId, bool isSuperAdmin, string userId = "");
        Task<AIResponse> ProcessCommandAsync(string message, Guid tenantId, string userId, bool isSuperAdmin, List<string> allowedModules, List<AIChatHistoryDto> history = null);
        Task<AIResponse> ProcessChatRequestAsync(AIRequest request, Guid tenantId, string userId, bool isSuperAdmin, List<string> allowedModules);
        Task<List<string>> GetUserAllowedModulesAsync(string userId);
    }
}
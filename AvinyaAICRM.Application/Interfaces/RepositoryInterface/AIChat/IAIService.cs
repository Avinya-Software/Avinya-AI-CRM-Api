using AvinyaAICRM.Shared.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat
{
    public interface IAIService
    {
        bool PreferRawGeneration { get; }
        Task<AIResponse> AnalyzeMessageAsync(string userMessage, Guid tenantId, bool isAdmin, List<string> allowedModules, List<AIChatHistoryDto> history = null);
        Task<AIResponse> RefineTemplateAsync(string userMessage, string templateSql, Guid tenantId, bool isSuperAdmin);
        Task<string> FixSqlAsync(string badSql, string errorMessage, string originalQuestion, Guid tenantId, bool isSuperAdmin);
        Task<AIResponse> RefineQueryAsync(string originalMessage, string badSql, string userCorrection, Guid tenantId);
    }
}

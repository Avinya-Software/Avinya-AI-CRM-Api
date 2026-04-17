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
        Task<AIResponse> AnalyzeMessageAsync(string userMessage, Guid tenantId, bool isAdmin, List<string> allowedModules);
        Task<string> FixSqlAsync(string badSql, string errorMessage, string originalQuestion, Guid tenantId, bool isSuperAdmin);
    }
}

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
        Task<AIResponse> GetIntentAsync(string userMessage);
        Task<SQLAIResponse> GenerateSqlAsync(string userMessage, Guid tenantId, bool isSuperAdmin, List<string> allowedModules);
    }
}

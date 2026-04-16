using AvinyaAICRM.Shared.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat
{
    public interface IDynamicQueryBuilder
    {
        (string sql, Dictionary<string, object> parameters) BuildQuery(QueryRequest request, Guid tenantId, string userId, bool isAdmin, List<string> allowedModules);
        QueryRequest NormalizeRequest(AIResponse aiResponse);
    }
}

using AvinyaAICRM.Domain.Entities.AI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.AI
{
    public interface IAIKnowledgeRepository
    {
        Task<AIQueryKnowledge?> GetByMessageAsync(string message);
        Task<AIQueryKnowledge> AddAsync(AIQueryKnowledge knowledge);
        Task UpdateAsync(AIQueryKnowledge knowledge);
        Task SaveChangesAsync();
    }
}

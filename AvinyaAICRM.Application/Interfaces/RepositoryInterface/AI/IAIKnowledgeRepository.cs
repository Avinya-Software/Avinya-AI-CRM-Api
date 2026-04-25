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

        /// <summary>
        /// Returns up to <paramref name="count"/> random OriginalMessage values
        /// from the knowledge base (excluding the current message) to use as suggestions.
        /// </summary>
        Task<List<string>> GetRandomSuggestionsAsync(string excludeMessage, int count = 4);
    }
}

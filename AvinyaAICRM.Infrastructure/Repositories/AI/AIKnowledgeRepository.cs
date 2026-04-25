using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AI;
using AvinyaAICRM.Domain.Entities.AI;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Repositories.AI
{
    public class AIKnowledgeRepository : IAIKnowledgeRepository
    {
        private readonly AppDbContext _context;

        public AIKnowledgeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AIQueryKnowledge?> GetByMessageAsync(string message)
        {
            var cleanMessage = message.Trim().ToLower();
            return await _context.AIQueryKnowledge
                .Where(x => x.OriginalMessage.ToLower() == cleanMessage)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<AIQueryKnowledge> AddAsync(AIQueryKnowledge knowledge)
        {
            await _context.AIQueryKnowledge.AddAsync(knowledge);
            return knowledge;
        }

        public async Task UpdateAsync(AIQueryKnowledge knowledge)
        {
            _context.AIQueryKnowledge.Update(knowledge);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetRandomSuggestionsAsync(string excludeMessage, int count = 4)
        {
            var clean = excludeMessage.Trim().ToLower();
            // Only surface queries the user has explicitly marked as good
            return await _context.AIQueryKnowledge
                .Where(x => x.OriginalMessage.ToLower() != clean && x.IsPositiveFeedback == true)
                .OrderBy(x => Guid.NewGuid())
                .Select(x => x.OriginalMessage)
                .Take(count)
                .ToListAsync();
        }
    }
}

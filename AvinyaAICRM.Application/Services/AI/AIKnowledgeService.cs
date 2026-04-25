using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AI;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using AvinyaAICRM.Domain.Entities.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.AI
{
    public class AIKnowledgeService : IAIKnowledgeService
    {
        private readonly IAIKnowledgeRepository _repository;

        public AIKnowledgeService(IAIKnowledgeRepository repository)
        {
            _repository = repository;
        }

        public async Task<string?> GetVerifiedQueryAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var knowledge = await _repository.GetByMessageAsync(message);
            
            if (knowledge != null && knowledge.IsPositiveFeedback == true)
            {
                return knowledge.GeneratedSql;
            }

            return null;
        }

        public async Task SaveFeedbackAsync(string message, string sql, bool isGood, string? userId, string? correction = null)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var existing = await _repository.GetByMessageAsync(message);

            if (existing != null)
            {
                existing.GeneratedSql = sql;
                existing.IsPositiveFeedback = isGood;
                existing.UserCorrection = correction;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.CreatedBy = userId ?? existing.CreatedBy;
                
                await _repository.UpdateAsync(existing);
            }
            else
            {
                var newKnowledge = new AIQueryKnowledge
                {
                    Id = Guid.NewGuid(),
                    OriginalMessage = message.Trim(),
                    GeneratedSql = sql,
                    IsPositiveFeedback = isGood,
                    UserCorrection = correction,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId ?? "System"
                };
                await _repository.AddAsync(newKnowledge);
            }

            await _repository.SaveChangesAsync();
        }

        public async Task RecordFirstTimeQueryAsync(string message, string sql, string userId)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(sql)) return;

            var existing = await _repository.GetByMessageAsync(message);
            if (existing == null)
            {
                var newKnowledge = new AIQueryKnowledge
                {
                    Id = Guid.NewGuid(),
                    OriginalMessage = message.Trim(),
                    GeneratedSql = sql,
                    IsPositiveFeedback = null, // Pending
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId ?? "System"
                };
                await _repository.AddAsync(newKnowledge);
                await _repository.SaveChangesAsync();
            }
        }

        public async Task<List<string>> GetRandomSuggestionsAsync(string excludeMessage, int count = 4)
        {
            return await _repository.GetRandomSuggestionsAsync(excludeMessage, count);
        }
    }
}

using AvinyaAICRM.Application.AI.Models;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using AvinyaAICRM.Shared.AI;
using Microsoft.Extensions.Logging;

namespace AvinyaAICRM.Application.AI.Pipeline
{
    public class AIPipeline
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIPipeline> _logger;
        private readonly ICreditService _creditService;
        private readonly IAIKnowledgeService _knowledge;

        public AIPipeline(
            IAIService aiService,
            ILogger<AIPipeline> logger,
            ICreditService creditService,
            IAIKnowledgeService knowledge)
        {
            _aiService = aiService;
            _logger = logger;
            _creditService = creditService;
            _knowledge = knowledge;
        }

        public async Task<PipelineResult> ProcessAsync(
            string message, Guid tenantId, string userId, bool isSuperAdmin,
            List<string> allowedModules, List<AIChatHistoryDto> history)
        {
            _logger.LogInformation("Processing message: {Message}", message);
            var result = new PipelineResult { OriginalMessage = message };

            // 0. Credit Check
            await _creditService.EnsureUserCreditExistsAsync(userId, tenantId);
            var currentBalance = await _creditService.GetRemainingCreditsAsync(userId);
            if (currentBalance <= 0)
            {
                result.ErrorMessage = "You have run out of AI credits. Please top up to continue using the assistant.";
                result.RemainingCredits = 0;
                return result;
            }

            // 1. Check Verified Knowledge Base (Reuse 'Good' queries from DB)
            var verifiedSql = await _knowledge.GetVerifiedQueryAsync(message);
            if (!string.IsNullOrEmpty(verifiedSql))
            {
                _logger.LogInformation("Knowledge Base Hit for message: {Message}", message);
                result.Sql = verifiedSql;
                result.Source = "knowledge_base";
                result.Action = "get_summary";
                result.TotalTokens = 100; // Verified knowledge is cheap

                // Populate follow-up suggestions from other known queries in the knowledge base
                result.Suggestions = await _knowledge.GetRandomSuggestionsAsync(message, 4);

                return await ReturnWithBalanceAsync(result, userId);
            }

            // 2. AI SQL Generation (Groq)
            _logger.LogInformation("Requesting AI Generation for: {Message}", message);
            result.Source = "ai_sql";

            var aiSqlResponse = await _aiService.AnalyzeMessageAsync(message, tenantId, isSuperAdmin, allowedModules, history);

            result.PromptTokens = aiSqlResponse.PromptTokens;
            result.ResponseTokens = aiSqlResponse.ResponseTokens;
            result.TotalTokens = aiSqlResponse.TotalTokens;
            result.Sql = aiSqlResponse.Sql;
            result.Action = aiSqlResponse.Action;
            result.Intent = aiSqlResponse.Intent;
            result.Parameters = aiSqlResponse.Parameters;
            result.SuccessMessage = aiSqlResponse.SuccessMessage;
            result.ErrorMessage = aiSqlResponse.ErrorMessage;
            result.Suggestions = aiSqlResponse.Suggestions;

            return await ReturnWithBalanceAsync(result, userId);
        }

        private async Task<PipelineResult> ReturnWithBalanceAsync(PipelineResult result, string userId)
        {
            result.RemainingCredits = await _creditService.GetRemainingCreditsAsync(userId);
            return result;
        }
    }
}

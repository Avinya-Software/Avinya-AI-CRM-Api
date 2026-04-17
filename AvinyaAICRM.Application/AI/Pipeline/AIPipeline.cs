using AvinyaAICRM.Application.AI.Models;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Shared.AI;
using Microsoft.Extensions.Logging;

namespace AvinyaAICRM.Application.AI.Pipeline
{
    public class AIPipeline
    {
        private readonly LocalIntentClassifier _classifier;
        private readonly SqlTemplateEngine _templates;
        private readonly QueryCache _cache;
        private readonly SqlValidator _validator;
        private readonly IAIService _aiService;
        private readonly ILogger<AIPipeline> _logger;
        private readonly IntentStore _trainedStore;
        private readonly ICreditService _creditService;

        public AIPipeline(
            LocalIntentClassifier classifier,
            SqlTemplateEngine templates,
            QueryCache cache,
            SqlValidator validator,
            IAIService aiService,
            ILogger<AIPipeline> logger,
            IntentStore trainedStore,
            ICreditService creditService)
        {
            _classifier = classifier;
            _templates = templates;
            _cache = cache;
            _validator = validator;
            _aiService = aiService;
            _logger = logger;
            _trainedStore = trainedStore;
            _creditService = creditService;
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

            // 1. Local Classification
            var intent = _classifier.Classify(message);
            var filters = _classifier.ExtractFilters(message);
            result.Intent = intent.Intent;

            // 2. Check Action Actions (Create Lead/Task)
            if (intent.Intent is "create_lead" or "create_task")
            {
                result.Source = "ai_params";
                var aiResponse = await _aiService.AnalyzeMessageAsync(message, tenantId, isSuperAdmin, allowedModules);
                
                result.PromptTokens = aiResponse.PromptTokens;
                result.ResponseTokens = aiResponse.ResponseTokens;
                result.TotalTokens = aiResponse.TotalTokens;
                
                result.Action = aiResponse.Action;
                result.Parameters = aiResponse.Parameters;
                result.ClarificationMessage = aiResponse.ClarificationMessage;
                result.IsClarificationRequired = aiResponse.IsClarificationRequired;
                result.SuccessMessage = aiResponse.SuccessMessage;
                result.ErrorMessage = aiResponse.ErrorMessage;
                return await DeductAndReturnAsync(result, userId);
            }

            // 3. Check SQL Cache
            if (_cache.TryGetSql(message, tenantId, out var cachedSql))
            {
                _logger.LogInformation("Cache hit for message: {Message}", message);
                result.Sql = cachedSql;
                result.Source = "cache";
                result.Action = "get_summary";
                result.TotalTokens = 1; // Reward: Cache hits are very cheap
                return await DeductAndReturnAsync(result, userId);
            }

            // 4. Try Template
            if (!intent.NeedsAI)
            {
                var templateSql = _templates.TryGetTemplateSql(intent, filters, tenantId, userId);
                if (templateSql != null)
                {
                    // Check if we need AI refinement (Is it a fuzzy/complex filter?)
                    bool needsRefinement = (filters.TimePeriod == "" && filters.ExplicitDate == null && filters.ExplicitStatus == null && ContainsFilterWords(message)) || message.Split(' ').Length > 8;

                    if (needsRefinement)
                    {
                        _logger.LogInformation("Template found but needs AI refinement: {Intent}", intent.Intent);
                        var refined = await _aiService.RefineTemplateAsync(message, templateSql, tenantId, isSuperAdmin);
                        
                        result.PromptTokens = refined.PromptTokens;
                        result.ResponseTokens = refined.ResponseTokens;
                        // Minimum charge: If AI fails (0 tokens) but we fallback to template, charge 5 tokens
                        result.TotalTokens = refined.TotalTokens > 0 ? refined.TotalTokens : 5;

                        result.Sql = string.IsNullOrEmpty(refined.Sql) ? templateSql : refined.Sql;
                        result.Source = "template_hybrid";
                        result.Action = refined.Action;
                        result.SuccessMessage = refined.SuccessMessage;
                        return await DeductAndReturnAsync(result, userId);
                    }

                    var validation = _validator.Validate(templateSql, tenantId, isSuperAdmin);
                    if (validation.IsValid)
                    {
                        _logger.LogInformation("Template matching for intent: {Intent}", intent.Intent);
                        result.Sql = templateSql;
                        result.Source = "template";
                        result.Action = "get_summary";
                        result.TotalTokens = 5; // Reward: Templates are cheaper than AI generation
                        _cache.SetSql(message, tenantId, templateSql);
                        return await DeductAndReturnAsync(result, userId);
                    }
                }
            }

            // 5. AI Fallback (SQL Generation)
            _logger.LogInformation("Falling back to AI for intent: {Intent}", intent.Intent);
            result.Source = "ai_sql";
            
            var aiSqlResponse = await _aiService.AnalyzeMessageAsync(message, tenantId, isSuperAdmin, allowedModules);
            
            result.PromptTokens = aiSqlResponse.PromptTokens;
            result.ResponseTokens = aiSqlResponse.ResponseTokens;
            result.TotalTokens = aiSqlResponse.TotalTokens;

            result.Sql = aiSqlResponse.Sql;
            result.Action = aiSqlResponse.Action;
            result.Intent = aiSqlResponse.Intent;
            result.SuccessMessage = aiSqlResponse.SuccessMessage;
            result.ErrorMessage = aiSqlResponse.ErrorMessage;
            
            // Learning logic...
            if (intent.Intent == "unknown" && !string.IsNullOrEmpty(aiSqlResponse.Intent))
            {
                _trainedStore.Train(message, aiSqlResponse.Intent);
            }

            // Cache if valid SQL
            if (!string.IsNullOrEmpty(result.Sql))
            {
                var validation = _validator.Validate(result.Sql, tenantId, isSuperAdmin);
                if (validation.IsValid)
                {
                    _cache.SetSql(message, tenantId, result.Sql);
                }
            }

            return await DeductAndReturnAsync(result, userId);
        }

        private async Task<PipelineResult> DeductAndReturnAsync(PipelineResult result, string userId)
        {
            if (result.TotalTokens > 0)
            {
                await _creditService.DeductCreditsAsync(userId, result.TotalTokens, result.Source.ToUpper());
            }
            
            result.RemainingCredits = await _creditService.GetRemainingCreditsAsync(userId);
            return result;
        }

        private bool ContainsFilterWords(string msg)
        {
            var words = new[] { "last", "days", "weeks", "months", "from", "to", "between", "before", "after", "by", "for" };
            return words.Any(w => msg.ToLower().Contains(w));
        }
    }
}

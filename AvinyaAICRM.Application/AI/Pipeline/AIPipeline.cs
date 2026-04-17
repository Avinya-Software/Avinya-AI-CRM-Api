using AvinyaAICRM.Application.AI.Models;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Shared.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public AIPipeline(
            LocalIntentClassifier classifier,
            SqlTemplateEngine templates,
            QueryCache cache,
            SqlValidator validator,
            IAIService aiService,
            ILogger<AIPipeline> logger)
        {
            _classifier = classifier;
            _templates = templates;
            _cache = cache;
            _validator = validator;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<PipelineResult> ProcessAsync(
            string message, Guid tenantId, string userId, bool isSuperAdmin,
            List<string> allowedModules, List<AIChatHistoryDto> history)
        {
            _logger.LogInformation("Processing message: {Message}", message);
            var result = new PipelineResult { OriginalMessage = message };

            // 1. Local Classification
            var intent = _classifier.Classify(message);
            var filters = _classifier.ExtractFilters(message);
            result.Intent = intent.Intent;

            // 2. Check Action Actions (Create Lead/Task)
            if (intent.Intent is "create_lead" or "create_task")
            {
                result.Source = "ai_params";
                var aiResponse = await _aiService.AnalyzeMessageAsync(message, tenantId, isSuperAdmin, allowedModules);
                result.Action = aiResponse.Action;
                result.Parameters = aiResponse.Parameters;
                result.ClarificationMessage = aiResponse.ClarificationMessage;
                result.IsClarificationRequired = aiResponse.IsClarificationRequired;
                result.SuccessMessage = aiResponse.SuccessMessage;
                result.ErrorMessage = aiResponse.ErrorMessage;
                return result;
            }

            // 3. Check SQL Cache
            if (_cache.TryGetSql(message, tenantId, out var cachedSql))
            {
                _logger.LogInformation("Cache hit for message: {Message}", message);
                result.Sql = cachedSql;
                result.Source = "cache";
                result.Action = "get_summary";
                return result;
            }

            // 4. Try Template
            if (!intent.NeedsAI)
            {
                var templateSql = _templates.TryGetTemplateSql(intent, filters, tenantId, userId);
                if (templateSql != null)
                {
                    var validation = _validator.Validate(templateSql, tenantId, isSuperAdmin);
                    if (validation.IsValid)
                    {
                        _logger.LogInformation("Template matching for intent: {Intent}", intent.Intent);
                        result.Sql = templateSql;
                        result.Source = "template";
                        result.Action = "get_summary";
                        _cache.SetSql(message, tenantId, templateSql);
                        return result;
                    }
                }
            }

            // 5. AI Fallback (SQL Generation)
            _logger.LogInformation("Falling back to AI for intent: {Intent}", intent.Intent);
            result.Source = "ai_sql";
            
            // Note: We'll update IAIService to accept intent/filters/history for better context
            var aiSqlResponse = await _aiService.AnalyzeMessageAsync(message, tenantId, isSuperAdmin, allowedModules);
            
            result.Sql = aiSqlResponse.Sql;
            result.Action = aiSqlResponse.Action;
            result.SuccessMessage = aiSqlResponse.SuccessMessage;
            result.ErrorMessage = aiSqlResponse.ErrorMessage;
            result.ClarificationMessage = aiSqlResponse.ClarificationMessage;
            result.IsClarificationRequired = aiSqlResponse.IsClarificationRequired;

            // Cache if valid SQL
            if (!string.IsNullOrEmpty(result.Sql))
            {
                var validation = _validator.Validate(result.Sql, tenantId, isSuperAdmin);
                if (validation.IsValid)
                {
                    _cache.SetSql(message, tenantId, result.Sql);
                }
            }

            return result;
        }
    }
}

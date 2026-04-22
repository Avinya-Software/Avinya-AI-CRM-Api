using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Shared.AI;
using Microsoft.Extensions.Logging;

namespace AvinyaAICRM.Infrastructure.Repositories
{
    public class FallbackAIService : IAIService
    {
        private readonly GroqService _groq;
        private readonly GeminiService _gemini;
        private readonly ILogger<FallbackAIService> _logger;
        public bool PreferRawGeneration => _groq.PreferRawGeneration;

        public FallbackAIService(GroqService groq, GeminiService gemini, ILogger<FallbackAIService> logger)
        {
            _groq = groq;
            _gemini = gemini;
            _logger = logger;
        }

        public async Task<AIResponse> AnalyzeMessageAsync(string userMessage, Guid tenantId, bool isAdmin, List<string> allowedModules, List<AIChatHistoryDto> history = null)
        {
            try
            {
                _logger.LogInformation("Attempting AnalyzeMessageAsync with Groq...");
                var response = await _groq.AnalyzeMessageAsync(userMessage, tenantId, isAdmin, allowedModules, history);
                
                if (response != null && !string.IsNullOrEmpty(response.Sql) || response?.Action != "message" || !string.IsNullOrEmpty(response?.ErrorMessage))
                {
                    if (response.ErrorMessage?.Contains("AI service error") == true)
                    {
                        throw new Exception("Groq returned error: " + response.ErrorMessage);
                    }
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq AnalyzeMessageAsync failed. Falling back to Gemini.");
            }

            return await _gemini.AnalyzeMessageAsync(userMessage, tenantId, isAdmin, allowedModules, history);
        }

        public async Task<AIResponse> RefineTemplateAsync(string userMessage, string templateSql, Guid tenantId, bool isSuperAdmin)
        {
            try
            {
                _logger.LogInformation("Attempting RefineTemplateAsync with Groq...");
                var response = await _groq.RefineTemplateAsync(userMessage, templateSql, tenantId, isSuperAdmin);
                if (response != null && !string.IsNullOrEmpty(response.Sql))
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq RefineTemplateAsync failed. Falling back to Gemini.");
            }

            return await _gemini.RefineTemplateAsync(userMessage, templateSql, tenantId, isSuperAdmin);
        }

        public async Task<string> FixSqlAsync(string badSql, string errorMessage, string originalQuestion, Guid tenantId, bool isSuperAdmin)
        {
            try
            {
                _logger.LogInformation("Attempting FixSqlAsync with Groq...");
                var response = await _groq.FixSqlAsync(badSql, errorMessage, originalQuestion, tenantId, isSuperAdmin);
                if (!string.IsNullOrEmpty(response))
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq FixSqlAsync failed. Falling back to Gemini.");
            }

            return await _gemini.FixSqlAsync(badSql, errorMessage, originalQuestion, tenantId, isSuperAdmin);
        }

        public async Task<AIResponse> RefineQueryAsync(string originalMessage, string badSql, string userCorrection, Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Attempting RefineQueryAsync with Groq...");
                var response = await _groq.RefineQueryAsync(originalMessage, badSql, userCorrection, tenantId);
                if (response != null && !string.IsNullOrEmpty(response.Sql))
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq RefineQueryAsync failed. Falling back to Gemini.");
            }

            return await _gemini.RefineQueryAsync(originalMessage, badSql, userCorrection, tenantId);
        }
    }
}

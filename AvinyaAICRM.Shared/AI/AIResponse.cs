using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Shared.AI
{
    public class AIResponse
    {
        public string Action { get; set; } = "message";
        public string? Intent { get; set; } = "unknown";
        public string? DateRange { get; set; }

        // Standardized Output Fields
        public string Message { get; set; } = string.Empty;
        public List<Dictionary<string, object>> Data { get; set; } = new();
        public int Count { get; set; }
        public string? Query { get; set; }

        // SQL Reporting Fields
        public string? Sql { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // Extracted lead/client data (CompanyName, Email, Mobile, Notes, etc.)
        [System.Text.Json.Serialization.JsonPropertyName("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        // Clarification logic
        public bool IsClarificationRequired { get; set; }
        public string? ClarificationMessage { get; set; }
        public List<ClientDisambiguationDto>? SuggestedClients { get; set; }

        // Token Usage Metrics
        public int PromptTokens { get; set; }
        public int ResponseTokens { get; set; }
        public int ThoughtsTokens { get; set; }
        public int TotalTokens { get; set; }
        public int RemainingCredits { get; set; }

        public string? Summary { get; set; }
        public List<string>? Suggestions { get; set; }
        
        // Metadata
        public string Source { get; set; } = "ai"; // source of the response (ai, cache, template, etc)
    }

    public class ClientDisambiguationDto
    {
        public Guid ClientID { get; set; }
        public string? CompanyName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
    }
}
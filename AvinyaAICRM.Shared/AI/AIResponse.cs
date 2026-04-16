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
        public string? Intent { get; set; }
        public string? Type { get; set; } // "LIST" | "SUMMARY" | "DETAIL"
        public List<string>? Entities { get; set; } = new();
        public Dictionary<string, object>? Filters { get; set; } = new();
        
        // Final Output
        public string? Sql { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // Extracted data for create/update actions
        public Dictionary<string, object>? Parameters { get; set; } = new();

        // Parameters for the generated SQL query
        public Dictionary<string, object>? SqlQueryParameters { get; set; } = new();

        // Clarification logic
        public bool IsClarificationRequired { get; set; }
        public string? ClarificationMessage { get; set; }
        public List<ClientDisambiguationDto>? SuggestedClients { get; set; }
        public List<string>? Suggestions { get; set; }
    }

    public class ClientDisambiguationDto
    {
        public Guid ClientID { get; set; }
        public string? CompanyName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
    }
}

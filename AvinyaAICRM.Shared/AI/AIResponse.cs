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
        public string DateRange { get; set; }
        
        // SQL Reporting Fields
        public string? Sql { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // Extracted lead/client data (CompanyName, Email, Mobile, Notes, etc.)
        public Dictionary<string, string>? Parameters { get; set; } = new();

        // Clarification logic
        public bool IsClarificationRequired { get; set; }
        public string? ClarificationMessage { get; set; }
        public List<ClientDisambiguationDto>? SuggestedClients { get; set; }
    }

    public class ClientDisambiguationDto
    {
        public Guid ClientID { get; set; }
        public string? CompanyName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
    }
}

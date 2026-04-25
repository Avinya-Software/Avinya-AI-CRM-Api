using System;
using System.Collections.Generic;

namespace AvinyaAICRM.Application.AI.Models
{


    public class PipelineResult
    {
        public string OriginalMessage { get; set; } = "";
        public string Intent { get; set; } = "";
        public string? Sql { get; set; }
        public string? Action { get; set; }
        public Dictionary<string, string>? Parameters { get; set; }
        public string? ClarificationMessage { get; set; }
        public bool IsClarificationRequired { get; set; }
        public string Source { get; set; } = ""; // local/template/cache/ai
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // Token Usage & Credits
        public int PromptTokens { get; set; }
        public int ResponseTokens { get; set; }
        public int TotalTokens { get; set; }
        public int CreditsUsed { get; set; }
        public int RemainingCredits { get; set; }
        public List<string>? Suggestions { get; set; }
    }
}

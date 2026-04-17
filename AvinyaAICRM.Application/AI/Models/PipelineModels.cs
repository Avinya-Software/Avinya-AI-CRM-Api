using System;
using System.Collections.Generic;

namespace AvinyaAICRM.Application.AI.Models
{
    public class ClassificationResult
    {
        public string Intent { get; set; } = "unknown";
        public double Confidence { get; set; }
        public bool NeedsAI => Confidence < 0.5;
    }

    public class FilterResult
    {
        public string TimePeriod { get; set; } = "";
        public string Status { get; set; } = "";
        public string SearchTerm { get; set; } = "";
        public bool IsCountQuery { get; set; }
        public bool IsSumQuery { get; set; }
        public bool IsPersonalQuery { get; set; }
        public int? Limit { get; set; }
        public DateTime? ExplicitDate { get; set; }
        public string? ExplicitStatus { get; set; }
        public string? ExplicitSource { get; set; }
    }

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
        public int RemainingCredits { get; set; }
    }
}

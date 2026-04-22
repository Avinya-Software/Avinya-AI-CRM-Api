using System;

namespace AvinyaAICRM.Application.DTOs.AI
{
    public class AIFeedbackDto
    {
        public string OriginalMessage { get; set; } = string.Empty;
        public string GeneratedSql { get; set; } = string.Empty;
        public bool IsGood { get; set; }
        public string? UserCorrection { get; set; }
    }
}

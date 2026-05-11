namespace AvinyaAICRM.Shared.AI
{
    public class ChatResponse
    {
        /// <summary>Human-written AI response message.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Rows returned from the query.</summary>
        public List<Dictionary<string, object>> Data { get; set; } = new();

        /// <summary>Total number of records returned.</summary>
        public int Count { get; set; }

        /// <summary>What the AI did: query | create_lead | create_task | create_expense | message</summary>
        public string Action { get; set; } = "message";

        /// <summary>Populated only for create actions — the parameters extracted by AI.</summary>
        public Dictionary<string, object>? Parameters { get; set; }

        /// <summary>Follow-up question suggestions.</summary>
        public List<string>? Suggestions { get; set; }

        /// <summary>Credits consumed by this request.</summary>
        public int CreditsUsed { get; set; }

        /// <summary>Remaining credits for the user.</summary>
        public int RemainingCredits { get; set; }

        /// <summary>Set only when something goes wrong — permission denied, missing fields, etc.</summary>
        public string? ErrorMessage { get; set; }
    }
}

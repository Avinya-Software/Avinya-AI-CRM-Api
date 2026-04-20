namespace AvinyaAICRM.Shared.AI
{
    public class AIRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<AIChatHistoryDto> History { get; set; } = new();
    }

    public class AIChatHistoryDto
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }
}

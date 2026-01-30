
namespace AvinyaAICRM.Application.Validators
{
    public static class VoiceRecurrenceParser
    {
        public static (bool IsRecurring, string? Rule) Parse(string text)
        {
            text = text.ToLower();

            if (text.Contains("roz") || text.Contains("daily"))
                return (true, "FREQ=DAILY");

            if (text.Contains("weekly") || text.Contains("har week"))
                return (true, "FREQ=WEEKLY");

            if (text.Contains("har friday"))
                return (true, "FREQ=WEEKLY;BYDAY=FR");

            if (text.Contains("monthly") || text.Contains("har mahine"))
                return (true, "FREQ=MONTHLY");

            return (false, null);
        }
    }

}

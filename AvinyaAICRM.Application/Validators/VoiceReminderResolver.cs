namespace AvinyaAICRM.Application.Validators
{
    public static class VoiceReminderResolver
    {
        public static DateTime? ResolveReminder(string text, DateTime? dueDateUtc)
        {
            if (!dueDateUtc.HasValue)
                return null;

            text = text.ToLowerInvariant();

            if (text.Contains("remind") || text.Contains("yaad dila"))
            {
                // Default: 30 minutes before due date
                return dueDateUtc.Value.AddMinutes(-30);
            }

            // You can add more rules here later (e.g. "remind me 1 hour before", "night before")
            return null;
        }
    }
}

namespace AvinyaAICRM.Application.Validators
{
    public static class VoiceReminderResolver
    {
        public static DateTime? ResolveReminder(string text, DateTime? dueDate)
        {
            if (!dueDate.HasValue) return null;

            text = text.ToLower();

            if (text.Contains("yaad dila") || text.Contains("remind"))
            {
                // default: 30 minutes before
                return dueDate.Value.AddMinutes(-30);
            }

            return null;
        }
    }

}

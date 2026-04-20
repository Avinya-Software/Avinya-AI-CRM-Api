using System.Text.RegularExpressions;

namespace AvinyaAICRM.Application.Validators
{
    /// <summary>
    /// Parses Hindi and English voice text for task recurrence rules (RRULE format).
    /// Supports: daily, weekly, monthly, fortnightly, and specific day-of-week patterns.
    /// </summary>
    public static class VoiceRecurrenceParser
    {
        public static (bool IsRecurring, string? Rule) Parse(string text)
        {
            text = text.ToLowerInvariant();

            // ── Daily ──────────────────────────────────────────────────────────
            if (Regex.IsMatch(text, @"\b(roz|roz roz|har din|daily|every day|everyday)\b"))
                return (true, "FREQ=DAILY");

            // ── Weekly (generic) ───────────────────────────────────────────────
            if (Regex.IsMatch(text, @"\b(weekly|har week|har hafte|every week|har hafta)\b"))
                return (true, "FREQ=WEEKLY");

            // ── Fortnightly ────────────────────────────────────────────────────
            if (Regex.IsMatch(text, @"\b(every two weeks?|har do hafte|fortnightly)\b"))
                return (true, "FREQ=WEEKLY;INTERVAL=2");

            // ── Monthly ────────────────────────────────────────────────────────
            if (Regex.IsMatch(text, @"\b(monthly|har mahine|har maheene|every month|har mas)\b"))
                return (true, "FREQ=MONTHLY");

            // ── Specific days of week ──────────────────────────────────────────
            var byDay = BuildByDayRule(text);
            if (byDay != null)
                return (true, $"FREQ=WEEKLY;BYDAY={byDay}");

            return (false, null);
        }

        /// <summary>
        /// Looks for "har [day]" or "every [day]" patterns and builds a BYDAY value.
        /// Supports multiple days — e.g. "har somwar aur shukrawar" → "MO,FR"
        /// </summary>
        private static string? BuildByDayRule(string text)
        {
            var days = new List<string>();

            if (Regex.IsMatch(text, @"\b(har raviwar|har itwar|every sunday)\b"))    days.Add("SU");
            if (Regex.IsMatch(text, @"\b(har somwar|every monday)\b"))               days.Add("MO");
            if (Regex.IsMatch(text, @"\b(har mangalwar|every tuesday)\b"))           days.Add("TU");
            if (Regex.IsMatch(text, @"\b(har budhwar|every wednesday)\b"))           days.Add("WE");
            if (Regex.IsMatch(text, @"\b(har guruwar|har brihaspatiwar|every thursday)\b")) days.Add("TH");
            if (Regex.IsMatch(text, @"\b(har shukrawar|every friday)\b"))            days.Add("FR");
            if (Regex.IsMatch(text, @"\b(har shaniwar|every saturday)\b"))           days.Add("SA");

            // Weekdays shorthand
            if (Regex.IsMatch(text, @"\b(weekdays|weekday|mon to fri|har weekday)\b"))
                return "MO,TU,WE,TH,FR";

            // Weekends shorthand
            if (Regex.IsMatch(text, @"\b(weekends?|saturday sunday|har weekend)\b"))
                return "SA,SU";

            return days.Count > 0 ? string.Join(",", days) : null;
        }
    }
}

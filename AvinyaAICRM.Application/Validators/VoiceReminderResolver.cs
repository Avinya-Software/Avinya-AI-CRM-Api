using System.Text.RegularExpressions;

namespace AvinyaAICRM.Application.Validators
{
    /// <summary>
    /// Resolves a reminder DateTime from voice text.
    /// Supports Hindi and English patterns like:
    ///   "remind me 30 minutes before", "1 ghante pehle yaad dilana",
    ///   "kal subah yaad dila", "30 minute baad remind karna"
    /// </summary>
    public static class VoiceReminderResolver
    {
        public static DateTime? ResolveReminder(string text, DateTime? dueDateUtc)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.ToLowerInvariant();

            bool hasReminderIntent =
                text.Contains("remind")       ||
                text.Contains("yaad dila")    ||
                text.Contains("yaad kar")     ||
                text.Contains("alert")        ||
                text.Contains("notification") ||
                text.Contains("batana")       ||
                text.Contains("bolna");

            if (!hasReminderIntent)
                return null;

            // ── "N minutes pehle" / "N minute before" ──────────────────────────
            var minBeforeMatch = Regex.Match(text,
                @"\b(\d+|ek|do|teen|chaar|paanch|das|bees|tees|pachas)\s*(minute|min|minutes?)\s*(pehle|pahle|pehle se|before)\b",
                RegexOptions.IgnoreCase);

            if (minBeforeMatch.Success && dueDateUtc.HasValue)
            {
                int mins = ParseNumber(minBeforeMatch.Groups[1].Value);
                return dueDateUtc.Value.AddMinutes(-mins);
            }

            // ── "N ghante pehle" / "N hour before" ────────────────────────────
            var hrBeforeMatch = Regex.Match(text,
                @"\b(ek|1|do|2|teen|3|chaar|4|paanch|5)\s*(ghante?|ghanta|hour|hours?)\s*(pehle|pahle|before)\b",
                RegexOptions.IgnoreCase);

            if (hrBeforeMatch.Success && dueDateUtc.HasValue)
            {
                int hrs = WordToNumber(hrBeforeMatch.Groups[1].Value);
                return dueDateUtc.Value.AddHours(-hrs);
            }

            // ── "N minute baad remind" / "remind after N minutes" ──────────────
            // (reminder set N minutes from NOW, not before due)
            var minAfterMatch = Regex.Match(text,
                @"\b(\d+)\s*(minute|min|minutes?)\s*(baad|bad|after)\s*remind\b",
                RegexOptions.IgnoreCase);

            if (minAfterMatch.Success)
            {
                var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                var nowIst  = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
                return TimeZoneInfo.ConvertTimeToUtc(nowIst.AddMinutes(int.Parse(minAfterMatch.Groups[1].Value)), istZone);
            }

            // ── "1 din pehle" / "night before" / "ek din pehle" ───────────────
            var dayBeforeMatch = Regex.Match(text,
                @"\b(ek|1|do|2)\s*(din|day|days?)\s*(pehle|pahle|before)\b",
                RegexOptions.IgnoreCase);

            if (dayBeforeMatch.Success && dueDateUtc.HasValue)
            {
                int days = WordToNumber(dayBeforeMatch.Groups[1].Value);
                return dueDateUtc.Value.AddDays(-days);
            }
            if ((text.Contains("night before") || text.Contains("ek raat pehle") || text.Contains("agle din subah")) && dueDateUtc.HasValue)
                return dueDateUtc.Value.AddHours(-12);

            // ── Default: 30 minutes before due date ───────────────────────────
            if (dueDateUtc.HasValue)
                return dueDateUtc.Value.AddMinutes(-30);
           

            return null;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static int ParseNumber(string s)
        {
            if (int.TryParse(s, out int n)) return n;
            return s.ToLower() switch
            {
                "ek"     => 1,
                "do"     => 2,
                "teen"   => 3,
                "chaar"  => 4,
                "paanch" => 5,
                "das"    => 10,
                "bees"   => 20,
                "tees"   => 30,
                "pachas" => 50,
                _        => 30
            };
        }

        private static int WordToNumber(string word) => word.ToLower() switch
        {
            "ek"    or "1" => 1,
            "do"    or "2" => 2,
            "teen"  or "3" => 3,
            "chaar" or "4" => 4,
            "paanch"or "5" => 5,
            _ => 1
        };
    }
}
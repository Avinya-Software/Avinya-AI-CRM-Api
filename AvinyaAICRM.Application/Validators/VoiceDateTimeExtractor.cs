using System;
using System.Text.RegularExpressions;

namespace AvinyaAICRM.Application.Validators
{
    public static class VoiceDateTimeExtractor
    {
        private static readonly TimeZoneInfo IstTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        public static DateTime? ExtractDueDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.ToLowerInvariant().Trim();

            var nowUtc = DateTime.UtcNow;
            var nowIst = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, IstTimeZone);

            DateTime baseDateIst = nowIst.Date;

            // =======================
            // 1. DATE PARSING (Today, Tomorrow, Specific Date)
            // =======================
            var specificDateMatch = Regex.Match(text,
                @"\b(\d{1,2})(?:st|nd|rd|th)?\s*(?:of\s*)?(january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\b",
                RegexOptions.IgnoreCase);

            if (specificDateMatch.Success)
            {
                int day = int.Parse(specificDateMatch.Groups[1].Value);
                string monthStr = specificDateMatch.Groups[2].Value.ToLower();

                int month = monthStr switch
                {
                    "january" or "jan" => 1,
                    "february" or "feb" => 2,
                    "march" or "mar" => 3,
                    "april" or "apr" => 4,
                    "may" => 5,
                    "june" or "jun" => 6,
                    "july" or "jul" => 7,
                    "august" or "aug" => 8,
                    "september" or "sep" => 9,
                    "october" or "oct" => 10,
                    "november" or "nov" => 11,
                    "december" or "dec" => 12,
                    _ => nowIst.Month
                };

                int year = nowIst.Year;
                var candidate = new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
                if (candidate < nowIst.Date.AddDays(-1))
                    year++;

                baseDateIst = new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
            }
            else if (text.Contains("today") || text.Contains("aaj"))
            {
                baseDateIst = nowIst.Date;
            }
            else if (text.Contains("tomorrow") || text.Contains("kal"))
            {
                baseDateIst = nowIst.Date.AddDays(1);
            }

            // =======================
            // 2. RELATIVE TIME: "adha ghante baad", "ek ghante baad", "do ghante bad"
            // =======================
            var relativeMatch = Regex.Match(text,
                @"\b(adha|half|ek|1|do|2|teen|3)\s*(ghante?|ghanta|hour|hours?)\s*(baad|bad|mein|me)\b",
                RegexOptions.IgnoreCase);

            if (relativeMatch.Success)
            {
                string numWord = relativeMatch.Groups[1].Value.ToLower();
                int hoursToAdd = numWord switch
                {
                    "adha" or "half" => 0,           // we'll add 30 minutes later
                    "ek" or "1" => 1,
                    "do" or "2" => 2,
                    "teen" or "3" => 3,
                    _ => 1
                };

                DateTime due = nowIst.AddHours(hoursToAdd);

                if (numWord == "adha" || numWord == "half")
                    due = nowIst.AddMinutes(30);

                // Convert relative time directly to UTC and return
                return TimeZoneInfo.ConvertTimeToUtc(due, IstTimeZone);
            }

            // =======================
            // 3. ABSOLUTE TIME PARSING (5 baje, sham 5 baje, 4:30, 5:30 pm, cal 4:45, etc.)
            // =======================
            int hour = 23;
            int minute = 59;
            bool timeFound = false;

            // Strong regex for mixed voice input
            var timeMatch = Regex.Match(text,
                @"\b(\d{1,2})(?::|\.| )?(\d{2})?\s*(a\.?m\.?|p\.?m\.?|am|pm|a m|p m|sham|shaam|evening|dopahar|raat|subah|baje|tak)?\b",
                RegexOptions.IgnoreCase);

            if (timeMatch.Success)
            {
                timeFound = true;
                hour = int.Parse(timeMatch.Groups[1].Value);
                minute = timeMatch.Groups[2].Success && !string.IsNullOrEmpty(timeMatch.Groups[2].Value)
                    ? int.Parse(timeMatch.Groups[2].Value)
                    : 0;

                string extra = timeMatch.Groups[3].Value.ToLowerInvariant()
                    .Replace(".", "").Replace(" ", "");

                bool isPm = extra.Contains("pm") || extra.Contains("p") ||
                           extra.Contains("sham") || extra.Contains("shaam") ||
                           extra.Contains("evening") || extra.Contains("raat");

                bool isAfternoon = extra.Contains("dopahar");

                if ((isPm || isAfternoon) && hour < 12)
                    hour += 12;
                else if (!isPm && hour == 12)
                    hour = 0;   // 12 am = midnight

                if (hour > 23) hour = 23;
                if (minute > 59) minute = 59;
            }

            if (!timeFound)
            {
                // Default to end of day if no time mentioned
                hour = 23;
                minute = 59;
            }

            // =======================
            // 4. Build final DateTime in IST → Convert to UTC
            // =======================
            var dueDateIst = new DateTime(
                baseDateIst.Year,
                baseDateIst.Month,
                baseDateIst.Day,
                hour,
                minute,
                0,
                DateTimeKind.Unspecified
            );

            return TimeZoneInfo.ConvertTimeToUtc(dueDateIst, IstTimeZone);
        }
    }
}
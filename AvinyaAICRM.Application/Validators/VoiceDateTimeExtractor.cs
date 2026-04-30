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

            // =======================
            // 0. RELATIVE DURATION from NOW (Primary Intent)
            // Priority: "adha ghante baad", "ek ghante baad", "30 minute mein"
            // We check this FIRST to avoid falling through to "today" + default time.
            // =======================
            var relHourMatch = Regex.Match(text,
                @"\b(adha|half|ek|1|do|2|teen|3|chaar|4|paanch|5|dedh|dhai)\s*(ghante?|ghanta|hours?|hour|hr|h)\s*(ke\s*(?:ba+d?|mein|me|ko)|ba+d?|mein|me|ko)\b",
                RegexOptions.IgnoreCase);

            if (relHourMatch.Success)
            {
                string numWord = relHourMatch.Groups[1].Value.ToLower();
                DateTime due = numWord switch
                {
                    "adha" or "half" => nowIst.AddMinutes(30),
                    "dedh" => nowIst.AddMinutes(90),
                    "dhai" => nowIst.AddMinutes(150),
                    _ => nowIst.AddHours(WordToNumber(numWord))
                };
                return TimeZoneInfo.ConvertTimeToUtc(due, IstTimeZone);
            }

            var relMinMatch = Regex.Match(text,
                @"\b(\d+)\s*(minutes?|minuts?|mints?|mint|mins|min|m)\s*(ke\s*(?:ba+d?|mein|me|ko)|ba+d?|mein|me|ko)\b",
                RegexOptions.IgnoreCase);

            if (relMinMatch.Success)
            {
                int mins = int.Parse(relMinMatch.Groups[1].Value);
                return TimeZoneInfo.ConvertTimeToUtc(nowIst.AddMinutes(mins), IstTimeZone);
            }

            DateTime baseDateIst = nowIst.Date;

            // =======================
            // 1. DATE PARSING
            // Priority: specific calendar date > relative keywords > day-of-week
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
                if (candidate < nowIst.Date.AddDays(-1)) year++;

                baseDateIst = new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
            }
            else if (Regex.IsMatch(text, @"\b(today|aaj)\b"))
            {
                baseDateIst = nowIst.Date;
            }
            else if (Regex.IsMatch(text, @"\b(tomorrow|kal|kaal|cal)\b"))
            {
                baseDateIst = nowIst.Date.AddDays(1);
            }
            else if (Regex.IsMatch(text, @"\b(parso|parson|day after tomorrow)\b"))
            {
                baseDateIst = nowIst.Date.AddDays(2);
            }
            else if (Regex.IsMatch(text, @"\b(narso|narsoo|nurson|tarsoo|tarson)\b"))
            {
                baseDateIst = nowIst.Date.AddDays(3);
            }
            else
            {
                // --- Relative: "N din baad" (N days later) ---
                var relDayMatch = Regex.Match(text,
                    @"\b(ek|1|do|2|teen|3|chaar|4|paanch|5|chhe|6|saat|7|aath|8|nau|9|das|10)\s*(din|day|days?)\s*(baad|bad|mein|me|ke\s*baad|ke\s*bad|after)\b",
                    RegexOptions.IgnoreCase);

                if (relDayMatch.Success)
                {
                    baseDateIst = nowIst.Date.AddDays(WordToNumber(relDayMatch.Groups[1].Value));
                }
                else
                {
                    // --- Relative: "N hafte baad" (N weeks later) ---
                    var relWeekMatch = Regex.Match(text,
                        @"\b(ek|1|do|2|teen|3|chaar|4)\s*(hafte?|week|weeks?)\s*(baad|bad|mein|me|ke\s*baad|ke\s*bad|after)\b",
                        RegexOptions.IgnoreCase);

                    if (relWeekMatch.Success)
                    {
                        baseDateIst = nowIst.Date.AddDays(WordToNumber(relWeekMatch.Groups[1].Value) * 7);
                    }
                    else
                    {
                        // --- Hindi / English day-of-week: next occurrence ---
                        var dayOfWeek = ExtractDayOfWeek(text);
                        if (dayOfWeek.HasValue)
                            baseDateIst = NextDayOfWeek(nowIst.Date, dayOfWeek.Value);
                    }
                }
            }

            // =======================
            // 3. ABSOLUTE TIME PARSING
            // Detect period word FIRST for AM/PM disambiguation
            // =======================
            bool isMorning   = Regex.IsMatch(text, @"\b(subah|savere|morning)\b");
            bool isAfternoon = Regex.IsMatch(text, @"\b(dopahar|duphar|afternoon)\b");
            bool isEvening   = Regex.IsMatch(text, @"\b(sham|shaam|evening)\b");
            bool isNight     = Regex.IsMatch(text, @"\b(raat|night)\b");
            bool hasAmPm     = Regex.IsMatch(text, @"\b(am|pm|a\.m|p\.m)\b");

            int hour = 23;
            int minute = 59;
            int second = 59;
            bool timeFound = false;

            // Priority 1: "N baje" or "N:MM baje" — most common Hindi time anchor
            var bajeMatch = Regex.Match(text,
                @"\b(\d{1,2})(?:[:.](\d{2}))?\s*(?:baje|bajey|baj)\b",
                RegexOptions.IgnoreCase);

            if (bajeMatch.Success)
            {
                timeFound = true;
                hour   = int.Parse(bajeMatch.Groups[1].Value);
                minute = bajeMatch.Groups[2].Success ? int.Parse(bajeMatch.Groups[2].Value) : 0;
                second = 0;
            }

            // Priority 2: Explicit colon/dot time "5:30 pm", "17:30", "4.30 am"
            if (!timeFound)
            {
                var colonMatch = Regex.Match(text,
                    @"\b(\d{1,2})[:.](\d{2})\s*(a\.?m\.?|p\.?m\.?)?\b",
                    RegexOptions.IgnoreCase);

                if (colonMatch.Success)
                {
                    timeFound = true;
                    hour   = int.Parse(colonMatch.Groups[1].Value);
                    minute = int.Parse(colonMatch.Groups[2].Value);
                    second = 0;

                    string ampm = colonMatch.Groups[3].Success
                        ? colonMatch.Groups[3].Value.Replace(".", "").ToLowerInvariant()
                        : "";

                    if (ampm.Contains("pm") && hour < 12) hour += 12;
                    else if (ampm.Contains("am") && hour == 12) hour = 0;
                }
            }

            // Apply period-based adjustment (subah/shaam/raat etc.)
            if (timeFound)
            {
                hour = AdjustHourForPeriod(hour, isMorning, isAfternoon, isEvening, isNight);
            }

            // Priority 3: "subah 9", "sham 5", "raat 10" — period keyword followed by number
            if (!timeFound && (isMorning || isAfternoon || isEvening || isNight))
            {
                var periodNumMatch = Regex.Match(text,
                    @"\b(?:subah|savere|morning|dopahar|duphar|afternoon|sham|shaam|evening|raat|night)\s+(\d{1,2})\b",
                    RegexOptions.IgnoreCase);

                if (periodNumMatch.Success)
                {
                    timeFound = true;
                    hour   = int.Parse(periodNumMatch.Groups[1].Value);
                    minute = 0;
                    second = 0;
                    hour   = AdjustHourForPeriod(hour, isMorning, isAfternoon, isEvening, isNight);
                }
            }

            // Priority 4: Plain am/pm number — "5 pm", "11 am"
            if (!timeFound && hasAmPm)
            {
                var ampmMatch = Regex.Match(text,
                    @"\b(\d{1,2})\s*(am|pm|a\.m|p\.m)\b",
                    RegexOptions.IgnoreCase);

                if (ampmMatch.Success)
                {
                    timeFound = true;
                    hour   = int.Parse(ampmMatch.Groups[1].Value);
                    minute = 0;
                    second = 0;
                    string ap = ampmMatch.Groups[2].Value.Replace(".", "").ToLowerInvariant();
                    if (ap.Contains("pm") && hour < 12) hour += 12;
                    else if (ap.Contains("am") && hour == 12) hour = 0;
                }
            }

            // Fallback: only a period keyword, no number → sensible default times
            if (!timeFound)
            {
                if (isMorning)        { hour = 9;  minute = 0; second = 0; timeFound = true; }
                else if (isAfternoon) { hour = 13; minute = 0; second = 0; timeFound = true; }
                else if (isEvening)   { hour = 18; minute = 0; second = 0; timeFound = true; }
                else if (isNight)     { hour = 21; minute = 0; second = 0; timeFound = true; }
            }

            // Last resort: end of day
            if (!timeFound) { hour = 23; minute = 59; second = 59; }

            hour   = Math.Clamp(hour, 0, 23);
            minute = Math.Clamp(minute, 0, 59);

            // =======================
            // 4. Build final DateTime in IST → Convert to UTC
            // =======================
            var dueDateIst = new DateTime(
                baseDateIst.Year,
                baseDateIst.Month,
                baseDateIst.Day,
                hour, minute, second,
                DateTimeKind.Unspecified
            );

            return TimeZoneInfo.ConvertTimeToUtc(dueDateIst, IstTimeZone);
        }

        // ─── Private Helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Converts Hindi/English period words into the correct 24h hour.
        /// No period context → small 1-6 assumed PM; 7-11 assumed AM; 12+ left alone.
        /// </summary>
        private static int AdjustHourForPeriod(int hour, bool morning, bool afternoon, bool evening, bool night)
        {
            if (morning)
            {
                if (hour == 12) return 0;   // 12 subah = midnight (though rare)
                return hour;                // 7 subah = 07:00
            }
            if (afternoon)
            {
                if (hour < 12) return hour + 12;  // 1 dopahar = 13:00
                return hour;
            }
            if (evening)
            {
                if (hour < 12) return hour + 12;  // 5 sham = 17:00
                return hour;
            }
            if (night)
            {
                // Hinglish context: "raat ke 9" = 21:00, but "raat ke 2" = 02:00
                if (hour >= 7 && hour <= 11) return hour + 12;
                if (hour == 12) return 0; // raat ke 12 = midnight
                return hour; // 1, 2, 3, 4, 5, 6 are naturally AM
            }
            // No period context: 1-6 assumed AM (late night/early morning), 7-12 assumed PM contextually? 
            // Actually usually 1-6 PM is common. But user specifically wants "raat 5" to be AM.
            // If No period context, we keep the original 1-6 PM assumption unless it's very early.
            // Actually, let's keep it safe:
            if (hour >= 1 && hour <= 6) return hour + 12; // Default is PM
            return hour;
        }

        /// <summary>Maps Hindi/English number words to int.</summary>
        private static int WordToNumber(string word) => word.ToLower() switch
        {
            "ek"    or "1" => 1,
            "do"    or "2" => 2,
            "teen"  or "3" => 3,
            "chaar" or "4" => 4,
            "paanch"or "5" => 5,
            "chhe"  or "6" => 6,
            "saat"  or "7" => 7,
            "aath"  or "8" => 8,
            "nau"   or "9" => 9,
            "das"   or "10"=> 10,
            _ => 1
        };

        /// <summary>
        /// Detects Hindi or English day-of-week keywords in the input text.
        /// Returns the matching <see cref="DayOfWeek"/> or null if none found.
        /// </summary>
        private static DayOfWeek? ExtractDayOfWeek(string text)
        {
            if (Regex.IsMatch(text, @"\b(raviwar|aaithwar|itwar|itwaar|sunday)\b"))   return DayOfWeek.Sunday;
            if (Regex.IsMatch(text, @"\b(somwar|soomwar|monday)\b"))                   return DayOfWeek.Monday;
            if (Regex.IsMatch(text, @"\b(mangalwar|mangalvaar|tuesday)\b"))            return DayOfWeek.Tuesday;
            if (Regex.IsMatch(text, @"\b(budhwar|budhvaar|wednesday)\b"))              return DayOfWeek.Wednesday;
            if (Regex.IsMatch(text, @"\b(guruwar|guruvaar|brihaspatiwar|thursday)\b")) return DayOfWeek.Thursday;
            if (Regex.IsMatch(text, @"\b(shukrawar|shukravaar|friday)\b"))             return DayOfWeek.Friday;
            if (Regex.IsMatch(text, @"\b(shaniwar|shanivaar|saturday)\b"))             return DayOfWeek.Saturday;
            return null;
        }

        /// <summary>Returns the next occurrence of <paramref name="target"/> after <paramref name="from"/>.</summary>
        private static DateTime NextDayOfWeek(DateTime from, DayOfWeek target)
        {
            int daysUntil = ((int)target - (int)from.DayOfWeek + 7) % 7;
            if (daysUntil == 0) daysUntil = 7; // if today is the target day, go to next week
            return from.AddDays(daysUntil);
        }
    }
}

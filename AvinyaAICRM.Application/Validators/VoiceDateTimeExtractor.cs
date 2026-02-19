
using System.Globalization;
using System.Text.RegularExpressions;

namespace AvinyaAICRM.Application.Validators
{

    public static class VoiceDateTimeExtractor
    {
        public static DateTime? ExtractDueDate(string text)
        {
            text = text.ToLower();
            var now = DateTime.Now;
            DateTime baseDate = now.Date;

            var monthMatch = Regex.Match(text, @"(\d{1,2})\s+(january|february|march|april|may|june|july|august|september|october|november|december)");

            if (monthMatch.Success)
            {
                int day = int.Parse(monthMatch.Groups[1].Value);
                string monthName = monthMatch.Groups[2].Value;

                int month = DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture).Month;

                baseDate = new DateTime(now.Year, month, day);
            }
            else
            {
                var dateMatch = Regex.Match(text, @"(\d{1,2})\s*(tarikh|date)");

                if (dateMatch.Success)
                {
                    int day = int.Parse(dateMatch.Groups[1].Value);

                    // If date already passed this month → next month
                    if (day < now.Day)
                        baseDate = new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(day - 1);
                    else
                        baseDate = new DateTime(now.Year, now.Month, day);
                }
                else if (text.Contains("kal"))
                {
                    baseDate = now.Date.AddDays(1);
                }
                else if (text.Contains("aaj"))
                {
                    baseDate = now.Date;
                }
            }

            var timeMatch = Regex.Match(text, @"(\d{1,2})(:(\d{2}))?\s*(baje|pm|am)?");

            bool timeSpecified = timeMatch.Success;

            int hour;
            int minute;

            if (timeSpecified)
            {
                hour = int.Parse(timeMatch.Groups[1].Value);
                minute = timeMatch.Groups[3].Success
                    ? int.Parse(timeMatch.Groups[3].Value)
                    : 0;

                if (text.Contains("pm") || text.Contains("shaam") || text.Contains("raat"))
                {
                    if (hour < 12)
                        hour += 12;
                }

                if (text.Contains("am") || text.Contains("subah"))
                {
                    if (hour == 12)
                        hour = 0;
                }
            }
            else
            {
                if (text.Contains("aaj"))
                {
                    hour = 23;
                    minute = 59;
                }
                else if (text.Contains("kal"))
                {
                    hour = 23;
                    minute = 59;
                }
                else
                {
                    hour = DateTime.Now.Hour;
                    minute = DateTime.Now.Minute;
                }
            }


            return baseDate.AddHours(hour).AddMinutes(minute);
        }
    }

}


namespace AvinyaAICRM.Application.Validators
{
    public static class VoiceDateTimeExtractor
    {
        public static DateTime? ExtractDueDate(string text)
        {
            text = text.ToLower();
            var now = DateTime.Now;

            if (text.Contains("aaj"))
                return now.Date.AddHours(18); // default evening

            if (text.Contains("kal"))
                return now.Date.AddDays(1).AddHours(10);

            if (text.Contains("subah"))
                return now.Date.AddHours(9);

            if (text.Contains("shaam") || text.Contains("evening"))
                return now.Date.AddHours(18);

            if (text.Contains("raat") || text.Contains("night"))
                return now.Date.AddHours(21);

            return now.Date; // user didn't specify
        }
    }

}

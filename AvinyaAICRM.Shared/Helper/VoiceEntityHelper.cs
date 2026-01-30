
namespace AvinyaAICRM.Shared.Helper
{
    public static class VoiceEntityHelper
    {
        public static string GetPerson(string text)
        {
            if (text.Contains("ko"))
                return text.Split("ko")[0].Trim();

            return null;
        }

        public static string GetAction(string text)
        {
            text = text.ToLower();

            if (text.Contains("payment") || text.Contains("paisa"))
                return "Payment";
            if (text.Contains("call") || text.Contains("phone"))
                return "Call";
            if (text.Contains("email") || text.Contains("mail"))
                return "Email";

            return "General";
        }
    }

}

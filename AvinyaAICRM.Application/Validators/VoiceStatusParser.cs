using System.Text.RegularExpressions;

namespace AvinyaAICRM.Application.Validators
{
    public static class VoiceStatusParser
    {
        public static string ExtractStatus(string text)
        {
            text = text.ToLower();

            if (
                Regex.IsMatch(text,
                    @"\b(done|completed|complete|finished|submitted|closed|resolved)\b") ||

                Regex.IsMatch(text,
                    @"\b(ho gaya|ho gaya hai|gaya hai|kar diya|kar diya hai|de diya|de diya hai|diya hai|submit kar diya|complete kar diya|band kar diya|finish kar diya|bana diya hai|gaya hai)\b")
               )
            {
                return "Completed";
            }

            if (
                Regex.IsMatch(text,
                    @"\b(in progress|processing|working|ongoing|started|start kar diya|is coming)\b") ||

                Regex.IsMatch(text,
                    @"\b(kar raha|kar raha hu|kar raha hun|chal raha|kaam chal raha|process me|start kiya|working hai|ja raha hu|ja raha hun|aa rahi hai|a rahi hai)\b")
               )
            {
                return "InProgress";
            }

            if (
                Regex.IsMatch(text,
                    @"\b(pending|not started|todo|to do|open)\b") ||

                Regex.IsMatch(text,
                    @"\b(karna hai|dena hai|baaki hai|pending hai|abhi tak nahi hua|shuru nahi kiya|diya jayega|le lunga|kar dunga|de dunga)\b")
               )
            {
                return "Pending";
            }

            return "Pending";
        }
    }
}
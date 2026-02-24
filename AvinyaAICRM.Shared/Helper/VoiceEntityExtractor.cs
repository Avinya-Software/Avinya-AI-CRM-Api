

namespace AvinyaAICRM.Shared.Helper
{
    public class VoiceEntities
    {
        public string? TeamName { get; set; }
        public string? AssigneeName { get; set; }
        public bool IsTeamTask { get; set; }
    }

    public static class VoiceEntityExtractor
    {
        static readonly string[] TeamKeywords =
        {
        "team", "sabko", "everyone", "all members"
    };

        static readonly string[] KnownNames =
        {
        "manish","rahul","ankit","rohit","amit"
    };

        public static VoiceEntities Extract(string text)
        {
            text = text.ToLower();

            var result = new VoiceEntities();

            // TEAM detection
            if (TeamKeywords.Any(k => text.Contains(k)))
                result.IsTeamTask = true;

            // USER detection
            foreach (var name in KnownNames)
            {
                if (text.Contains(name))
                {
                    result.AssigneeName = name;
                    result.IsTeamTask = true;
                    break;
                }
            }

            // TEAM NAME detection (Sales team, HR team)
            var words = text.Split(' ');

            for (int i = 0; i < words.Length - 1; i++)
            {
                if (words[i + 1] == "team")
                {
                    result.TeamName = words[i];
                    result.IsTeamTask = true;
                }
            }

            return result;
        }
    }
}

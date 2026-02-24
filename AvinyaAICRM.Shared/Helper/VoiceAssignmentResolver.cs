using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Shared.Helper
{
    public static class VoiceAssignmentResolver
    {
        public static (string? assigneeName, bool isTeam) Resolve(string text)
        {
            text = text.ToLower();

            if (text.Contains("team") || text.Contains("sabko"))
                return (null, true);

            var knownUsers = new[]
            {
            "manish","rahul","ankit","rohit","amit"
        };

            foreach (var user in knownUsers)
            {
                if (text.Contains(user))
                    return (user, true);
            }

            return (null, false);
        }
    }

}

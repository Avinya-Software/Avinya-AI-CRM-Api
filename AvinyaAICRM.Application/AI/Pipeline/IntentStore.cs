using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AvinyaAICRM.Application.AI.Pipeline
{
    public class IntentStore
    {
        private static readonly ConcurrentDictionary<string, string> _phraseToIntent = new();
        private const string CacheFile = "intent_knowledge.json";

        static IntentStore()
        {
            Load();
        }

        public string? GetIntent(string phrase)
        {
            var lower = phrase.ToLower().Trim();
            if (_phraseToIntent.TryGetValue(lower, out var intent))
                return intent;

            // Simple "Contains" match for a few common trained phrases
            var key = _phraseToIntent.Keys.FirstOrDefault(k => lower.Contains(k));
            return key != null ? _phraseToIntent[key] : null;
        }

        public void Train(string phrase, string intent)
        {
            var lower = phrase.ToLower().Trim();
            if (intent == "unknown") return;

            _phraseToIntent[lower] = intent;
            Save();
        }

        private static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_phraseToIntent);
                File.WriteAllText(CacheFile, json);
            }
            catch { /* Ignore IO errors in memory-first cache */ }
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(CacheFile))
                {
                    var json = File.ReadAllText(CacheFile);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null)
                    {
                        foreach (var kv in data) _phraseToIntent[kv.Key] = kv.Value;
                    }
                }
            }
            catch { }
        }
    }
}

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AvinyaAICRM.Application.AI.Pipeline
{
    public class QueryCache
    {
        private static readonly ConcurrentDictionary<string, string> _sqlCache = new();
        private static readonly string CacheFile = "sql_knowledge.json";
        private static readonly string CacheDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");

        static QueryCache()
        {
            Load();
        }

        private static string GetCachePath()
        {
            if (!Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir);
            return Path.Combine(CacheDir, CacheFile);
        }

        public bool TryGetSql(string message, Guid tenantId, out string? sql)
        {
            var key = BuildKey(message, tenantId);
            return _sqlCache.TryGetValue(key, out sql);
        }

        public void SetSql(string message, Guid tenantId, string sql)
        {
            var key = BuildKey(message, tenantId);
            _sqlCache[key] = sql;
            Save();
        }

        private string BuildKey(string message, Guid tenantId)
        {
            var normalized = message.ToLower().Trim()
                .Replace("please", "").Replace("can you", "")
                .Replace("  ", " ").Trim();

            // Inclusion of date in key ensures "today" and "recent" queries refresh daily
            var dateKey = DateTime.Now.ToString("yyyy-MM-dd");

            using var sha256 = SHA256.Create();
            var hash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized + dateKey)))[..16];
            return $"sql:{tenantId}:{hash}";
        }

        private static void Save()
        {
            try { File.WriteAllText(GetCachePath(), JsonSerializer.Serialize(_sqlCache)); } catch { }
        }

        private static void Load()
        {
            try
            {
                var path = GetCachePath();
                if (File.Exists(path))
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
                    if (data != null) foreach (var kv in data) _sqlCache[kv.Key] = kv.Value;
                }
            } catch { }
        }
    }
}

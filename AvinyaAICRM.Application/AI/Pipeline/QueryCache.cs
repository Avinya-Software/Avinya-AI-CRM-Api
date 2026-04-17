using Microsoft.Extensions.Caching.Memory;
using System;
using System.Security.Cryptography;
using System.Text;

namespace AvinyaAICRM.Application.AI.Pipeline
{
    public class QueryCache
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);

        public QueryCache(IMemoryCache cache) => _cache = cache;

        public bool TryGetSql(string message, Guid tenantId, out string? sql)
        {
            var key = BuildKey(message, tenantId);
            return _cache.TryGetValue(key, out sql);
        }

        public void SetSql(string message, Guid tenantId, string sql)
        {
            var key = BuildKey(message, tenantId);
            _cache.Set(key, sql, _ttl);
        }

        private string BuildKey(string message, Guid tenantId)
        {
            var normalized = message.ToLower().Trim()
                .Replace("please", "").Replace("can you", "")
                .Replace("  ", " ").Trim();

            using var sha256 = SHA256.Create();
            var hash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized)))[..16];
            return $"sql:{tenantId}:{hash}";
        }
    }
}

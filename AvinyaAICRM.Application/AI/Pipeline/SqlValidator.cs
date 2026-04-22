using AvinyaAICRM.Domain.Constant;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace AvinyaAICRM.Application.AI.Pipeline
{
    public class SqlValidator
    {
        public ValidationResult Validate(string sql, Guid tenantId, bool isSuperAdmin)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(sql))
            {
                result.IsValid = false;
                result.Error = "Query is empty.";
                return result;
            }

            var upper = sql.ToUpper().Trim();

            // Rule 1: Must start with SELECT
            if (!upper.StartsWith("SELECT"))
            {
                result.IsValid = false;
                result.Error = "Only SELECT queries are allowed.";
                return result;
            }

            // Rule 2: Block dangerous keywords (using regex to check for whole words only)
            // This prevents false positives on columns like 'IsDeleted' which contains 'Delete'
            var blocked = new[] { "DELETE", "UPDATE", "DROP", "INSERT", "ALTER", "TRUNCATE", "CREATE", "EXEC", "EXECUTE", "XP_" };
            
            foreach (var word in blocked)
            {
                if (Regex.IsMatch(upper, @"\b" + word + @"\b"))
                {
                    result.IsValid = false;
                    result.Error = $"Forbidden operation detected: {word}";
                    return result;
                }
            }

            // Rule 3: TenantId check
            if (!isSuperAdmin)
            {
                if (!sql.Contains("@TenantId", StringComparison.OrdinalIgnoreCase) && !sql.Contains(tenantId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Error = "Security check failed: Missing tenant filter.";
                    return result;
                }
            }

            // Rule 4: Known Tables only
            var knownTables = AISchema.TableNames.Select(t => t.ToUpper()).ToHashSet();
            var matches = Regex.Matches(sql, @"FROM\s+(?:dbo\.)?(\w+)|\bJOIN\s+(?:dbo\.)?(\w+)", RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var tableName = (match.Groups[1].Value + match.Groups[2].Value).ToUpper().Trim();
                if (!string.IsNullOrEmpty(tableName) && !knownTables.Contains(tableName))
                {
                    result.IsValid = false;
                    result.Error = $"Access denied to unrecognized data source: {tableName}";
                    return result;
                }
            }

            result.IsValid = true;
            return result;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}

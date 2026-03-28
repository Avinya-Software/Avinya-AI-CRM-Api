using AvinyaAICRM.Application.DTOs.AICHATS;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Application.Services.AICHATS
{
    public class CRMQueryService : ICRMQueryService
    {
        private readonly AppDbContext _context;

        public CRMQueryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteRawSqlAsync(string sql, Guid tenantId, bool isSuperAdmin)
        {
            if (string.IsNullOrWhiteSpace(sql)) return new List<Dictionary<string, object>>();

            // Basic safety check (Server side)
            var forbidden = new[] { "UPDATE", "DELETE", "DROP", "INSERT", "ALTER", "TRUNCATE", "CREATE" };
            if (!isSuperAdmin && sql.ToUpper().Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Any(word => forbidden.Contains(word)))
            {
                throw new UnauthorizedAccessException("Only SELECT queries are allowed for safety.");
            }

            // Security Guard: For regular users, the AI must have included the @TenantId parameter
            if (!isSuperAdmin)
            {
                if (!sql.Contains("@TenantId", StringComparison.OrdinalIgnoreCase) && !sql.Contains(tenantId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Security Error: Query must be filtered by TenantId.");
                }
            }

            var results = new List<Dictionary<string, object>>();

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    
                    // Only add parameter if not SuperAdmin OR if the SQL actually used it
                    if (!isSuperAdmin || sql.Contains("@TenantId", StringComparison.OrdinalIgnoreCase))
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@TenantId";
                        parameter.Value = tenantId;
                        command.Parameters.Add(parameter);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                    }
                }
            }
            return results;
        }

        public async Task<SummaryDto> GetSummaryAsync(string dateRange)
        {
            var query = _context.Leads.AsQueryable();

            // Apply filter based on AI
            if (dateRange == "last_7_days")
            {
                var date = DateTime.UtcNow.AddDays(-7);
                query = query.Where(x => x.CreatedDate >= date);
            }
            else if (dateRange == "this_month")
            {
                var now = DateTime.UtcNow;
                query = query.Where(x =>
                    x.CreatedDate.Day >= 1 &&
                    x.CreatedDate.Month == now.Month &&
                    x.CreatedDate.Year == now.Year);
            }

            // Aggregate data
            var result = new SummaryDto
            {
                TotalLeads = await query.CountAsync(),
                Converted = await query.CountAsync(x => x.Status == "Converted"),
            };

            return result;
        }
    }
}

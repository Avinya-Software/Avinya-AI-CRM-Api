using AvinyaAICRM.Application.DTOs.AICHATS;
using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AICHAT;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.AI;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Application.Services.AICHATS
{
    public class CRMQueryService : ICRMQueryService
    {
        private readonly AppDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILeadService _leadService;

        public CRMQueryService(AppDbContext context, IAIService aiService, ILeadService leadService)
        {
            _context = context;
            _aiService = aiService;
            _leadService = leadService;
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
            };

            return result;
        }
        public async Task<AIResponse> ProcessCommandAsync(string message, Guid tenantId, string userId, bool isSuperAdmin)
        {
            // 1. Analyze Intent and Extract Data
            var aiResponse = await _aiService.GetIntentAsync(message);

            if (aiResponse.Action == "create_lead")
            {
                return await HandleCreateLeadAsync(aiResponse, tenantId, userId, isSuperAdmin);
            }

            return aiResponse;
        }

        private async Task<AIResponse> HandleCreateLeadAsync(AIResponse aiResponse, Guid tenantId, string userId, bool isSuperAdmin)
        {
            var parameters = aiResponse.Parameters ?? new Dictionary<string, string>();
            parameters.TryGetValue("CompanyName", out var clientName);

            if (!string.IsNullOrEmpty(clientName))
            {
                // 2. Check Existing Client
                var existingClients = await _context.Clients
                    .Where(c => c.CompanyName.Contains(clientName) && (isSuperAdmin || c.TenantId == tenantId))
                    .Select(c => new ClientDisambiguationDto
                    {
                        ClientID = c.ClientID,
                        CompanyName = c.CompanyName,
                        Email = c.Email,
                        Mobile = c.Mobile
                    })
                    .ToListAsync();

                if (existingClients.Count == 0)
                {
                    // Proceed normally: Lead without ClientID
                    return await CreateLeadAndFinalize(aiResponse, null, tenantId, userId);
                }
                else if (existingClients.Count == 1)
                {
                    // Use unique ClientID
                    return await CreateLeadAndFinalize(aiResponse, existingClients[0].ClientID, tenantId, userId);
                }
                else
                {
                    // Multiple found: Use metadata for disambiguation
                    // Check if user already provided extra info in this message (e.g. mobile/email)
                    parameters.TryGetValue("Email", out var email);
                    parameters.TryGetValue("Mobile", out var mobile);

                    if (!string.IsNullOrEmpty(email) || !string.IsNullOrEmpty(mobile))
                    {
                        var exactMatch = existingClients.FirstOrDefault(c => 
                            (!string.IsNullOrEmpty(email) && c.Email == email) || 
                            (!string.IsNullOrEmpty(mobile) && c.Mobile == mobile));

                        if (exactMatch != null)
                        {
                            return await CreateLeadAndFinalize(aiResponse, exactMatch.ClientID, tenantId, userId);
                        }
                    }

                    // Still ambiguous
                    aiResponse.IsClarificationRequired = true;
                    aiResponse.SuggestedClients = existingClients;
                    aiResponse.ClarificationMessage = $"I found multiple clients named '{clientName}'. Please provide an email or phone number to identify the correct one, or select from the list below:";
                    return aiResponse;
                }
            }

            // If no client name provided, create lead without ClientID
            return await CreateLeadAndFinalize(aiResponse, null, tenantId, userId);
        }

        private async Task<AIResponse> CreateLeadAndFinalize(AIResponse aiResponse, Guid? clientId, Guid tenantId, string userId)
        {
            var p = aiResponse.Parameters ?? new Dictionary<string, string>();
            
            var dto = new LeadRequestDto
            {
                ClientID = clientId,
                CompanyName = p.GetValueOrDefault("CompanyName"),
                Email = p.GetValueOrDefault("Email"),
                Mobile = p.GetValueOrDefault("Mobile"),
                Notes = p.GetValueOrDefault("Notes"),
                ContactPerson = p.GetValueOrDefault("ContactPerson"),
                RequirementDetails = p.GetValueOrDefault("Notes"),
                Date = DateTime.UtcNow,
                CreatedBy = userId
            };

            var result = await _leadService.CreateAsync(dto, userId);

            if (result.StatusCode == 200 || result.StatusCode == 201)
            {
                aiResponse.ClarificationMessage = clientId.HasValue 
                    ? $"Successfully created lead for existing client: {dto.CompanyName}." 
                    : $"Successfully created a new lead for: {dto.CompanyName}.";
            }
            else
            {
                aiResponse.ClarificationMessage = "I encountered an error while creating the lead: " + result.StatusMessage;
            }

            return aiResponse;
        }
        public async Task<List<string>> GetUserAllowedModulesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<string>();

            return await (from up in _context.UserPermissions
                           join p in _context.Permissions on up.PermissionId equals p.PermissionId
                           join mm in _context.Modules on p.ModuleId equals mm.ModuleId
                           join a in _context.Actions on p.ActionId equals a.ActionId
                           where up.UserId == userId && mm.IsActive == true && a.ActionKey.ToLower() == "view"
                           select mm.ModuleKey.ToLower()).Distinct().ToListAsync();
        }
    }
}

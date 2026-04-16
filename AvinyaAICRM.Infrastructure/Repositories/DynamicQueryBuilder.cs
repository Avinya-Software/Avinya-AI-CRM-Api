using AvinyaAICRM.Application.Interfaces.RepositoryInterface.AIChat;
using AvinyaAICRM.Shared.AI;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AvinyaAICRM.Infrastructure.Repositories
{
    public class DynamicQueryBuilder : IDynamicQueryBuilder
    {
        private readonly Dictionary<string, EntityPolicy> _policies;
        private readonly List<Relationship> _relationships;
        private readonly List<FilterMap> _filterMaps;

        public DynamicQueryBuilder()
        {
            _policies = InitializePolicies();
            _relationships = InitializeRelationships();
            _filterMaps = InitializeFilterMaps();
        }

        public QueryRequest NormalizeRequest(AIResponse aiResponse)
        {
            var request = new QueryRequest
            {
                Entities = aiResponse.Entities ?? new List<string>(),
                Type = aiResponse.Type?.ToUpper() switch
                {
                    "SUMMARY" => QueryType.SUMMARY,
                    "DETAIL" => QueryType.DETAIL,
                    _ => QueryType.LIST
                },
                RawFilters = aiResponse.Filters ?? new Dictionary<string, object>()
            };

            // Normalize Filters
            request.NormalizedFilters.IsMyData = GetStringValue(request.RawFilters, "assignedTo") == "me";
            request.NormalizedFilters.Search = GetStringValue(request.RawFilters, "search");
            
            var scope = GetStringValue(request.RawFilters, "taskScope");
            if (!string.IsNullOrEmpty(scope))
                request.NormalizedFilters.TaskFilterType = scope.ToUpper();
            else if (request.NormalizedFilters.IsMyData)
                request.NormalizedFilters.TaskFilterType = "MY";

            var dateRange = GetStringValue(request.RawFilters, "dateRange");
            if (!string.IsNullOrEmpty(dateRange))
            {
                var now = DateTime.UtcNow;
                dateRange = dateRange.ToLower();
                if (dateRange.Contains("7") || dateRange.Contains("week"))
                    request.NormalizedFilters.StartDate = now.AddDays(-7);
                else if (dateRange.Contains("30") || dateRange.Contains("month"))
                    request.NormalizedFilters.StartDate = now.AddDays(-30);
                else if (dateRange.Contains("today"))
                    request.NormalizedFilters.StartDate = now.Date;
                else if (dateRange.Contains("yesterday"))
                {
                    request.NormalizedFilters.StartDate = now.Date.AddDays(-1);
                    request.NormalizedFilters.EndDate = now.Date.AddSeconds(-1);
                }
            }

            return request;
        }

        private string? GetStringValue(Dictionary<string, object> dict, string key)
        {
            if (!dict.TryGetValue(key, out var val) || val == null) return null;
            
            if (val is string s) return s;
            
            if (val is System.Text.Json.JsonElement elem)
            {
                if (elem.ValueKind == System.Text.Json.JsonValueKind.String) return elem.GetString();
                return elem.GetRawText(); // Handle objects as raw text for relative processing
            }
            
            return val.ToString();
        }

        public (string sql, Dictionary<string, object> parameters) BuildQuery(QueryRequest request, Guid tenantId, string userId, bool isAdmin, List<string> allowedModules)
        {
            if (request.Entities == null || !request.Entities.Any())
                throw new ArgumentException("No entities specified.");

            // Performance Guard: Complexity Score
            int complexity = CalculateComplexity(request);
            if (complexity > 25)
                throw new Exception("Query too complex. Please simplify filters or reduce entities.");

            var sql = new StringBuilder();
            var queryParams = new Dictionary<string, object>();
            queryParams["TenantId"] = tenantId;
            queryParams["UserId"] = userId;

            // Primary entity
            var primaryTable = request.Entities[0];
            if (!_policies.TryGetValue(primaryTable, out var policy))
                throw new KeyNotFoundException($"Policy not found for entity: {primaryTable}");

            if (!isAdmin && !allowedModules.Contains(policy.ModuleKey))
                throw new UnauthorizedAccessException($"No permission to access {policy.ModuleKey}.");

            if (request.Type == QueryType.SUMMARY)
                BuildSummaryQuery(sql, policy, request, queryParams, userId, isAdmin);
            else
                BuildListQuery(sql, policy, request, queryParams, userId, isAdmin);

            return (sql.ToString(), queryParams);
        }

        private int CalculateComplexity(QueryRequest request)
        {
            // Summaries are optimized via JSON aggregation, so they carry less complexity cost per entity
            int entityMultiplier = request.Type == QueryType.SUMMARY ? 1 : 2;
            int score = (request.Entities.Count - 1) * entityMultiplier;
            score += request.RawFilters.Count;
            return score;
        }

        private void BuildListQuery(StringBuilder sql, EntityPolicy policy, QueryRequest request, Dictionary<string, object> queryParams, string userId, bool isAdmin)
        {
            var alias = policy.Alias;
            var columns = string.Join(", ", policy.SelectColumns.Select(c => $"{alias}.[{c}]"));
            
            sql.Append($"SELECT {columns} FROM [{policy.TableName}] {alias} ");

            // BFS Pathfinding Joins
            var joinedPolicies = new List<EntityPolicy> { policy };
            var joinedTableNames = new HashSet<string> { policy.TableName };

            foreach (var targetEntity in request.Entities.Skip(1))
            {
                if (_policies.TryGetValue(targetEntity, out var targetPolicy))
                {
                    var path = FindShortestPath(policy.TableName, targetPolicy.TableName);
                    if (path != null)
                    {
                        foreach (var rel in path)
                        {
                            var fromPolicy = _policies.Values.First(p => p.TableName == rel.FromTable);
                            var toPolicy = _policies.Values.First(p => p.TableName == rel.ToTable);

                            if (!joinedTableNames.Contains(toPolicy.TableName))
                            {
                                sql.Append($"LEFT JOIN [{toPolicy.TableName}] {toPolicy.Alias} ON {fromPolicy.Alias}.[{rel.FromColumn}] = {toPolicy.Alias}.[{rel.ToColumn}] ");
                                joinedTableNames.Add(toPolicy.TableName);
                                joinedPolicies.Add(toPolicy);
                            }
                        }
                    }
                }
            }

            sql.Append(" WHERE 1=1 ");

            // 1. Apply Universal Security to ALL joined tables
            foreach (var p in joinedPolicies)
            {
                ApplyUniversalSecurity(sql, p);
            }

            // 2. Apply Custom Filters and RBAC to Primary table
            ApplySecurityAndFilters(sql, policy, request, queryParams, userId, isAdmin);

            sql.Append($" ORDER BY {alias}.[{policy.DefaultDateColumn}] DESC ");
            sql.Append(" OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY ");
        }

        private void BuildSummaryQuery(StringBuilder sql, EntityPolicy policy, QueryRequest request, Dictionary<string, object> queryParams, string userId, bool isAdmin)
        {
            // Ensure date parameters are populated for subqueries
            if (request.NormalizedFilters.StartDate.HasValue) queryParams["StartDate"] = request.NormalizedFilters.StartDate.Value;
            if (request.NormalizedFilters.EndDate.HasValue) queryParams["EndDate"] = request.NormalizedFilters.EndDate.Value;

            // The "Super Dashboard" Logic
            // If the user specified entities, we summarize those. 
            // If they just said "Summary", we provide a default dashboard scope.
            var entitiesToSummarize = request.Entities?.Any() == true 
                ? request.Entities 
                : new List<string> { "Leads", "Clients", "Quotations", "Invoices", "Projects", "TaskOccurrences" };

            sql.Append("SELECT ");
            
            bool first = true;
            foreach (var entity in entitiesToSummarize)
            {
                if (!_policies.TryGetValue(entity, out var p)) continue;
                
                if (!first) sql.Append(", ");
                
                // 1. Module Count
                sql.Append($"{GetStatSubquery(p, request, "COUNT", isAdmin)} as [{entity}Count]");
                
                // 2. Module Data (Recent Records as JSON)
                sql.Append($", {GetStatSubquery(p, request, "DATA", isAdmin)} as [{entity}Data]");

                // 3. Status Breakdown (JSON)
                sql.Append($", {GetStatSubquery(p, request, "STATUS", isAdmin)} as [{entity}StatusBreakdown]");

                first = false;
            }

            if (first) // Fallback if no valid entities found
            {
                sql.Append(" 'No valid entities for summary' as Info ");
            }
        }

        private string GetStatSubquery(EntityPolicy policy, QueryRequest request, string type, bool isAdmin)
        {
            var tableName = policy.TableName;
            var alias = "sub_" + policy.Alias;
            var dateCol = policy.DefaultDateColumn;

            // Base Filters (Tenant + Deleted)
            var filters = new List<string>();
            if (!string.IsNullOrEmpty(policy.TenantColumn))
                filters.Add($"[{policy.TenantColumn}] = @TenantId");
            if (policy.HasIsDeleted)
                filters.Add("[IsDeleted] = 0");

            // RBAC Filtering: If not admin, or if specifically "my" data
            if (!isAdmin || request.NormalizedFilters.IsMyData)
            {
                if (!string.IsNullOrEmpty(policy.UserColumn))
                    filters.Add($"[{policy.UserColumn}] = @UserId");
            }

            // Date Range
            if (request.NormalizedFilters.StartDate.HasValue)
                filters.Add($"[{dateCol}] >= @StartDate");
            if (request.NormalizedFilters.EndDate.HasValue)
                filters.Add($"[{dateCol}] <= @EndDate");

            var whereClause = filters.Any() ? "WHERE " + string.Join(" AND ", filters) : "";

            if (type == "COUNT")
            {
                return $"(SELECT COUNT(*) FROM [{tableName}] {whereClause})";
            }
            
            if (type == "DATA")
            {
                var selectCols = string.Join(", ", policy.SelectColumns.Take(4));
                return $"(SELECT TOP 5 {selectCols} FROM [{tableName}] {whereClause} ORDER BY [{dateCol}] DESC FOR JSON PATH)";
            }

            if (type == "STATUS")
            {
                // Intelligent Status Column Detection
                var statusCol = policy.SelectColumns.FirstOrDefault(c => c.Contains("Status")) 
                               ?? (tableName == "Leads" ? "LeadStatusID" : (tableName == "Quotations" ? "QuotationStatusID" : null));

                if (statusCol == null) return " '[]' ";

                return $"(SELECT [{statusCol}] as Status, COUNT(*) as Count FROM [{tableName}] {whereClause} GROUP BY [{statusCol}] FOR JSON PATH)";
            }

            return "NULL";
        }

        private void ApplyUniversalSecurity(StringBuilder sql, EntityPolicy policy)
        {
            var alias = policy.Alias;
            if (!string.IsNullOrEmpty(policy.TenantColumn))
                sql.Append($" AND {alias}.[{policy.TenantColumn}] = @TenantId ");
            if (policy.HasIsDeleted)
                sql.Append($" AND {alias}.[IsDeleted] = 0 ");
        }

        private void ApplySecurityAndFilters(StringBuilder sql, EntityPolicy policy, QueryRequest request, Dictionary<string, object> queryParams, string userId, bool isAdmin)
        {
            var alias = policy.Alias;

            // RBAC Logic (Primary Only)
            ApplyRBAC(sql, policy, request, isAdmin);

            // Smart Date Resolver
            ApplyDateFilters(sql, policy, request, queryParams);

            // Filter Mapping (Ambiguous filters)
            foreach (var filter in request.RawFilters)
            {
                var map = _filterMaps.FirstOrDefault(m => m.FilterKey.Equals(filter.Key, StringComparison.OrdinalIgnoreCase));
                if (map != null && map.Table == policy.TableName)
                {
                    var val = GetStringValue(request.RawFilters, filter.Key);
                    if (!string.IsNullOrEmpty(val))
                    {
                        sql.Append($" AND {alias}.[{map.Column}] = @{filter.Key} ");
                        queryParams[filter.Key] = val;
                    }
                }
            }

            // Search
            if (!string.IsNullOrEmpty(request.NormalizedFilters.Search))
            {
                var searchCol = policy.SelectColumns.FirstOrDefault(c => c.Contains("Name") || c.Contains("No")) ?? policy.SelectColumns[0];
                sql.Append($" AND {alias}.[{searchCol}] LIKE @Search ");
                queryParams["Search"] = $"%{request.NormalizedFilters.Search}%";
            }
        }

        private void ApplyRBAC(StringBuilder sql, EntityPolicy policy, QueryRequest request, bool isAdmin)
        {
            var alias = policy.Alias;
            
            // Task specific logic (Architect Refinement)
            if (policy.TableName == "TaskOccurrences")
            {
                // Admins see all tasks unless they say "my"
                if (isAdmin && !request.NormalizedFilters.IsMyData) 
                    return;

                if (request.NormalizedFilters.TaskFilterType == "MY")
                    sql.Append($" AND {alias}.[AssignedTo] = @UserId ");
                else if (request.NormalizedFilters.TaskFilterType == "CREATED")
                    sql.Append($" AND t_series.[CreatedBy] = @UserId "); // Joins to series
                return;
            }

            // General RBAC: Restricted if NOT Admin OR if Admin explicitly asked for "my" data
            if (!isAdmin || request.NormalizedFilters.IsMyData)
            {
                if (!string.IsNullOrEmpty(policy.UserColumn))
                    sql.Append($" AND {alias}.[{policy.UserColumn}] = @UserId ");
            }
        }

        private void ApplyDateFilters(StringBuilder sql, EntityPolicy policy, QueryRequest request, Dictionary<string, object> queryParams)
        {
            var alias = policy.Alias;
            var dateCol = policy.DefaultDateColumn;

            // Check if user specified a specific date column via filter aliases
            foreach (var aliasEntry in policy.DateColumnAliases)
            {
                if (request.RawFilters.ContainsKey(aliasEntry.Key))
                {
                    dateCol = aliasEntry.Value;
                    break;
                }
            }

            if (request.NormalizedFilters.StartDate.HasValue)
            {
                sql.Append($" AND {alias}.[{dateCol}] >= @StartDate ");
                queryParams["StartDate"] = request.NormalizedFilters.StartDate.Value;
            }
            if (request.NormalizedFilters.EndDate.HasValue)
            {
                sql.Append($" AND {alias}.[{dateCol}] <= @EndDate ");
                queryParams["EndDate"] = request.NormalizedFilters.EndDate.Value;
            }
        }

        private List<Relationship> FindShortestPath(string start, string end)
        {
            // Simple Breadth-First Search (BFS) to find join path
            var queue = new Queue<(string table, List<Relationship> path)>();
            queue.Enqueue((start, new List<Relationship>()));
            var visited = new HashSet<string> { start };

            while (queue.Count > 0)
            {
                var (current, path) = queue.Dequeue();
                if (current == end) return path;

                var neighbors = _relationships.Where(r => r.FromTable == current || r.ToTable == current);
                foreach (var rel in neighbors)
                {
                    var nextTable = rel.FromTable == current ? rel.ToTable : rel.FromTable;
                    if (!visited.Contains(nextTable))
                    {
                        visited.Add(nextTable);
                        var nextPath = new List<Relationship>(path) { rel };
                        queue.Enqueue((nextTable, nextPath));
                    }
                }
            }
            return null;
        }

        private Dictionary<string, EntityPolicy> InitializePolicies()
        {
            return new Dictionary<string, EntityPolicy>(StringComparer.OrdinalIgnoreCase)
            {
                ["Leads"] = new EntityPolicy { 
                    TableName = "Leads", Alias = "l", UserColumn = "AssignedTo", ModuleKey = "lead",
                    SelectColumns = new List<string> { "LeadID", "LeadNo", "Date", "RequirementDetails", "CreatedDate" },
                    DateColumnAliases = new Dictionary<string, string> { ["registrationDate"] = "Date" },
                    HasIsDeleted = true, TenantColumn = "TenantId"
                },
                ["LeadFollowups"] = new EntityPolicy { 
                    TableName = "LeadFollowups", Alias = "lf", UserColumn = "FollowUpBy", ModuleKey = "lead",
                    SelectColumns = new List<string> { "FollowUpID", "Notes", "NextFollowupDate", "CreatedDate" },
                    HasIsDeleted = false, TenantColumn = null
                },
                ["Clients"] = new EntityPolicy { 
                    TableName = "Clients", Alias = "c", UserColumn = "CreatedBy", ModuleKey = "client",
                    SelectColumns = new List<string> { "ClientID", "CompanyName", "ContactPerson", "Email", "Mobile" },
                    HasIsDeleted = true, TenantColumn = "TenantId"
                },
                ["Quotations"] = new EntityPolicy { 
                    TableName = "Quotations", Alias = "q", UserColumn = "CreatedBy", ModuleKey = "quotation", DefaultDateColumn = "QuotationDate",
                    SelectColumns = new List<string> { "QuotationID", "QuotationNo", "QuotationDate", "TotalAmount", "GrandTotal" },
                    HasIsDeleted = true, TenantColumn = "TenantId"
                },
                ["Orders"] = new EntityPolicy { 
                    TableName = "Orders", Alias = "o", UserColumn = "CreatedBy", ModuleKey = "order", DefaultDateColumn = "OrderDate",
                    SelectColumns = new List<string> { "OrderID", "OrderNo", "OrderDate", "GrandTotal", "Status" },
                    HasIsDeleted = true, TenantColumn = "TenantId"
                },
                ["Expenses"] = new EntityPolicy { 
                    TableName = "Expenses", Alias = "e", UserColumn = "CreatedBy", ModuleKey = "expense", DefaultDateColumn = "ExpenseDate",
                    SelectColumns = new List<string> { "ExpenseId", "ExpenseDate", "Amount", "Description" },
                    HasIsDeleted = true, TenantColumn = "TenantId"
                },
                ["Invoices"] = new EntityPolicy { 
                    TableName = "Invoices", Alias = "inv", UserColumn = null, ModuleKey = "invoice", DefaultDateColumn = "InvoiceDate",
                    SelectColumns = new List<string> { "InvoiceID", "InvoiceNo", "InvoiceDate", "GrandTotal", "OutstandingAmount" },
                    HasIsDeleted = true, TenantColumn = "TenantId"
                },
                ["Payments"] = new EntityPolicy { 
                    TableName = "Payments", Alias = "pay", UserColumn = "ReceivedBy", ModuleKey = "invoice", DefaultDateColumn = "PaymentDate",
                    SelectColumns = new List<string> { "PaymentID", "Amount", "PaymentMode", "PaymentDate" },
                    HasIsDeleted = false, TenantColumn = null
                },
                ["Projects"] = new EntityPolicy { 
                    TableName = "Projects", Alias = "prj", UserColumn = "CreatedBy", ModuleKey = "project",
                    SelectColumns = new List<string> { "ProjectID", "ProjectName", "Status", "StartDate", "EndDate" },
                    HasIsDeleted = true, TenantColumn = "TenantId"
                },
                ["TaskOccurrences"] = new EntityPolicy { 
                    TableName = "TaskOccurrences", Alias = "t_occ", UserColumn = "AssignedTo", ModuleKey = "task", DefaultDateColumn = "CreatedAt",
                    SelectColumns = new List<string> { "Id", "Status", "DueDateTime", "CreatedAt" },
                    HasIsDeleted = false, TenantColumn = null
                },
                ["TaskSeries"] = new EntityPolicy { 
                    TableName = "TaskSeries", Alias = "t_series", UserColumn = "CreatedBy", ModuleKey = "task",
                    SelectColumns = new List<string> { "Id", "Title", "Description", "TaskScope" },
                    HasIsDeleted = false, TenantColumn = null // Joins to TaskLists or Projects for tenant scope if needed
                },
                ["Products"] = new EntityPolicy { 
                    TableName = "Products", Alias = "prod", UserColumn = "CreatedBy", ModuleKey = "product",
                    SelectColumns = new List<string> { "ProductID", "ProductName", "Category", "DefaultRate" },
                    HasIsDeleted = true, TenantColumn = "TenantId"
                }
            };
        }

        private List<Relationship> InitializeRelationships()
        {
            return new List<Relationship>
            {
                new Relationship { FromTable = "Leads", ToTable = "Clients", FromColumn = "ClientID", ToColumn = "ClientID", Weight = 1 },
                new Relationship { FromTable = "LeadFollowups", ToTable = "Leads", FromColumn = "LeadID", ToColumn = "LeadID", Weight = 1 },
                new Relationship { FromTable = "Quotations", ToTable = "Leads", FromColumn = "LeadID", ToColumn = "LeadID", Weight = 1 },
                new Relationship { FromTable = "Orders", ToTable = "Quotations", FromColumn = "QuotationID", ToColumn = "QuotationID", Weight = 1 },
                new Relationship { FromTable = "Invoices", ToTable = "Orders", FromColumn = "OrderID", ToColumn = "OrderID", Weight = 1 },
                new Relationship { FromTable = "Payments", ToTable = "Invoices", FromColumn = "InvoiceID", ToColumn = "InvoiceID", Weight = 1 },
                new Relationship { FromTable = "TaskOccurrences", ToTable = "TaskSeries", FromColumn = "TaskSeriesId", ToColumn = "Id", Weight = 1 },
                new Relationship { FromTable = "Projects", ToTable = "Clients", FromColumn = "ClientID", ToColumn = "ClientID", Weight = 1 },
                new Relationship { FromTable = "TaskSeries", ToTable = "Projects", FromColumn = "ProjectId", ToColumn = "ProjectID", Weight = 1 },
                new Relationship { FromTable = "Orders", ToTable = "Clients", FromColumn = "ClientID", ToColumn = "ClientID", Weight = 2 } // Alternative path
            };
        }

        private List<FilterMap> InitializeFilterMaps()
        {
            return new List<FilterMap>
            {
                new FilterMap { FilterKey = "status", Table = "Leads", Column = "LeadStatusID" },
                new FilterMap { FilterKey = "status", Table = "TaskOccurrences", Column = "Status" },
                new FilterMap { FilterKey = "status", Table = "Orders", Column = "Status" },
                new FilterMap { FilterKey = "status", Table = "Projects", Column = "Status" }
            };
        }
    }
}

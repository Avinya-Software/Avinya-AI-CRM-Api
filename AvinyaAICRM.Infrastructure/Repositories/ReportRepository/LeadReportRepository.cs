using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Application.Interfaces;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.ReportRepository
{
    public class LeadReportRepository : ILeadReportRepository
    {
        private readonly AppDbContext _context;

        public LeadReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LeadPipelineReportDto> GetLeadPipelineReportAsync(LeadPipelineFilterDto filter)
        {
            // ── Base query ────────────────────────────────────────────────────────
            var leadsQuery = _context.Leads
                .Where(l => !l.IsDeleted && l.TenantId == filter.TenantId);

            if (filter.DateFrom.HasValue)
                leadsQuery = leadsQuery.Where(l => l.CreatedDate >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                leadsQuery = leadsQuery.Where(l => l.CreatedDate <= filter.DateTo.Value);

            if (filter.LeadSourceId.HasValue)
                leadsQuery = leadsQuery.Where(l => l.LeadSourceID == filter.LeadSourceId.Value);

            if (filter.LeadStatusId.HasValue)
                leadsQuery = leadsQuery.Where(l => l.LeadStatusID == filter.LeadStatusId.Value);

            if (!string.IsNullOrEmpty(filter.AssignedTo))
                leadsQuery = leadsQuery.Where(l => l.AssignedTo == filter.AssignedTo);

            // ── KPIs ──────────────────────────────────────────────────────────────
            // Load status names in one round-trip
            var statuses = await _context.leadStatusMasters
                .ToDictionaryAsync(s => s.LeadStatusID, s => s.StatusName);

            // Leads with their status name joined in memory (avoids string comparison in SQL)
            var leadsWithStatus = await leadsQuery
                .Select(l => new
                {
                    l.LeadID,
                    l.LeadStatusID,
                    l.LeadSourceID,
                    l.AssignedTo
                })
                .ToListAsync();

            int total = leadsWithStatus.Count;
            int converted = leadsWithStatus.Count(l =>
                l.LeadStatusID.HasValue &&
                statuses.TryGetValue(l.LeadStatusID.Value, out var sn) &&
                sn == "Converted");

            int lost = leadsWithStatus.Count(l =>
                l.LeadStatusID.HasValue &&
                statuses.TryGetValue(l.LeadStatusID.Value, out var sn) &&
                sn == "Lost");

            // Average follow-ups per lead
            var leadIds = leadsWithStatus.Select(l => l.LeadID).ToList();
            var followUpCounts = await _context.LeadFollowups
                .Where(f => leadIds.Contains(f.LeadID))
                .GroupBy(f => f.LeadID)
                .Select(g => g.Count())
                .ToListAsync();

            double avgFollowUps = followUpCounts.Any()
                ? Math.Round(followUpCounts.Average(), 1)
                : 0;

            var kpi = new LeadPipelineKpiDto
            {
                TotalLeads = total,
                ConvertedLeads = converted,
                LostLeads = lost,
                OpenLeads = total - converted - lost,
                ConversionRate = total > 0 ? Math.Round((double)converted / total * 100, 1) : 0,
                LossRate = total > 0 ? Math.Round((double)lost / total * 100, 1) : 0,
                AvgFollowUps = avgFollowUps
            };

            // ── Funnel by status ──────────────────────────────────────────────────
            var funnelGroups = leadsWithStatus
                .GroupBy(l => l.LeadStatusID)
                .Select(g => new
                {
                    StatusId = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // Join with status master and sort by SortOrder
            var statusMasterOrdered = await _context.leadStatusMasters
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            var funnel = statusMasterOrdered.Select(sm =>
            {
                var count = funnelGroups
                    .FirstOrDefault(f => f.StatusId == sm.LeadStatusID)?.Count ?? 0;
                return new LeadFunnelItemDto
                {
                    StatusName = sm.StatusName,
                    Count = count,
                    Percentage = total > 0 ? Math.Round((double)count / total * 100, 1) : 0
                };
            }).ToList();

            // ── Source breakdown ──────────────────────────────────────────────────
            var sourceMaster = await _context.leadSourceMasters
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ToDictionaryAsync(s => s.LeadSourceID, s => s.SourceName);

            var sourceGroups = leadsWithStatus
                .GroupBy(l => l.LeadSourceID)
                .Select(g => new { SourceId = g.Key, Count = g.Count() })
                .ToList();

            var sourceBreakdown = sourceMaster.Select(sm =>
            {
                var count = sourceGroups
                    .FirstOrDefault(g => g.SourceId == sm.Key)?.Count ?? 0;
                return new LeadSourceBreakdownDto
                {
                    SourceName = sm.Value,
                    Count = count,
                    Percentage = total > 0 ? Math.Round((double)count / total * 100, 1) : 0
                };
            })
            .OrderByDescending(s => s.Count)
            .ToList();

            // ── Source conversion rate ────────────────────────────────────────────
            var sourceConversion = sourceMaster.Select(sm =>
            {
                var sourceLeads = leadsWithStatus.Where(l => l.LeadSourceID == sm.Key).ToList();
                int srcTotal = sourceLeads.Count;
                int srcConverted = sourceLeads.Count(l =>
                    l.LeadStatusID.HasValue &&
                    statuses.TryGetValue(l.LeadStatusID.Value, out var sn) &&
                    sn == "Converted");

                return new LeadSourceConversionDto
                {
                    SourceName = sm.Value,
                    TotalLeads = srcTotal,
                    ConvertedLeads = srcConverted,
                    ConversionRate = srcTotal > 0 ? Math.Round((double)srcConverted / srcTotal * 100, 1) : 0
                };
            })
            .OrderByDescending(s => s.ConversionRate)
            .ToList();

            // ── Overdue follow-ups ────────────────────────────────────────────────
            var today = DateTime.UtcNow;

            var overdueRaw = await _context.LeadFollowups
                .Where(f =>
                    leadIds.Contains(f.LeadID) &&
                    f.NextFollowupDate.HasValue &&
                    f.NextFollowupDate.Value < today)
                .OrderBy(f => f.NextFollowupDate)
                .Select(f => new
                {
                    f.LeadID,
                    f.UpdatedDate,
                    f.NextFollowupDate,
                    f.FollowUpBy,
                    f.Status,
                    f.CreatedDate
                })
                .ToListAsync();

            // Join with Leads and Clients for display names
            var clientMap = await _context.Clients
                .Where(c => !c.IsDeleted)
                .ToDictionaryAsync(c => c.ClientID, c => c.CompanyName);

            var leadMap = await _context.Leads
                .Where(l => leadIds.Contains(l.LeadID))
                .ToDictionaryAsync(l => l.LeadID, l => new { l.LeadNo, l.ClientID, l.AssignedTo });

            var userMap = await _context.Users
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            var followUpStatusMap = await _context.LeadFollowupStatuses
                .ToDictionaryAsync(s => s.LeadFollowupStatusID, s => s.StatusName);

            var overdueFollowUps = overdueRaw
                .Where(f => leadMap.ContainsKey(f.LeadID))
                .Select(f =>
                {
                    var lead = leadMap[f.LeadID];
                    var clientName = lead.ClientID.HasValue && clientMap.ContainsKey(lead.ClientID.Value)
                        ? clientMap[lead.ClientID.Value]
                        : "—";
                    var assignedTo = !string.IsNullOrEmpty(f.FollowUpBy) && userMap.ContainsKey(f.FollowUpBy)
                        ? userMap[f.FollowUpBy]
                        : f.FollowUpBy ?? "—";
                    var statusName = followUpStatusMap.ContainsKey(f.Status)
                                     ? followUpStatusMap[f.Status]
                                     : "—";

                    return new OverdueFollowUpDto
                    {
                        LeadNo = lead.LeadNo ?? "—",
                        ClientName = clientName,
                        AssignedTo = assignedTo,
                        LastFollowUp = f.UpdatedDate ?? f.CreatedDate ?? today,
                        NextDue = f.NextFollowupDate!.Value,
                        DaysOverdue = (int)(today - f.NextFollowupDate!.Value).TotalDays,
                        FollowUpStatus = statusName
                    };
                })
                .OrderByDescending(f => f.DaysOverdue)
                .ToList();

            return new LeadPipelineReportDto
            {
                Kpi = kpi,
                Funnel = funnel,
                SourceBreakdown = sourceBreakdown,
                SourceConversion = sourceConversion,
                OverdueFollowUps = overdueFollowUps,
                AppliedFilters = filter
            };
        }

        public async Task<PagedResult<LeadLifecycleReportDto>> GetLeadLifecycleReportAsync(LeadPipelineFilterDto filter)
        {
            var leadsQuery = _context.Leads
                .Where(l => !l.IsDeleted && l.TenantId == filter.TenantId);

            if (filter.DateFrom.HasValue)
                leadsQuery = leadsQuery.Where(l => l.CreatedDate >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                leadsQuery = leadsQuery.Where(l => l.CreatedDate <= filter.DateTo.Value);

            if (filter.LeadSourceId.HasValue)
                leadsQuery = leadsQuery.Where(l => l.LeadSourceID == filter.LeadSourceId.Value);

            if (filter.LeadStatusId.HasValue)
                leadsQuery = leadsQuery.Where(l => l.LeadStatusID == filter.LeadStatusId.Value);

            if (!string.IsNullOrEmpty(filter.AssignedTo))
                leadsQuery = leadsQuery.Where(l => l.AssignedTo == filter.AssignedTo);

            var totalRecords = await leadsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalRecords / filter.PageSize);

            var leads = await leadsQuery
                .OrderByDescending(l => l.CreatedDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var leadIds = leads.Select(l => l.LeadID).ToList();

            // Fetch related data in bulk to avoid N+1
            var quotations = await _context.Quotations
                .Where(q => q.LeadID.HasValue && leadIds.Contains(q.LeadID.Value) && !q.IsDeleted)
                .ToListAsync();
            
            var quotationIds = quotations.Select(q => q.QuotationID).ToList();

            var orders = await _context.Orders
                .Where(o => o.QuotationID.HasValue && quotationIds.Contains(o.QuotationID.Value) && !o.IsDeleted)
                .ToListAsync();

            var followups = await _context.LeadFollowups
                .Where(f => leadIds.Contains(f.LeadID))
                .ToListAsync();

            // Map masters
            var statuses = await _context.leadStatusMasters.ToDictionaryAsync(s => s.LeadStatusID, s => s.StatusName);
            var sources = await _context.leadSourceMasters.ToDictionaryAsync(s => s.LeadSourceID, s => s.SourceName);
            var clients = await _context.Clients.ToDictionaryAsync(c => c.ClientID, c => c.CompanyName);
            var users = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
            var quotationStatuses = await _context.QuotationStatusMaster.ToDictionaryAsync(s => s.QuotationStatusID, s => s.StatusName);
            var orderStatuses = await _context.OrderStatusMasters.ToDictionaryAsync(s => s.StatusID, s => s.StatusName);
            var followupStatuses = await _context.LeadFollowupStatuses.ToDictionaryAsync(s => s.LeadFollowupStatusID, s => s.StatusName);

            var reportData = leads.Select(l => new LeadLifecycleReportDto
            {
                LeadID = l.LeadID,
                LeadNo = l.LeadNo,
                CreatedDate = l.CreatedDate,
                RequirementDetails = l.RequirementDetails,
                ClientName = l.ClientID.HasValue && clients.TryGetValue(l.ClientID.Value, out var cn) ? cn : "—",
                StatusName = l.LeadStatusID.HasValue && statuses.TryGetValue(l.LeadStatusID.Value, out var sn) ? sn : "—",
                SourceName = l.LeadSourceID.HasValue && sources.TryGetValue(l.LeadSourceID.Value, out var src) ? src : "—",
                AssignedToName = !string.IsNullOrEmpty(l.AssignedTo) && users.TryGetValue(l.AssignedTo, out var un) ? un : l.AssignedTo ?? "—",
                
                Quotations = quotations.Where(q => q.LeadID == l.LeadID).Select(q => new LeadQuotationDto
                {
                    QuotationID = q.QuotationID,
                    QuotationNo = q.QuotationNo,
                    QuotationDate = q.QuotationDate,
                    GrandTotal = q.GrandTotal,
                    StatusName = quotationStatuses.TryGetValue(q.QuotationStatusID, out var qsn) ? qsn : "—"
                }).ToList(),

                Orders = orders.Where(o => quotations.Any(q => q.QuotationID == o.QuotationID && q.LeadID == l.LeadID)).Select(o => new LeadOrderDto
                {
                    OrderID = o.OrderID,
                    OrderNo = o.OrderNo,
                    OrderDate = o.OrderDate,
                    GrandTotal = o.GrandTotal,
                    StatusName = orderStatuses.TryGetValue(o.Status, out var osn) ? osn : "—"
                }).ToList(),

                Followups = followups.Where(f => f.LeadID == l.LeadID).Select(f => new LeadFollowupDto
                {
                    FollowUpID = f.FollowUpID,
                    Notes = f.Notes,
                    NextFollowupDate = f.NextFollowupDate,
                    StatusName = followupStatuses.TryGetValue(f.Status, out var fsn) ? fsn : "—",
                    FollowUpByName = !string.IsNullOrEmpty(f.FollowUpBy) && users.TryGetValue(f.FollowUpBy, out var fun) ? fun : f.FollowUpBy ?? "—",
                    CreatedDate = f.CreatedDate
                }).ToList()
            }).ToList();

            return new PagedResult<LeadLifecycleReportDto>
            {
                Data = reportData,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages
            };
        }
    }
}

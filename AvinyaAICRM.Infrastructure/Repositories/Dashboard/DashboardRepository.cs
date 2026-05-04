using AvinyaAICRM.Application.DTOs.Dashboard;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Dashboard;
using AvinyaAICRM.Domain.Entities.Client;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AvinyaAICRM.Infrastructure.Repositories.Dashboard
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        // Preserve interface method signature by delegating to the richer overload
       

        public async Task<DashboardDto> GetDashboardAsync(
         string tenantId,
         string? role,
         string? userId,
         DateTime? fromDate,
         DateTime? toDate)
        {
            bool isManagerOrAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
            
            Guid.TryParse(tenantId, out Guid tenantGuid);
            var today = DateTime.Today;
            var now = DateTime.Now;

            // Normalize date range
            DateTime start = fromDate?.Date ?? DateTime.MinValue;
            DateTime end = toDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;

            // =========================
            // 📊 BASIC COUNTS
            // =========================
            var counts = new DashboardCounts
            {
                Clients = await _context.Clients.CountAsync(x =>
                    !x.IsDeleted && x.IsCustomer &&
                    (x.TenantId == tenantGuid && (isManagerOrAdmin || x.CreatedBy == userId))),

                Leads = await _context.Leads.CountAsync(x =>
                    !x.IsDeleted &&
                    (x.TenantId == tenantGuid && (isManagerOrAdmin || x.CreatedBy == userId))),

                Quotations = await _context.Quotations.CountAsync(x =>
                    !x.IsDeleted &&
                    (x.TenantId == tenantGuid && (isManagerOrAdmin || x.CreatedBy == userId))),

                Orders = await _context.Orders.CountAsync(x =>
                    !x.IsDeleted &&
                    (x.TenantId == tenantGuid && (isManagerOrAdmin || x.CreatedBy == userId))),

                Products = await _context.Products.CountAsync(x =>
                    !x.IsDeleted &&
                    x.TenantId == tenantGuid),

                Expenses = await _context.Expenses.CountAsync(x =>
                    !x.IsDeleted &&
                  x.TenantId == tenantGuid && (isManagerOrAdmin || x.CreatedBy.ToString() == userId)),

                // Fixed Tasks count
                Tasks = await _context.TaskOccurrences.CountAsync(t =>
                    (isManagerOrAdmin
                        ? _context.Users.Any(u => u.Id == t.TaskSeries.CreatedBy && u.TenantId == tenantGuid)
                        : t.TaskSeries.CreatedBy == userId))
            };

            // =========================
            // 👥 CLIENT SUMMARY
            // =========================
            var clientSummary = new ClientSummaryDto
            {
                TotalClients = counts.Clients,

                ActiveClients = await _context.Clients.CountAsync(x =>
                    !x.IsDeleted && x.Status && x.IsCustomer &&
                    x.TenantId == tenantGuid && (isManagerOrAdmin || x.CreatedBy == userId)),

                InactiveClients = await _context.Clients.CountAsync(x =>
                    !x.IsDeleted && !x.Status && x.IsCustomer &&
                    (x.TenantId == tenantGuid && (isManagerOrAdmin || x.CreatedBy == userId)))
            };

            // =========================
            // 🔴 TODAY ACTION CENTER
            // =========================
            var overdueFollowups = await _context.LeadFollowups.CountAsync(lf =>
                lf.NextFollowupDate < today &&
                lf.Status != 3 &&
                 _context.Leads.Any(l => l.LeadID == lf.LeadID && l.TenantId == tenantGuid && (isManagerOrAdmin || l.CreatedBy == userId)));

            // Use HasValue and Value.Date for nullable DateTime
            var todayFollowups = await _context.LeadFollowups.CountAsync(lf =>
                lf.NextFollowupDate.HasValue &&
                lf.NextFollowupDate.Value.Date == today &&
                lf.Status != 3 &&
    
                 _context.Leads.Any(l => l.LeadID == lf.LeadID && l.TenantId == tenantGuid && (isManagerOrAdmin || l.CreatedBy == userId)));

            var pendingQuotations = await _context.Quotations.CountAsync(q =>
                !q.IsDeleted &&
                (q.TenantId == tenantGuid && (isManagerOrAdmin || q.CreatedBy == userId)) &&
                _context.QuotationStatusMaster
                    .Where(s => s.QuotationStatusID == q.QuotationStatusID)
                    .Select(s => s.StatusName)
                    .FirstOrDefault() == "Sent");

            var inactiveLeads = await _context.Leads.CountAsync(l =>
                !l.IsDeleted &&
                (l.TenantId == tenantGuid && (isManagerOrAdmin || l.CreatedBy == userId)) &&
                !_context.LeadFollowups.Any(f =>
                    f.LeadID == l.LeadID &&
                    f.NextFollowupDate >= today.AddDays(-10)));

            // =========================
            // 📋 PENDING TASK LIST (Top 10)
            // =========================
            var pendingTasks = await _context.TaskOccurrences
         .Where(x => x.Status != "Completed" &&
                     x.DueDateTime.HasValue &&                                   // Ensure DueDate exists
                     x.DueDateTime.Value.Date >= start.Date &&                   // Filter by Due Date (not Created)
                     x.DueDateTime.Value.Date <= end.Date &&                     // Important fix
                    
                      _context.TaskSeries.Any(s => s.Id == x.TaskSeriesId &&
                                                  s.CreatedBy.ToString() == userId))
         .OrderBy(x => x.DueDateTime)
         .Take(10)
         .Select(x => new PendingTaskDto
         {
             OccurrenceId = x.Id,
             Title = x.TaskSeries.Title,
             DueDateTime = x.DueDateTime,
             IsOverdue = x.DueDateTime < now
         })
         .ToListAsync();

            // =========================
            // 🔥 HOT LEADS
            // =========================
            var hotLeads = await (
                from l in _context.Leads
                join c in _context.Clients on l.ClientID equals c.ClientID
                where !l.IsDeleted
                   &&  (l.TenantId == tenantGuid && (isManagerOrAdmin || l.CreatedBy == userId))
                orderby l.CreatedDate descending
                select new HotLeadDto
                {
                    LeadId = l.LeadID,
                    LeadName = c.ContactPerson ?? "",
                    LastActivity = l.CreatedDate
                }
            ).Take(5).ToListAsync();

            // =========================
            // 🚨 NEEDS ATTENTION
            // =========================
            var attentionLeads = await (
                from q in _context.Quotations
                join c in _context.Clients on q.ClientID equals c.ClientID
                where !q.IsDeleted &&
                      (q.TenantId == tenantGuid && (isManagerOrAdmin || q.CreatedBy == userId))
                select new AttentionDto
                {
                    ClientName = c.ContactPerson ?? "",
                    Issue = "Quotation sent but no follow-up"
                }
            ).Take(5).ToListAsync();

            // =========================
            // 🧠 SMART SUGGESTIONS
            // =========================
            var suggestions = new List<string>();
            if (overdueFollowups > 0)
                suggestions.Add($"{overdueFollowups} follow-ups are overdue");

            if (pendingQuotations > 0)
                suggestions.Add($"{pendingQuotations} quotations need follow-up");

            if (pendingTasks.Any(t => t.IsOverdue))
                suggestions.Add("You have overdue tasks");

            // =========================
            // 📦 RECENT ORDERS
            // =========================
            var recentOrders = await (
                from o in _context.Orders
                join c in _context.Clients on o.ClientID equals c.ClientID
                where  (o.TenantId == tenantGuid && (isManagerOrAdmin || o.CreatedBy == userId))
                      && o.CreatedDate >= start && o.CreatedDate <= end
                orderby o.CreatedDate descending
                select new RecentOrderDto
                {
                    OrderID = o.OrderID,
                    OrderNo = o.OrderNo,
                    ClientName = c.ContactPerson ?? "",
                    GrandTotal = o.GrandTotal,
                    OrderDate = o.OrderDate
                }
            ).Take(5).ToListAsync();

            // =========================
            // 📄 RECENT QUOTATIONS
            // =========================
            var recentQuotations = await (
                from q in _context.Quotations
                join c in _context.Clients on q.ClientID equals c.ClientID
                where  (q.TenantId == tenantGuid && (isManagerOrAdmin || q.CreatedBy == userId))
                      && q.CreatedDate >= start && q.CreatedDate <= end
                orderby q.CreatedDate descending
                select new RecentQuotationDto
                {
                    QuotationID = q.QuotationID,
                    QuotationNo = q.QuotationNo,
                    ClientName = c.ContactPerson ?? "",
                    GrandTotal = q.GrandTotal
                }
            ).Take(5).ToListAsync();

            // =========================
            // 📅 UPCOMING FOLLOWUPS
            // =========================
            var upcomingFollowups = await (
                from lf in _context.LeadFollowups
                join l in _context.Leads on lf.LeadID equals l.LeadID
                where lf.NextFollowupDate >= today &&
                     (l.TenantId == tenantGuid && (isManagerOrAdmin || l.CreatedBy == userId))
                orderby lf.NextFollowupDate
                select new UpcomingFollowupDto
                {
                    LeadID = lf.LeadID,
                    LeadNo = l.LeadNo ?? "",
                    NextFollowupDate = lf.NextFollowupDate
                }
            ).Take(5).ToListAsync();

            // =========================
            // 🎯 FINAL RESPONSE
            // =========================
            return new DashboardDto
            {
                Counts = counts,
                ClientSummary = clientSummary,
                TodayActions = new TodayActionDto
                {
                    TodayFollowups = todayFollowups,
                    OverdueFollowups = overdueFollowups,
                    PendingQuotations = pendingQuotations,
                    InactiveLeads = inactiveLeads
                },
                PendingTasks = pendingTasks,
                HotLeads = hotLeads,
                NeedsAttention = attentionLeads,
                Suggestions = suggestions,
                RecentOrders = recentOrders,
                RecentQuotations = recentQuotations,
                UpcomingFollowups = upcomingFollowups
            };
        }
    }
}

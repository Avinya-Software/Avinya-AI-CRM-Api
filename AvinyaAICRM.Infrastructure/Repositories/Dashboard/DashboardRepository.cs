using AvinyaAICRM.Application.DTOs.Dashboard;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Dashboard;
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
            bool isSuperAdmin = role == "SuperAdmin";
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
                    !x.IsDeleted &&
                    x.CreatedDate >= start && x.CreatedDate <= end &&
                    (isSuperAdmin || x.TenantId.ToString() == tenantId)),

                Leads = await _context.Leads.CountAsync(x =>
                    !x.IsDeleted &&
                    x.CreatedDate >= start && x.CreatedDate <= end &&
                    (isSuperAdmin || x.TenantId.ToString() == tenantId)),

                Quotations = await _context.Quotations.CountAsync(x =>
                    !x.IsDeleted &&
                    x.CreatedDate >= start && x.CreatedDate <= end &&
                    (isSuperAdmin || x.TenantId.ToString() == tenantId)),

                Orders = await _context.Orders.CountAsync(x =>
                    !x.IsDeleted &&
                    x.CreatedDate >= start && x.CreatedDate <= end &&
                    (isSuperAdmin || x.TenantId.ToString() == tenantId)),

                Products = await _context.Products.CountAsync(x =>
                    !x.IsDeleted &&
                    (isSuperAdmin || x.TenantId.ToString() == tenantId)),

                Expenses = await _context.Expenses.CountAsync(x =>
                    !x.IsDeleted &&
                    x.CreatedDate >= start && x.CreatedDate <= end &&
                    (isSuperAdmin || x.TenantId.ToString() == tenantId)),

                // Fixed Tasks count
                Tasks = await _context.TaskOccurrences.CountAsync(t =>
                    isSuperAdmin ||
                    _context.TaskSeries.Any(s => s.Id == t.TaskSeriesId && s.CreatedBy.ToString() == userId))
            };

            // =========================
            // 👥 CLIENT SUMMARY
            // =========================
            var clientSummary = new ClientSummaryDto
            {
                TotalClients = counts.Clients,

                ActiveClients = await _context.Clients.CountAsync(x =>
                    !x.IsDeleted && x.Status &&
                    x.CreatedDate >= start && x.CreatedDate <= end &&
                    (isSuperAdmin || x.TenantId.ToString() == tenantId)),

                InactiveClients = await _context.Clients.CountAsync(x =>
                    !x.IsDeleted && !x.Status &&
                    x.CreatedDate >= start && x.CreatedDate <= end &&
                    (isSuperAdmin || x.TenantId.ToString() == tenantId))
            };

            // =========================
            // 🔴 TODAY ACTION CENTER
            // =========================
            var overdueFollowups = await _context.LeadFollowups.CountAsync(lf =>
                lf.NextFollowupDate < today &&
                lf.Status != 3 &&
                (isSuperAdmin ||
                 _context.Leads.Any(l => l.LeadID == lf.LeadID && l.TenantId.ToString() == tenantId)));

            // Use HasValue and Value.Date for nullable DateTime
            var todayFollowups = await _context.LeadFollowups.CountAsync(lf =>
                lf.NextFollowupDate.HasValue &&
                lf.NextFollowupDate.Value.Date == today &&
                lf.Status != 3 &&
                (isSuperAdmin ||
                 _context.Leads.Any(l => l.LeadID == lf.LeadID && l.TenantId.ToString() == tenantId)));

            var pendingQuotations = await _context.Quotations.CountAsync(q =>
                !q.IsDeleted &&
                q.CreatedDate >= start && q.CreatedDate <= end &&
                (isSuperAdmin || q.TenantId.ToString() == tenantId) &&
                _context.QuotationStatusMaster
                    .Where(s => s.QuotationStatusID == q.QuotationStatusID)
                    .Select(s => s.StatusName)
                    .FirstOrDefault() == "Sent");

            var inactiveLeads = await _context.Leads.CountAsync(l =>
                !l.IsDeleted &&
                l.CreatedDate >= start && l.CreatedDate <= end &&
                (isSuperAdmin || l.TenantId.ToString() == tenantId) &&
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
                     (isSuperAdmin ||
                      _context.TaskSeries.Any(s => s.Id == x.TaskSeriesId &&
                                                  s.CreatedBy.ToString() == userId)))
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
                   && l.CreatedDate >= start
                   && l.CreatedDate <= end
                   && (isSuperAdmin || l.TenantId.ToString() == tenantId)
                orderby l.CreatedDate descending
                select new HotLeadDto
                {
                    LeadId = l.LeadID,
                    LeadName = c.ContactPerson,
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
                      q.CreatedDate >= start && q.CreatedDate <= end &&
                      q.CreatedDate < now.AddDays(-5) &&
                      (isSuperAdmin || q.TenantId.ToString() == tenantId)
                select new AttentionDto
                {
                    ClientName = c.ContactPerson,
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
                where (isSuperAdmin || o.TenantId.ToString() == tenantId)
                      && o.CreatedDate >= start && o.CreatedDate <= end
                orderby o.CreatedDate descending
                select new RecentOrderDto
                {
                    OrderID = o.OrderID,
                    OrderNo = o.OrderNo,
                    ClientName = c.ContactPerson,
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
                where (isSuperAdmin || q.TenantId.ToString() == tenantId)
                      && q.CreatedDate >= start && q.CreatedDate <= end
                orderby q.CreatedDate descending
                select new RecentQuotationDto
                {
                    QuotationID = q.QuotationID,
                    QuotationNo = q.QuotationNo,
                    ClientName = c.ContactPerson,
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
                      (isSuperAdmin || l.TenantId.ToString() == tenantId)
                orderby lf.NextFollowupDate
                select new UpcomingFollowupDto
                {
                    LeadID = lf.LeadID,
                    LeadNo = l.LeadNo,
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

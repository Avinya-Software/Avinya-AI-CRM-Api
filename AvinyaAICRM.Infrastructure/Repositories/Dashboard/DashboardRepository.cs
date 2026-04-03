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

        public async Task<DashboardDto> GetDashboardAsync(string tenantId, string? role, string? userId)
        {
            bool isSuperAdmin = role == "SuperAdmin";

            var today = DateTime.Today;


            var counts = new DashboardCounts
            {
                Clients = await _context.Clients.CountAsync(x => !x.IsDeleted && (isSuperAdmin || x.TenantId.ToString() == tenantId)),
                Leads = await _context.Leads.CountAsync(x => !x.IsDeleted && (isSuperAdmin || x.TenantId.ToString() == tenantId)),
                Quotations = await _context.Quotations.CountAsync(x => !x.IsDeleted && (isSuperAdmin || x.TenantId.ToString() == tenantId)),
                Orders = await _context.Orders.CountAsync(x => !x.IsDeleted && (isSuperAdmin || x.TenantId.ToString() == tenantId)),
                Products = await _context.Products.CountAsync(x => !x.IsDeleted && (isSuperAdmin || x.TenantId.ToString() == tenantId)),
                Expenses = await _context.Expenses.CountAsync(x => !x.IsDeleted && (isSuperAdmin || x.TenantId.ToString() == tenantId)),
                Tasks = await _context.TaskOccurrences.CountAsync(t => isSuperAdmin ||
                    _context.TaskSeries
                        .Any(l => l.Id == t.TaskSeriesId &&
                                  l.CreatedBy.ToString() == userId))
            };


            var clientSummary = new ClientSummaryDto
            {
                TotalClients = await _context.Clients.CountAsync(x => !x.IsDeleted && (isSuperAdmin || x.TenantId.ToString() == tenantId)),

                ActiveClients = await _context.Clients
                    .CountAsync(x => !x.IsDeleted && x.Status == true && (isSuperAdmin || x.TenantId.ToString() == tenantId)),

                InactiveClients = await _context.Clients
                    .CountAsync(x => !x.IsDeleted && x.Status == false && (isSuperAdmin || x.TenantId.ToString() == tenantId))
            };

            //var overdueFollowups = await _context.LeadFollowups
            //    .CountAsync(x =>
            //        x.NextFollowupDate < today
            //        && x.Status != 3
            //    );

            var overdueFollowups = await _context.LeadFollowups
                .CountAsync(lf =>
                 lf.NextFollowupDate < today
                    && lf.Status != 3 &&
                    (isSuperAdmin ||
                    _context.Leads
                        .Any(l => l.LeadID == lf.LeadID &&
                                  l.TenantId.ToString() == tenantId))
                );


            var pendingQuotations = await _context.Quotations
                .CountAsync(x =>
                    !x.IsDeleted && (isSuperAdmin || x.TenantId.ToString() == tenantId) 
                    && _context.QuotationStatusMaster
                        .Where(s => s.QuotationStatusID == x.QuotationStatusID)
                        .Select(s => s.StatusName)
                        .FirstOrDefault() == "Pending");

            var totalOrders = await _context.Orders
            .CountAsync(x => !x.IsDeleted && (isSuperAdmin || x.TenantId.ToString() == tenantId));

            var pendingOrders = await _context.Orders
                .CountAsync(x =>
                    !x.IsDeleted &&
                    x.Status == 1
                    && (isSuperAdmin || x.TenantId.ToString() == tenantId)
                );

            var recentOrders = await _context.Orders
                  .Where(x => isSuperAdmin || x.TenantId.ToString() == tenantId)
                .OrderByDescending(x => x.CreatedDate)
                .Take(5)
                .Select(o => new RecentOrderDto
                {
                    OrderID = o.OrderID,
                    OrderNo = o.OrderNo,

                    ClientName = _context.Clients
                        .Where(c => c.ClientID == o.ClientID)
                        .Select(c => c.ContactPerson)
                        .FirstOrDefault(),

                    GrandTotal = o.GrandTotal,
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            var recentQuotations = await _context.Quotations
                  .Where(x => isSuperAdmin || x.TenantId.ToString() == tenantId)
                .OrderByDescending(x => x.CreatedDate)
                .Take(5)
                .Select(q => new RecentQuotationDto
                {
                    QuotationID = q.QuotationID,
                    QuotationNo = q.QuotationNo,

                    ClientName = _context.Clients
                        .Where(c => c.ClientID == q.ClientID)
                        .Select(c => c.ContactPerson)
                        .FirstOrDefault(),

                    GrandTotal = q.GrandTotal
                })
                .ToListAsync();

            var upcomingFollowups = await (
                     from lf in _context.LeadFollowups
                     join l in _context.Leads on lf.LeadID equals l.LeadID
                     where lf.NextFollowupDate >= today
                           && (isSuperAdmin || l.TenantId.ToString() == tenantId)
                     orderby lf.NextFollowupDate
                     select new UpcomingFollowupDto
                     {
                         LeadID = lf.LeadID,
                         LeadNo = l.LeadNo,
                         NextFollowupDate = lf.NextFollowupDate
                     }
                 )
                 .Take(5)
                 .ToListAsync();

            var pendingTasks = await _context.TaskOccurrences
                .Where(x => x.Status != "Completed")
                .OrderBy(x => x.DueDateTime)
                .Take(5)
                .Select(x => new PendingTaskDto
                {
                    OccurrenceId = x.Id,
                    Title = x.TaskSeries.Title,
                    DueDateTime = x.DueDateTime
                })
                .ToListAsync();


            return new DashboardDto
            {
                Counts = counts,

                ClientSummary = clientSummary,

                OverdueFollowupsCount = overdueFollowups,

                PendingQuotationsCount = pendingQuotations,

                TotalOrdersCount = totalOrders,

                PendingOrdersCount = pendingOrders,

                RecentOrders = recentOrders,

                RecentQuotations = recentQuotations,

                UpcomingFollowups = upcomingFollowups,

                PendingTasks = pendingTasks
            };
        }
    }
}

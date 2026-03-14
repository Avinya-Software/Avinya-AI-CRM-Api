using AvinyaAICRM.Application.DTOs.Dashboard;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Dashboard;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.Dashboard
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var today = DateTime.Today;


            var counts = new DashboardCounts
            {
                Clients = await _context.Clients.CountAsync(x => !x.IsDeleted),
                Leads = await _context.Leads.CountAsync(x => !x.IsDeleted),
                Quotations = await _context.Quotations.CountAsync(x => !x.IsDeleted),
                Orders = await _context.Orders.CountAsync(x => !x.IsDeleted),
                Products = await _context.Products.CountAsync(x => !x.IsDeleted),
                Expenses = await _context.Expenses.CountAsync(x => !x.IsDeleted),
                Tasks = await _context.TaskOccurrences.CountAsync()
            };


            var clientSummary = new ClientSummaryDto
            {
                TotalClients = await _context.Clients.CountAsync(x => !x.IsDeleted),

                ActiveClients = await _context.Clients
                    .CountAsync(x => !x.IsDeleted && x.Status == true),

                InactiveClients = await _context.Clients
                    .CountAsync(x => !x.IsDeleted && x.Status == false)
            };

            var overdueFollowups = await _context.LeadFollowups
                .CountAsync(x =>
                    x.NextFollowupDate < today
                    && x.Status != 3
                );

            var pendingQuotations = await _context.Quotations
                .CountAsync(x =>
                    !x.IsDeleted &&
                    _context.QuotationStatusMaster
                        .Where(s => s.QuotationStatusID == x.Status)
                        .Select(s => s.StatusName)
                        .FirstOrDefault() == "Pending");

            var totalOrders = await _context.Orders
            .CountAsync(x => !x.IsDeleted);

            var pendingOrders = await _context.Orders
                .CountAsync(x =>
                    !x.IsDeleted &&
                    x.Status == 1
                );

            var recentOrders = await _context.Orders
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

            var upcomingFollowups = await _context.LeadFollowups
                .Where(x => x.NextFollowupDate >= today)
                .OrderBy(x => x.NextFollowupDate)
                .Take(5)
                .Select(x => new UpcomingFollowupDto
                {
                    LeadID = x.LeadID,

                    LeadNo = _context.Leads
                        .Where(l => l.LeadID == x.LeadID)
                        .Select(l => l.LeadNo)
                        .FirstOrDefault(),

                    NextFollowupDate = x.NextFollowupDate
                })
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

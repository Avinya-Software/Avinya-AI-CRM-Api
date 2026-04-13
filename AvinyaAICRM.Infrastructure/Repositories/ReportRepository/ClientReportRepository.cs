using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Repositories.ReportRepository
{
    public class ClientReportRepository : IClientReportRepository
    {
        private readonly AppDbContext _context;

        public ClientReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ClientReportDto> GetClientReportAsync(ClientReportFilterDto filter)
        {
            // ── Master lookups (one round-trip each) ─────────────────────────────
            var stateMap = await _context.States
                .ToDictionaryAsync(s => s.StateID, s => s.StateName);

            var cityMap = await _context.Cities
                .ToDictionaryAsync(c => c.CityID, c => c.CityName);

            var invoiceStatusMap = await _context.InvoiceStatuses
                .ToDictionaryAsync(s => s.InvoiceStatusID, s => s.InvoiceStatusName);

            // ── Clients base query ────────────────────────────────────────────────
            var clientsQuery = _context.Clients
                .Where(c => !c.IsDeleted && c.TenantId == filter.TenantId);

            if (filter.ClientId.HasValue)
                clientsQuery = clientsQuery.Where(c => c.ClientID == filter.ClientId.Value);

            if (filter.ClientType.HasValue)
                clientsQuery = clientsQuery.Where(c => c.ClientType == filter.ClientType.Value);

            if (filter.StateId.HasValue)
                clientsQuery = clientsQuery.Where(c => c.StateID == filter.StateId.Value);

            var clients = await clientsQuery.ToListAsync();
            var clientIds = clients.Select(c => c.ClientID).ToList();

            // ── Orders in date range ──────────────────────────────────────────────
            var ordersQuery = _context.Orders
                .Where(o => !o.IsDeleted &&
                            clientIds.Contains(o.ClientID) &&
                            o.TenantId == filter.TenantId);

            if (filter.DateFrom.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= filter.DateTo.Value);

            var orders = await ordersQuery
                .Select(o => new
                {
                    o.OrderID,
                    o.ClientID,
                    o.OrderDate,
                    o.GrandTotal
                })
                .ToListAsync();

            var orderIds = orders.Select(o => o.OrderID.ToString()).ToList();

            // ── Invoices linked to those orders ───────────────────────────────────
            // Note: Invoices.OrderID is nvarchar — string comparison needed
            var invoices = await _context.Invoices
                .Where(i => !i.IsDeleted && orderIds.Contains(i.OrderID))
                .Select(i => new
                {
                    i.InvoiceID,
                    i.OrderID,
                    i.ClientID,
                    i.GrandTotal,
                    i.PaidAmount,
                    i.OutstandingAmount,
                    i.InvoiceStatusID,
                    i.DueDate,
                    i.InvoiceNo,
                    i.InvoiceDate
                })
                .ToListAsync();

            // ── Payments for those invoices ───────────────────────────────────────
            var invoiceIds = invoices.Select(i => i.InvoiceID).ToList();
            var payments = await _context.Payments
                .Where(p => invoiceIds.Contains(p.InvoiceID))
                .Select(p => new
                {
                    p.InvoiceID,
                    p.Amount,
                    p.PaymentDate,
                    p.PaymentMode
                })
                .ToListAsync();

            // ── Leads ─────────────────────────────────────────────────────────────
            var leads = await _context.Leads
                .Where(l => !l.IsDeleted &&
                            l.ClientID.HasValue &&
                            clientIds.Contains(l.ClientID.Value) &&
                            l.TenantId == filter.TenantId)
                .Select(l => new { l.LeadID, l.ClientID, l.Date, l.LeadStatusID })
                .ToListAsync();

            var leadStatusMap = await _context.leadStatusMasters
                .ToDictionaryAsync(s => s.LeadStatusID, s => s.StatusName);

            // ── Quotations ────────────────────────────────────────────────────────
            var quotations = await _context.Quotations
                .Where(q => !q.IsDeleted &&
                            q.ClientID.HasValue &&
                            clientIds.Contains(q.ClientID.Value) &&
                            q.TenantId == filter.TenantId)
                .Select(q => new { q.QuotationID, q.ClientID, q.QuotationStatusID })
                .ToListAsync();

            var quotStatusMap = await _context.QuotationStatusMaster
                .ToDictionaryAsync(s => s.QuotationStatusID, s => s.StatusName);

            // ── Projects ──────────────────────────────────────────────────────────
            var projects = await _context.Projects
                .Where(p => p.IsDeleted != true &&
                            p.ClientID.HasValue &&
                            clientIds.Contains(p.ClientID.Value) &&
                            p.TenantId == filter.TenantId)
                .Select(p => new { p.ProjectID, p.ClientID, p.EstimatedValue })
                .ToListAsync();

            // ── KPIs ──────────────────────────────────────────────────────────────
            var today = DateTime.UtcNow;
            decimal totInvoiced = invoices.Sum(i => i.GrandTotal);
            decimal totCollected = payments.Sum(p => p.Amount);
            decimal totOutstanding = invoices.Sum(i => i.OutstandingAmount);

            decimal totOverdue = invoices
                .Where(i => i.DueDate.HasValue && i.DueDate.Value < today && i.OutstandingAmount > 0)
                .Sum(i => i.OutstandingAmount);

            decimal avgOrderValue = orders.Any()
                                ? Math.Round(orders.Average(o => o.GrandTotal), 2)
                                : 0;

            var kpi = new ClientReportKpiDto
            {
                TotalClients = clients.Count,
                ActiveClients = clients.Count(c => c.Status),
                TotalInvoiced = totInvoiced,
                TotalCollected = totCollected,
                TotalOutstanding = totOutstanding,
                TotalOverdue = totOverdue,
                AverageOrderValue = avgOrderValue,
                TotalOrders = orders.Count
            };

            // ── Top clients by revenue ────────────────────────────────────────────
            var topClients = clients.Select(c =>
            {
                var cInvoices = invoices.Where(i => i.ClientID == c.ClientID.ToString()).ToList();
                var cOrders = orders.Where(o => o.ClientID == c.ClientID).ToList();
                decimal inv = cInvoices.Sum(i => i.GrandTotal);

                return new ClientRevenueItemDto
                {
                    ClientId = c.ClientID,
                    CompanyName = c.CompanyName,
                    ContactPerson = c.ContactPerson ?? string.Empty,
                    StateName = c.StateID.HasValue && stateMap.ContainsKey(c.StateID.Value)
                                        ? stateMap[c.StateID.Value] : string.Empty,
                    TotalOrders = cOrders.Count,
                    TotalInvoiced = inv,
                    TotalCollected = cInvoices.Sum(i => i.PaidAmount),
                    Outstanding = cInvoices.Sum(i => i.OutstandingAmount),
                    RevenueShare = totInvoiced > 0
                                        ? Math.Round(inv / totInvoiced * 100, 1) : 0
                };
            })
            .OrderByDescending(c => c.TotalInvoiced)
            .ToList();

            // ── Client 360 ────────────────────────────────────────────────────────
            var client360 = clients.Select(c =>
            {
                var cInvoices = invoices.Where(i => i.ClientID == c.ClientID.ToString()).ToList();
                var cPayments = payments
                    .Where(p => cInvoices.Select(i => i.InvoiceID).Contains(p.InvoiceID))
                    .ToList();
                var cOrders = orders.Where(o => o.ClientID == c.ClientID).ToList();
                var cLeads = leads.Where(l => l.ClientID == c.ClientID).ToList();
                var cQuots = quotations.Where(q => q.ClientID == c.ClientID).ToList();
                var cProjects = projects.Where(p => p.ClientID == c.ClientID).ToList();

                int convertedLeads = cLeads.Count(l =>
                    l.LeadStatusID.HasValue &&
                    leadStatusMap.TryGetValue(l.LeadStatusID.Value, out var sn) &&
                    sn == "Converted");

                int acceptedQuots = cQuots.Count(q =>
                                  quotStatusMap.TryGetValue(q.QuotationStatusID, out var sn) &&
                                  sn == "Accepted");

                // Preferred payment mode — most used
                var prefMode = cPayments
                    .GroupBy(p => p.PaymentMode)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? string.Empty;

                // Avg days from InvoiceDate to first payment
                double avgDays = 0;
                var paidInvoices = cInvoices
                    .Where(i => cPayments.Any(p => p.InvoiceID == i.InvoiceID))
                    .ToList();
                if (paidInvoices.Any())
                {
                    avgDays = paidInvoices.Average(i =>
                    {
                        var firstPay = cPayments
                            .Where(p => p.InvoiceID == i.InvoiceID)
                            .Min(p => p.PaymentDate);
                        return (firstPay - i.InvoiceDate).TotalDays;
                    });
                    avgDays = Math.Round(avgDays, 1);
                }

                return new Client360Dto
                {
                    ClientId = c.ClientID,
                    CompanyName = c.CompanyName,
                    ContactPerson = c.ContactPerson ?? string.Empty,
                    Mobile = c.Mobile ?? string.Empty,
                    Email = c.Email ?? string.Empty,
                    GSTNo = c.GSTNo ?? string.Empty,
                    StateName = c.StateID.HasValue && stateMap.ContainsKey(c.StateID.Value)
                                              ? stateMap[c.StateID.Value] : string.Empty,
                    CityName = c.CityID.HasValue && cityMap.ContainsKey(c.CityID.Value)
                                              ? cityMap[c.CityID.Value] : string.Empty,
                    TotalLeads = cLeads.Count,
                    ConvertedLeads = convertedLeads,
                    TotalQuotations = cQuots.Count,
                    AcceptedQuotations = acceptedQuots,
                    TotalOrders = cOrders.Count,
                    TotalProjects = cProjects.Count,
                    TotalInvoiced = cInvoices.Sum(i => i.GrandTotal),
                    TotalCollected = cInvoices.Sum(i => i.PaidAmount),
                    Outstanding = cInvoices.Sum(i => i.OutstandingAmount),
                    TotalExpectedValue = cProjects.Sum(p => p.EstimatedValue ?? 0),
                    PreferredPaymentMode = prefMode,
                    AvgDaysToPayment = avgDays,
                    LastOrderDate = cOrders.Any() ? cOrders.Max(o => o.OrderDate) : null,
                    LastPaymentDate = cPayments.Any() ? cPayments.Max(p => p.PaymentDate) : null,
                    LastLeadDate = cLeads.Any() ? cLeads.Max(l => l.Date) : null
                };
            })
            .OrderByDescending(c => c.TotalInvoiced)
            .ToList();

            // ── Aging list (overdue + upcoming outstanding) ───────────────────────
            var agingList = invoices
                .Where(i => i.OutstandingAmount > 0)
                .Select(i =>
                {
                    var client = clients.FirstOrDefault(c => c.ClientID.ToString() == i.ClientID);
                    int daysOverdue = i.DueDate.HasValue && i.DueDate.Value < today
                        ? (int)(today - i.DueDate.Value).TotalDays
                        : 0;

                    return new ClientAgingRowDto
                    {
                        CompanyName = client?.CompanyName ?? "—",
                        InvoiceNo = i.InvoiceNo,
                        Outstanding = i.OutstandingAmount,
                        DueDate = i.DueDate ?? i.InvoiceDate,
                        DaysOverdue = daysOverdue,
                        InvoiceStatus = invoiceStatusMap.TryGetValue(i.InvoiceStatusID, out var sn)
                                            ? sn : "—"
                    };
                })
                .OrderByDescending(a => a.DaysOverdue)
                .ToList();

            // ── State-wise revenue breakdown ──────────────────────────────────────
            var stateBreakdown = clients
                .Where(c => c.StateID.HasValue)
                .GroupBy(c => c.StateID!.Value)
                .Select(g =>
                {
                    var stateClientIds = g.Select(c => c.ClientID.ToString()).ToList();
                    decimal stateInv = invoices
                        .Where(i => stateClientIds.Contains(i.ClientID))
                        .Sum(i => i.GrandTotal);

                    return new StateRevenueDto
                    {
                        StateName = stateMap.TryGetValue(g.Key, out var sn) ? sn : "Unknown",
                        ClientCount = g.Count(),
                        TotalInvoiced = stateInv,
                        Percentage = totInvoiced > 0
                                            ? Math.Round(stateInv / totInvoiced * 100, 1) : 0
                    };
                })
                .OrderByDescending(s => s.TotalInvoiced)
                .ToList();

            return new ClientReportDto
            {
                Kpi = kpi,
                TopClients = topClients,
                Client360 = client360,
                AgingList = agingList,
                StateBreakdown = stateBreakdown,
                AppliedFilters = filter
            };
        }
    }
}

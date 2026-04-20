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
                .Where(c => c.TenantId == filter.TenantId);

            if (filter.ClientId.HasValue)
                clientsQuery = clientsQuery.Where(c => c.ClientID == filter.ClientId.Value);

            if (filter.ClientType.HasValue)
                clientsQuery = clientsQuery.Where(c => c.ClientType == filter.ClientType.Value);

            if (filter.StateId.HasValue)
                clientsQuery = clientsQuery.Where(c => c.StateID == filter.StateId.Value);

            // Total count for pagination
            var totalClient360Count = await clientsQuery.CountAsync();

            // All clients matching filters (needed for KPI calculation and Top Clients)
            var allFilteredClients = await clientsQuery.ToListAsync();
            var allFilteredClientIds = allFilteredClients.Select(c => c.ClientID).ToList();

            // Paginated clients for the 360 list
            var paginatedClients = await clientsQuery
                .OrderByDescending(c => c.CreatedDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();
            var paginatedClientIds = paginatedClients.Select(c => c.ClientID).ToList();


            // ── KPI DATA (Date Range Filtered) ───────────────────────────────────
            var ordersQueryForKpi = _context.Orders
                .Where(o => !o.IsDeleted &&
                            allFilteredClientIds.Contains(o.ClientID) &&
                            o.TenantId == filter.TenantId);

            if (filter.DateFrom.HasValue)
                ordersQueryForKpi = ordersQueryForKpi.Where(o => o.OrderDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                ordersQueryForKpi = ordersQueryForKpi.Where(o => o.OrderDate <= filter.DateTo.Value);

            var ordersForKpi = await ordersQueryForKpi
                .Select(o => new { o.OrderID, o.ClientID, o.OrderDate, o.GrandTotal })
                .ToListAsync();

            var orderIdsForKpi = ordersForKpi.Select(o => o.OrderID.ToString()).ToList();

            var invoicesForKpi = await _context.Invoices
                .Where(i => !i.IsDeleted && orderIdsForKpi.Contains(i.OrderID))
                .Select(i => new
                {
                    i.InvoiceID, i.OrderID, i.ClientID, i.GrandTotal, i.PaidAmount,
                    i.RemainingPayment, i.InvoiceStatusID, i.DueDate, i.InvoiceNo, i.InvoiceDate
                })
                .ToListAsync();

            var invoiceIdsForKpi = invoicesForKpi.Select(i => i.InvoiceID).ToList();
            var paymentsForKpi = await _context.Payments
                .Where(p => invoiceIdsForKpi.Contains(p.InvoiceID))
                .Select(p => new { p.InvoiceID, p.Amount, p.PaymentDate, p.PaymentMode })
                .ToListAsync();


            // ── 360 DATA (LIFETIME - for paginated clients only) ──────────────────
            var orders360 = await _context.Orders
                .Where(o => !o.IsDeleted && paginatedClientIds.Contains(o.ClientID))
                .Select(o => new { o.OrderID, o.ClientID, o.OrderDate, o.GrandTotal })
                .ToListAsync();

            var orderIds360Str = orders360.Select(o => o.OrderID.ToString()).ToList();
            var invoices360 = await _context.Invoices
                .Where(i => !i.IsDeleted && orderIds360Str.Contains(i.OrderID))
                .Select(i => new
                {
                    i.InvoiceID, i.OrderID, i.ClientID, i.GrandTotal, i.PaidAmount,
                    i.RemainingPayment, i.InvoiceDate, i.InvoiceStatusID
                })
                .ToListAsync();

            var invoiceIds360 = invoices360.Select(i => i.InvoiceID).ToList();
            var payments360 = await _context.Payments
                .Where(p => invoiceIds360.Contains(p.InvoiceID))
                .Select(p => new { p.InvoiceID, p.Amount, p.PaymentDate, p.PaymentMode })
                .ToListAsync();

            var leads360 = await _context.Leads
                .Where(l => !l.IsDeleted && l.ClientID.HasValue && paginatedClientIds.Contains(l.ClientID.Value))
                .Select(l => new { l.LeadID, l.ClientID, l.Date, l.LeadStatusID })
                .ToListAsync();

            var quots360 = await _context.Quotations
                .Where(q => !q.IsDeleted && q.ClientID.HasValue && paginatedClientIds.Contains(q.ClientID.Value))
                .Select(q => new { q.QuotationID, q.ClientID, q.QuotationStatusID })
                .ToListAsync();

            var projects360 = await _context.Projects
                .Where(p => p.IsDeleted != true && p.ClientID.HasValue && paginatedClientIds.Contains(p.ClientID.Value))
                .Select(p => new { p.ProjectID, p.ClientID, p.EstimatedValue })
                .ToListAsync();


            // ── KPI CALCULATIONS (Using filtered data) ──────────────────────────
            var today = DateTime.UtcNow;
            decimal totInvoiced = invoicesForKpi.Sum(i => i.GrandTotal);
            decimal totCollected = paymentsForKpi.Sum(p => p.Amount);
            decimal totRemainingPayment = invoicesForKpi.Sum(i => i.RemainingPayment);
            decimal totOverdue = invoicesForKpi.Where(i => i.DueDate.HasValue && i.DueDate.Value < today && i.RemainingPayment > 0)
                                     .Sum(i => i.RemainingPayment);

            decimal avgOrderValue = ordersForKpi.Any() ? Math.Round(ordersForKpi.Average(o => o.GrandTotal), 2) : 0;

            var kpi = new ClientReportKpiDto
            {
                TotalClients = allFilteredClients.Count,
                ActiveClients = allFilteredClients.Count(c => c.Status),
                TotalInvoiced = totInvoiced,
                TotalCollected = totCollected,
                TotalRemainingPayment = totRemainingPayment,
                TotalOverdue = totOverdue,
                AverageOrderValue = avgOrderValue,
                TotalOrders = ordersForKpi.Count
            };

            // ── Top clients by revenue (Filtered) ────────────────────────────────
            var topClients = allFilteredClients.Select(c =>
            {
                var cInvoices = invoicesForKpi.Where(i => i.ClientID == c.ClientID.ToString()).ToList();
                var cOrders = ordersForKpi.Where(o => o.ClientID == c.ClientID).ToList();
                decimal inv = cInvoices.Sum(i => i.GrandTotal);

                return new ClientRevenueItemDto
                {
                    ClientId = c.ClientID,
                    CompanyName = c.CompanyName,
                    ContactPerson = c.ContactPerson ?? string.Empty,
                    StateName = c.StateID.HasValue && stateMap.ContainsKey(c.StateID.Value) ? stateMap[c.StateID.Value] : string.Empty,
                    TotalOrders = cOrders.Count,
                    TotalInvoiced = inv,
                    TotalCollected = cInvoices.Sum(i => i.PaidAmount),
                    RemainingPayment = cInvoices.Sum(i => i.RemainingPayment), 
                    RevenueShare = totInvoiced > 0 ? Math.Round(inv / totInvoiced * 100, 1) : 0
                };
            })
            .OrderByDescending(c => c.TotalInvoiced)
            .Take(10) // Limit top clients
            .ToList();

            // ── Client 360 (Lifetime for paginated clients) ──────────────────────
            var leadStatusMap = await _context.leadStatusMasters.ToDictionaryAsync(s => s.LeadStatusID, s => s.StatusName);
            var quotStatusMap = await _context.QuotationStatusMaster.ToDictionaryAsync(s => s.QuotationStatusID, s => s.StatusName);

            var client360 = paginatedClients.Select(c =>
            {
                var cInvoices = invoices360.Where(i => i.ClientID == c.ClientID.ToString()).ToList();
                var cPayments = payments360.Where(p => cInvoices.Select(i => i.InvoiceID).Contains(p.InvoiceID)).ToList();
                var cOrders = orders360.Where(o => o.ClientID == c.ClientID).ToList();
                var cLeads = leads360.Where(l => l.ClientID == c.ClientID).ToList();
                var cQuots = quots360.Where(q => q.ClientID == c.ClientID).ToList();
                var cProjects = projects360.Where(p => p.ClientID == c.ClientID).ToList();

                int convertedLeads = cLeads.Count(l => l.LeadStatusID.HasValue && leadStatusMap.TryGetValue(l.LeadStatusID.Value, out var sn) && sn == "Converted");
                int acceptedQuots = cQuots.Count(q => quotStatusMap.TryGetValue(q.QuotationStatusID, out var sn) && sn == "Accepted");

                var prefMode = cPayments.GroupBy(p => p.PaymentMode).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? string.Empty;

                double avgDays = 0;
                var paidInvoices = cInvoices.Where(i => cPayments.Any(p => p.InvoiceID == i.InvoiceID)).ToList();
                if (paidInvoices.Any())
                {
                    avgDays = paidInvoices.Average(i =>
                    {
                        var firstPay = cPayments.Where(p => p.InvoiceID == i.InvoiceID).Min(p => p.PaymentDate);
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
                    StateName = c.StateID.HasValue && stateMap.ContainsKey(c.StateID.Value) ? stateMap[c.StateID.Value] : string.Empty,
                    CityName = c.CityID.HasValue && cityMap.ContainsKey(c.CityID.Value) ? cityMap[c.CityID.Value] : string.Empty,
                    TotalLeads = cLeads.Count,
                    ConvertedLeads = convertedLeads,
                    TotalQuotations = cQuots.Count,
                    AcceptedQuotations = acceptedQuots,
                    TotalOrders = cOrders.Count,
                    TotalProjects = cProjects.Count,
                    TotalInvoiced = cInvoices.Sum(i => i.GrandTotal),
                    TotalCollected = cInvoices.Sum(i => i.PaidAmount),
                    RemainingPayment = cInvoices.Sum(i => i.RemainingPayment),
                    TotalExpectedValue = cProjects.Sum(p => p.EstimatedValue ?? 0),
                    PreferredPaymentMode = prefMode,
                    AvgDaysToPayment = avgDays,
                    LastOrderDate = cOrders.Any() ? cOrders.Max(o => o.OrderDate) : null,
                    LastPaymentDate = cPayments.Any() ? cPayments.Max(p => p.PaymentDate) : null,
                    LastLeadDate = cLeads.Any() ? cLeads.Max(l => l.Date) : null
                };
            }).ToList();

            // ── Aging list (Using Filtered Data) ─────────────────────────────────
            var agingList = invoicesForKpi
                .Where(i => i.RemainingPayment > 0) 
                .Select(i =>
                {
                    var client = allFilteredClients.FirstOrDefault(c => c.ClientID.ToString() == i.ClientID);
                    int daysOverdue = i.DueDate.HasValue && i.DueDate.Value < today ? (int)(today - i.DueDate.Value).TotalDays : 0;

                    return new ClientAgingRowDto
                    {
                        CompanyName = client?.CompanyName ?? "—",
                        InvoiceNo = i.InvoiceNo,
                        RemainingPayment = i.RemainingPayment,
                        DueDate = i.DueDate ?? i.InvoiceDate,
                        DaysOverdue = daysOverdue,
                        InvoiceStatus = invoiceStatusMap.TryGetValue(i.InvoiceStatusID, out var sn) ? sn : "—"
                    };
                })
                .OrderByDescending(a => a.DaysOverdue)
                .ToList();

            // ── State breakdown (Using Filtered Data) ────────────────────────────
            var stateBreakdown = allFilteredClients
                .Where(c => c.StateID.HasValue)
                .GroupBy(c => c.StateID!.Value)
                .Select(g =>
                {
                    var stateClientIds = g.Select(c => c.ClientID.ToString()).ToList();
                    decimal stateInv = invoicesForKpi.Where(i => stateClientIds.Contains(i.ClientID)).Sum(i => i.GrandTotal);

                    return new StateRevenueDto
                    {
                        StateName = stateMap.TryGetValue(g.Key, out var sn) ? sn : "Unknown",
                        ClientCount = g.Count(),
                        TotalInvoiced = stateInv,
                        Percentage = totInvoiced > 0 ? Math.Round(stateInv / totInvoiced * 100, 1) : 0
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
                Client360TotalCount = totalClient360Count,
                AppliedFilters = filter
            };
        }

        public async Task<ClientDrillDownResponseDto> GetClientDrillDownAsync(Guid clientId, Guid tenantId)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.ClientID == clientId && c.TenantId == tenantId);

            if (client == null) return new ClientDrillDownResponseDto();

            var response = new ClientDrillDownResponseDto
            {
                ClientId = client.ClientID,
                CompanyName = client.CompanyName
            };

            var productMap = await _context.Products.ToDictionaryAsync(p => p.ProductID, p => p.ProductName);
            var usersMap = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.UserName);

            // 1. Leads
            var leadStatusMap = await _context.leadStatusMasters.ToDictionaryAsync(s => s.LeadStatusID, s => s.StatusName);
            response.Leads = await _context.Leads
                .Where(l => l.ClientID == clientId && l.TenantId == tenantId && !l.IsDeleted)
                .OrderByDescending(l => l.Date)
                .Select(l => new ClientDrillDownLeadDto
                {
                    LeadId = l.LeadID,
                    LeadNo = l.LeadNo ?? string.Empty,
                    Date = l.Date ?? DateTime.MinValue,
                    Status = l.LeadStatusID.HasValue && leadStatusMap.ContainsKey(l.LeadStatusID.Value) ? leadStatusMap[l.LeadStatusID.Value] : "Pending",
                    AssignedTo = l.AssignedTo != null && usersMap.ContainsKey(l.AssignedTo) ? usersMap[l.AssignedTo]! : "Unassigned",
                    Requirement = l.RequirementDetails ?? string.Empty
                })
                .ToListAsync();

            // 2. Quotations
            var quotStatusMap = await _context.QuotationStatusMaster.ToDictionaryAsync(s => s.QuotationStatusID, s => s.StatusName);
            var quotationEntities = await _context.Quotations
                .Where(q => q.ClientID == clientId && q.TenantId == tenantId && !q.IsDeleted)
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();

            var quotationIds = quotationEntities.Select(q => q.QuotationID).ToList();
            var quotationItems = await _context.QuotationItems
                .Where(qi => quotationIds.Contains(qi.QuotationID))
                .ToListAsync();

            response.Quotations = quotationEntities.Select(q => new ClientDrillDownQuotationDto
            {
                QuotationId = q.QuotationID,
                QuotationNo = q.QuotationNo ?? string.Empty,
                Date = q.CreatedDate,
                TotalAmount = q.TotalAmount,
                Status = quotStatusMap.ContainsKey(q.QuotationStatusID) ? quotStatusMap[q.QuotationStatusID] : "Draft",
                Items = quotationItems.Where(i => i.QuotationID == q.QuotationID).Select(i => new ClientDrillDownItemDto
                {
                    ProductId = i.ProductID,
                    ProductName = productMap.ContainsKey(i.ProductID) ? productMap[i.ProductID] : "Unknown Product",
                    Description = i.Description ?? string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Total = i.Quantity * i.UnitPrice
                }).ToList()
            }).ToList();

            // 3. Orders
            var orderEntities = await _context.Orders
                .Where(o => o.ClientID == clientId && o.TenantId == tenantId && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderIds = orderEntities.Select(o => o.OrderID).ToList();
            var allOrderItems = await _context.OrderItems
                .Where(oi => orderIds.Contains(oi.OrderID))
                .ToListAsync();

            response.Orders = orderEntities.Select(o => new ClientDrillDownOrderDto
            {
                OrderId = o.OrderID,
                OrderNo = o.OrderNo ?? string.Empty,
                Date = o.OrderDate,
                TotalAmount = o.GrandTotal,
                Status = _context.OrderStatusMasters.Where(os => os.StatusID == o.StateID).Select(os=> os.StatusName).FirstOrDefault(),
                Items = allOrderItems.Where(i => i.OrderID == o.OrderID).Select(i => new ClientDrillDownItemDto
                {
                    ProductId = i.ProductID,
                    ProductName = productMap.ContainsKey(i.ProductID) ? productMap[i.ProductID] : "Unknown Product",
                    Description = i.Description ?? string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Total = i.LineTotal
                }).ToList()
            }).ToList();

            // 4. Invoices
            var invoiceStatusMap = await _context.InvoiceStatuses.ToDictionaryAsync(s => s.InvoiceStatusID, s => s.InvoiceStatusName);
            var clientIdStr = clientId.ToString();
            response.Invoices = await _context.Invoices
                .Where(i => i.ClientID == clientIdStr && !i.IsDeleted)
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new ClientDrillDownInvoiceDto
                {
                    InvoiceId = i.InvoiceID,
                    InvoiceNo = i.InvoiceNo ?? string.Empty,
                    Date = i.InvoiceDate,
                    TotalAmount = i.GrandTotal,
                    PaidAmount = i.PaidAmount,
                    BalanceAmount = i.RemainingPayment,
                    Status = invoiceStatusMap.ContainsKey(i.InvoiceStatusID) ? invoiceStatusMap[i.InvoiceStatusID] : "Pending",
                    DueDate = i.DueDate
                })
                .ToListAsync();

            return response;
        }
    }
}

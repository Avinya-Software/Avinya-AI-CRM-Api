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
    public class QuotationReportRepository : IQuotationReportRepository
    {
        private readonly AppDbContext _context;

        public QuotationReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<QuotationReportDto> GetQuotationReportAsync(QuotationReportFilterDto filter)
        {
            var today = DateTime.UtcNow;

            // ── Master lookups ────────────────────────────────────────────────────
            var statusMaster = await _context.QuotationStatusMaster
                .Where(s => s.IsActive)
                .ToDictionaryAsync(s => s.QuotationStatusID, s => s.StatusName);

            var clientMap = await _context.Clients
                .Where(c => !c.IsDeleted && c.TenantId == filter.TenantId)
                .ToDictionaryAsync(c => c.ClientID, c => c.CompanyName);

            var productMap = await _context.Products
                .Where(p => !p.IsDeleted && p.TenantId == filter.TenantId)
                .ToDictionaryAsync(p => p.ProductID, p => new { p.ProductName, p.Category });

            var userMap = await _context.Users
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            // ── Quotations base query ─────────────────────────────────────────────
            var quotQuery = _context.Quotations
                .Where(q => !q.IsDeleted && q.TenantId == filter.TenantId);

            if (filter.DateFrom.HasValue)
                quotQuery = quotQuery.Where(q => q.QuotationDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                quotQuery = quotQuery.Where(q => q.QuotationDate <= filter.DateTo.Value);
            if (filter.QuotationStatusId.HasValue)
                quotQuery = quotQuery.Where(q => q.QuotationStatusID == filter.QuotationStatusId.Value);
            if (filter.ClientId.HasValue)
                quotQuery = quotQuery.Where(q => q.ClientID == filter.ClientId.Value);
            if (!string.IsNullOrEmpty(filter.CreatedBy))
                quotQuery = quotQuery.Where(q => q.CreatedBy == filter.CreatedBy);
            if (filter.FirmId.HasValue)
                quotQuery = quotQuery.Where(q => q.FirmID == filter.FirmId.Value);

            var quotations = await quotQuery
                .Select(q => new
                {
                    q.QuotationID,
                    q.QuotationNo,
                    q.ClientID,
                    q.LeadID,
                    q.QuotationDate,
                    q.ValidTill,
                    q.TotalAmount,
                    q.Taxes,
                    q.GrandTotal,
                    q.QuotationStatusID,
                    q.RejectedNotes,
                    q.CreatedBy,
                    q.FirmID
                })
                .ToListAsync();

            var quotationIds = quotations.Select(q => q.QuotationID).ToList();

            // ── Quotation items for product breakdown ─────────────────────────────
            var quotItems = await _context.QuotationItems
                .Where(qi => quotationIds.Contains(qi.QuotationID))
                .Select(qi => new
                {
                    qi.QuotationItemID,
                    qi.QuotationID,
                    qi.ProductID,
                    qi.Quantity,
                    qi.UnitPrice,
                    qi.LineTotal
                })
                .ToListAsync();

            // ── Classify quotations ───────────────────────────────────────────────
            int total = quotations.Count;

            Func<Guid?, bool> isStatus = (id) =>
                id.HasValue && statusMaster.ContainsKey(id.Value)
                    ? true : false;

            string GetStatus(Guid? id) =>
                id.HasValue && statusMaster.TryGetValue(id.Value, out var sn) ? sn : string.Empty;

            int sent = quotations.Count(q => GetStatus(q.QuotationStatusID) == "Sent");
            int accepted = quotations.Count(q => GetStatus(q.QuotationStatusID) == "Accepted");
            int rejected = quotations.Count(q => GetStatus(q.QuotationStatusID) == "Rejected");
            int expired = quotations.Count(q =>
                q.ValidTill < today &&
                GetStatus(q.QuotationStatusID) != "Accepted" &&
                GetStatus(q.QuotationStatusID) != "Rejected");
            int pending = quotations.Count(q =>
                q.ValidTill >= today &&
                GetStatus(q.QuotationStatusID) == "Sent");

            decimal totalValue = quotations.Sum(q => q.GrandTotal);
            decimal acceptedValue = quotations
                .Where(q => GetStatus(q.QuotationStatusID) == "Accepted")
                .Sum(q => q.GrandTotal);
            decimal rejectedValue = quotations
                .Where(q => GetStatus(q.QuotationStatusID) == "Rejected")
                .Sum(q => q.GrandTotal);

            var kpi = new QuotationReportKpiDto
            {
                TotalQuotations = total,
                SentQuotations = sent,
                AcceptedQuotations = accepted,
                RejectedQuotations = rejected,
                ExpiredQuotations = expired,
                PendingQuotations = pending,
                AcceptanceRate = total > 0 ? Math.Round((double)accepted / total * 100, 1) : 0,
                RejectionRate = total > 0 ? Math.Round((double)rejected / total * 100, 1) : 0,
                TotalQuotedValue = totalValue,
                AcceptedValue = acceptedValue,
                RejectedValue = rejectedValue,
                AvgQuotationValue = total > 0 ? Math.Round(totalValue / total, 2) : 0
            };

            // ── Status breakdown ──────────────────────────────────────────────────
            var statusBreakdown = statusMaster.Select(sm =>
            {
                var group = quotations.Where(q => q.QuotationStatusID == sm.Key).ToList();
                return new QuotationStatusBreakdownDto
                {
                    StatusName = sm.Value,
                    Count = group.Count,
                    TotalValue = group.Sum(q => q.GrandTotal),
                    Percentage = total > 0
                        ? Math.Round((double)group.Count / total * 100, 1) : 0
                };
            })
            .OrderByDescending(s => s.Count)
            .ToList();

            // ── Product breakdown ─────────────────────────────────────────────────
            var productBreakdown = quotItems
                .GroupBy(qi => qi.ProductID)
                .Select(g =>
                {
                    var product = productMap.TryGetValue(g.Key, out var p)
                        ? p : null;

                    // Check how many of these quotations were accepted
                    var quotIdsForProduct = g.Select(qi => qi.QuotationID).Distinct().ToList();
                    int timesConverted = quotations
                        .Count(q => quotIdsForProduct.Contains(q.QuotationID) &&
                                    GetStatus(q.QuotationStatusID) == "Accepted");

                    int timesQuoted = quotIdsForProduct.Count;

                    return new QuotationProductBreakdownDto
                    {
                        ProductName = product?.ProductName ?? "Unknown",
                        Category = product?.Category ?? string.Empty,
                        TimesQuoted = timesQuoted,
                        TotalQuotedValue = g.Sum(qi => qi.LineTotal),
                        TimesConverted = timesConverted,
                        ConversionRate = timesQuoted > 0
                            ? Math.Round((double)timesConverted / timesQuoted * 100, 1) : 0
                    };
                })
                .OrderByDescending(p => p.TimesQuoted)
                .ToList();

            // ── Client summary ────────────────────────────────────────────────────
            var clientSummary = quotations
                .Where(q => q.ClientID.HasValue)
                .GroupBy(q => q.ClientID!.Value)
                .Select(g =>
                {
                    var grpList = g.ToList();
                    int cAccepted = grpList.Count(q => GetStatus(q.QuotationStatusID) == "Accepted");
                    int cRejected = grpList.Count(q => GetStatus(q.QuotationStatusID) == "Rejected");
                    decimal cTotal = grpList.Sum(q => q.GrandTotal);
                    decimal cAccValue = grpList
                        .Where(q => GetStatus(q.QuotationStatusID) == "Accepted")
                        .Sum(q => q.GrandTotal);

                    return new QuotationClientSummaryDto
                    {
                        ClientId = g.Key,
                        CompanyName = clientMap.TryGetValue(g.Key, out var cn) ? cn : "—",
                        TotalQuotations = grpList.Count,
                        AcceptedQuotations = cAccepted,
                        RejectedQuotations = cRejected,
                        TotalQuotedValue = cTotal,
                        AcceptedValue = cAccValue,
                        AcceptanceRate = grpList.Count > 0
                            ? Math.Round((double)cAccepted / grpList.Count * 100, 1) : 0
                    };
                })
                .OrderByDescending(c => c.TotalQuotedValue)
                .ToList();

            // ── Expiry list: expired + expiring in next 7 days ────────────────────
            var expiryList = quotations
                .Where(q =>
                    GetStatus(q.QuotationStatusID) != "Accepted" &&
                    GetStatus(q.QuotationStatusID) != "Rejected" &&
                    q.ValidTill <= today.AddDays(7))
                .Select(q =>
                {
                    int daysOverdue = (int)(today - q.ValidTill).TotalDays;
                    return new QuotationExpiryRowDto
                    {
                        QuotationNo = q.QuotationNo,
                        CompanyName = q.ClientID.HasValue && clientMap.ContainsKey(q.ClientID.Value)
                                        ? clientMap[q.ClientID.Value] : "—",
                        GrandTotal = q.GrandTotal,
                        ValidTill = q.ValidTill,
                        DaysOverdue = daysOverdue,
                        StatusName = GetStatus(q.QuotationStatusID),
                        CreatedBy = !string.IsNullOrEmpty(q.CreatedBy) && userMap.ContainsKey(q.CreatedBy)
                                        ? userMap[q.CreatedBy] : q.CreatedBy ?? "—"
                    };
                })
                .OrderByDescending(e => e.DaysOverdue)
                .ToList();

            // ── Rejection analysis ────────────────────────────────────────────────
            var rejectionList = quotations
                .Where(q => GetStatus(q.QuotationStatusID) == "Rejected")
                .Select(q => new QuotationRejectionRowDto
                {
                    QuotationNo = q.QuotationNo,
                    CompanyName = q.ClientID.HasValue && clientMap.ContainsKey(q.ClientID.Value)
                                        ? clientMap[q.ClientID.Value] : "—",
                    GrandTotal = q.GrandTotal,
                    RejectedNotes = q.RejectedNotes ?? string.Empty,
                    QuotationDate = q.QuotationDate,
                    CreatedBy = !string.IsNullOrEmpty(q.CreatedBy) && userMap.ContainsKey(q.CreatedBy)
                                        ? userMap[q.CreatedBy] : q.CreatedBy ?? "—"
                })
                .OrderByDescending(r => r.QuotationDate)
                .ToList();

            // ── Monthly trend ─────────────────────────────────────────────────────
            var monthlyTrend = quotations
                .GroupBy(q => new { q.QuotationDate.Year, q.QuotationDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g =>
                {
                    var list = g.ToList();
                    int mAccepted = list.Count(q => GetStatus(q.QuotationStatusID) == "Accepted");
                    int mRejected = list.Count(q => GetStatus(q.QuotationStatusID) == "Rejected");

                    return new QuotationMonthlyTrendDto
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        MonthName = new DateTime(g.Key.Year, g.Key.Month, 1)
                                           .ToString("MMM yyyy"),
                        TotalSent = list.Count,
                        Accepted = mAccepted,
                        Rejected = mRejected,
                        TotalValue = list.Sum(q => q.GrandTotal),
                        AcceptedValue = list
                            .Where(q => GetStatus(q.QuotationStatusID) == "Accepted")
                            .Sum(q => q.GrandTotal)
                    };
                })
                .ToList();

            return new QuotationReportDto
            {
                Kpi = kpi,
                StatusBreakdown = statusBreakdown,
                ProductBreakdown = productBreakdown,
                ClientSummary = clientSummary,
                ExpiryList = expiryList,
                RejectionList = rejectionList,
                MonthlyTrend = monthlyTrend,
                AppliedFilters = filter
            };
        }

        public async Task<List<QuotationLifecycleReportDto>> GetQuotationLifecycleReportAsync(QuotationReportFilterDto filter)
        {
            // 1. Base Query
            var quotQuery = _context.Quotations
                .Where(q => !q.IsDeleted && q.TenantId == filter.TenantId);

            if (filter.DateFrom.HasValue)
                quotQuery = quotQuery.Where(q => q.QuotationDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                quotQuery = quotQuery.Where(q => q.QuotationDate <= filter.DateTo.Value);
            if (filter.QuotationStatusId.HasValue)
                quotQuery = quotQuery.Where(q => q.QuotationStatusID == filter.QuotationStatusId.Value);
            if (filter.ClientId.HasValue)
                quotQuery = quotQuery.Where(q => q.ClientID == filter.ClientId.Value);
            if (!string.IsNullOrEmpty(filter.CreatedBy))
                quotQuery = quotQuery.Where(q => q.CreatedBy == filter.CreatedBy);

            // 2. Fetch Paginated Quotations
            var quots = await quotQuery
                .OrderByDescending(q => q.QuotationDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            if (!quots.Any()) return new List<QuotationLifecycleReportDto>();

            var quotIds = quots.Select(q => q.QuotationID).ToList();

            // 3. Round-trip Fetching (Pre-fetch for efficiency)
            var clientMap = await _context.Clients
                .Where(c => !c.IsDeleted && c.TenantId == filter.TenantId)
                .ToDictionaryAsync(c => c.ClientID, c => c.CompanyName);

            var statusMap = await _context.QuotationStatusMaster
                .ToDictionaryAsync(s => s.QuotationStatusID, s => s.StatusName);

            var productMap = await _context.Products
                .Where(p => !p.IsDeleted && p.TenantId == filter.TenantId)
                .ToDictionaryAsync(p => p.ProductID, p => p.ProductName);

            var userMap = await _context.Users
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            var quotItems = await _context.QuotationItems
                .Where(qi => quotIds.Contains(qi.QuotationID))
                .ToListAsync();

            var orders = await _context.Orders
                .Where(o => !o.IsDeleted && o.QuotationID.HasValue && quotIds.Contains(o.QuotationID.Value))
                .ToListAsync();

            var orderStatusMap = await _context.OrderStatusMasters
                .ToDictionaryAsync(s => s.StatusID, s => s.StatusName);

            var orderIdsStr = orders.Select(o => o.OrderID.ToString()).ToList();
            var invoices = await _context.Invoices
                .Where(i => !i.IsDeleted && orderIdsStr.Contains(i.OrderID))
                .ToListAsync();

            var invoiceStatusMap = await _context.InvoiceStatuses
                .ToDictionaryAsync(s => s.InvoiceStatusID, s => s.InvoiceStatusName);

            // 4. Mapping
            var result = quots.Select(q => new QuotationLifecycleReportDto
            {
                QuotationID = q.QuotationID,
                QuotationNo = q.QuotationNo ?? "—",
                QuotationDate = q.QuotationDate,
                ClientName = q.ClientID.HasValue && clientMap.ContainsKey(q.ClientID.Value) ? clientMap[q.ClientID.Value] : "—",
                StatusName = statusMap.ContainsKey(q.QuotationStatusID) ? statusMap[q.QuotationStatusID] : "—",
                GrandTotal = q.GrandTotal,
                CreatedBy = !string.IsNullOrEmpty(q.CreatedBy) && userMap.ContainsKey(q.CreatedBy) ? userMap[q.CreatedBy] : "—",
                Items = quotItems.Where(i => i.QuotationID == q.QuotationID).Select(i => new QuotationLifecycleItemDto
                {
                    ProductName = productMap.ContainsKey(i.ProductID) ? productMap[i.ProductID] : "Unknown",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList(),
                Orders = orders.Where(o => o.QuotationID == q.QuotationID).Select(o => new QuotationLifecycleOrderDto
                {
                    OrderID = o.OrderID,
                    OrderNo = o.OrderNo ?? "—",
                    OrderDate = o.OrderDate,
                    GrandTotal = o.GrandTotal,
                    StatusName = orderStatusMap.ContainsKey(o.Status) ? orderStatusMap[o.Status] : "Confirmed",
                    Invoices = invoices.Where(i => i.OrderID == o.OrderID.ToString()).Select(i => new QuotationLifecycleInvoiceDto
                    {
                        InvoiceID = i.InvoiceID,
                        InvoiceNo = i.InvoiceNo ?? "—",
                        InvoiceDate = i.InvoiceDate,
                        GrandTotal = i.GrandTotal,
                        PaidAmount = i.PaidAmount,
                        RemainingPayment = i.RemainingPayment,
                        StatusName = invoiceStatusMap.ContainsKey(i.InvoiceStatusID) ? invoiceStatusMap[i.InvoiceStatusID] : "—"
                    }).ToList()
                }).ToList()
            }).ToList();

            return result;
        }
    }
}

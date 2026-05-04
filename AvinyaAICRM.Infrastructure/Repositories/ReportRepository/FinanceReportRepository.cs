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
    public class FinanceReportRepository : IFinanceReportRepository
    {
        private readonly AppDbContext _context;

        public FinanceReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FinanceReportDto> GetFinanceReportAsync(FinanceReportFilterDto filter)
        {
            var today = DateTime.Now;

            // ── Master lookups ────────────────────────────────────────────────────
            var invoiceStatusMap = await _context.InvoiceStatuses
                .ToDictionaryAsync(s => s.InvoiceStatusID, s => s.InvoiceStatusName);

            var clientMap = await _context.Clients
                .Where(c => !c.IsDeleted && c.TenantId == filter.TenantId && c.IsCustomer)
                .ToDictionaryAsync(c => c.ClientID.ToString(), c => c.CompanyName);

            var expenseCategoryMap = await _context.ExpenseCategories
                .Where(ec => !ec.IsDeleted && ec.IsActive)
                .ToDictionaryAsync(ec => ec.CategoryId, ec => ec.CategoryName);

            var userMap = await _context.Users
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            // ════════════════════════════════════════════════════════════════════
            // SECTION B — INVOICES
            // ════════════════════════════════════════════════════════════════════

            var invoiceQuery = _context.Invoices
                .Where(i => !i.IsDeleted &&
                            i.TenantId == filter.TenantId.ToString());

            if (filter.DateFrom.HasValue)
                invoiceQuery = invoiceQuery.Where(i => i.InvoiceDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                invoiceQuery = invoiceQuery.Where(i => i.InvoiceDate <= filter.DateTo.Value);
            if (filter.InvoiceStatusId.HasValue)
                invoiceQuery = invoiceQuery.Where(i => i.InvoiceStatusID == filter.InvoiceStatusId.Value);
            if (filter.ClientId.HasValue)
                invoiceQuery = invoiceQuery.Where(i => i.ClientID == filter.ClientId.Value.ToString());
            if (filter.OverdueOnly)
                invoiceQuery = invoiceQuery.Where(i =>
                    i.AmountAfterDiscount > 0 &&
                    i.DueDate.HasValue &&
                    i.DueDate.Value < today);

            var invoices = await invoiceQuery
                .Select(i => new
                {
                    i.InvoiceID,
                    i.InvoiceNo,
                    i.ClientID,
                    i.InvoiceDate,
                    i.SubTotal,
                    i.Taxes,
                    i.Discount,
                    i.GrandTotal,
                    i.PaidAmount,
                    i.RemainingPayment,
                    i.AmountAfterDiscount,
                    i.InvoiceStatusID,
                    i.DueDate,
                    i.OrderID
                })
                .ToListAsync();

            // ── Payments ──────────────────────────────────────────────────────────
            var invoiceIds = invoices.Select(i => i.InvoiceID).ToList();

            var paymentQuery = _context.Payments
                .Where(p => invoiceIds.Contains(p.InvoiceID));

            if (filter.DateFrom.HasValue)
                paymentQuery = paymentQuery.Where(p => p.PaymentDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                paymentQuery = paymentQuery.Where(p => p.PaymentDate <= filter.DateTo.Value);
            if (!string.IsNullOrEmpty(filter.PaymentMode))
                paymentQuery = paymentQuery.Where(p => p.PaymentMode == filter.PaymentMode);

            var payments = await paymentQuery
                .Select(p => new
                {
                    p.PaymentID,
                    p.InvoiceID,
                    p.PaymentDate,
                    p.Amount,
                    p.PaymentMode,
                    p.TransactionRef,
                    p.ReceivedBy
                })
                .ToListAsync();

            // ════════════════════════════════════════════════════════════════════
            // SECTION D — EXPENSES
            // ════════════════════════════════════════════════════════════════════

            var expenseQuery = _context.Expenses
                .Where(e => !e.IsDeleted && e.TenantId == filter.TenantId);

            if (filter.DateFrom.HasValue)
                expenseQuery = expenseQuery.Where(e => e.ExpenseDate >= DateOnly.FromDateTime(filter.DateFrom.Value).ToDateTime(TimeOnly.MinValue));
            if (filter.DateTo.HasValue)
                expenseQuery = expenseQuery.Where(e => e.ExpenseDate <= DateOnly.FromDateTime(filter.DateTo.Value).ToDateTime(TimeOnly.MinValue));
            if (filter.ExpenseCategoryId.HasValue)
                expenseQuery = expenseQuery.Where(e => e.CategoryId == filter.ExpenseCategoryId.Value);
            if (!string.IsNullOrEmpty(filter.PaymentMode))
                expenseQuery = expenseQuery.Where(e => e.PaymentMode == filter.PaymentMode);

            var expenses = await expenseQuery
                .Select(e => new
                {
                    e.ExpenseId,
                    e.ExpenseDate,
                    e.CategoryId,
                    e.Amount,
                    e.PaymentMode,
                    e.Description,
                    e.Status,
                    e.CreatedBy,
                    e.CreatedDate
                })
                .ToListAsync();

            // ════════════════════════════════════════════════════════════════════
            // SECTION A — COMBINED SUMMARY KPI
            // ════════════════════════════════════════════════════════════════════

            decimal totalInvoiced = invoices.Sum(i => i.GrandTotal);
            decimal totalCollected = payments.Sum(p => p.Amount);
            decimal totalOutstanding = invoices.Sum(i => i.AmountAfterDiscount);
            decimal totalExpenses = expenses.Sum(e => e.Amount);
            decimal netPosition = totalCollected - totalExpenses;

            decimal totalOverdue = invoices
                .Where(i => i.AmountAfterDiscount > 0 &&
                            i.DueDate.HasValue &&
                            i.DueDate.Value < today)
                .Sum(i => i.AmountAfterDiscount);

            string GetInvoiceStatus(int id) =>
                invoiceStatusMap.TryGetValue(id, out var s) ? s : string.Empty;

            int paidCount = invoices.Count(i => GetInvoiceStatus(i.InvoiceStatusID) == "Paid");
            int partialCount = invoices.Count(i => GetInvoiceStatus(i.InvoiceStatusID) == "Partial");
            int unpaidCount = invoices.Count(i => GetInvoiceStatus(i.InvoiceStatusID) == "Unpaid");
            int overdueCount = invoices.Count(i =>
                i.AmountAfterDiscount > 0 &&
                i.DueDate.HasValue &&
                i.DueDate.Value < today);

            var summary = new FinanceSummaryKpiDto
            {
                TotalInvoiced = totalInvoiced,
                TotalCollected = totalCollected,
                TotalOutstanding = totalOutstanding,
                TotalOverdue = totalOverdue,
                CollectionRate = totalInvoiced > 0 ? Math.Round((double)(totalCollected / totalInvoiced) * 100, 1) : 0,
                TotalExpenses = totalExpenses,
                NetPosition = netPosition,
                GrossProfitMargin = totalCollected > 0 ? Math.Round((netPosition / totalCollected) * 100m, 1) : 0,
                TotalInvoices = invoices.Count,
                PaidInvoices = paidCount,
                PartialInvoices = partialCount,
                UnpaidInvoices = unpaidCount,
                OverdueInvoices = overdueCount,
                TotalPaymentRecords = payments.Count,
                TotalExpenseRecords = expenses.Count
            };

            // ════════════════════════════════════════════════════════════════════
            // SECTION B — INVOICE BREAKDOWNS
            // ════════════════════════════════════════════════════════════════════

            // Status breakdown
            var invoiceStatusBreakdown = invoiceStatusMap.Select(sm =>
            {
                var group = invoices.Where(i => i.InvoiceStatusID == sm.Key).ToList();
                return new InvoiceStatusBreakdownDto
                {
                    StatusName = sm.Value,
                    Count = group.Count,
                    TotalValue = group.Sum(i => i.GrandTotal),
                    TotalPaid = group.Sum(i => i.PaidAmount),
                    TotalOutstanding = group.Sum(i => i.AmountAfterDiscount),
                    Percentage = invoices.Count > 0
                        ? Math.Round((double)group.Count / invoices.Count * 100, 1) : 0
                };
            })
            .OrderByDescending(s => s.Count)
            .ToList();

            // Aging buckets — based on DueDate
            var agingBuckets = new List<(string Label, int MinDays, int MaxDays)>
            {
                ("0–15 days",   0,  15),
                ("16–30 days", 16,  30),
                ("31–60 days", 31,  60),
                ("> 60 days",  61, int.MaxValue)
            };

            var overdueInvoices = invoices
                .Where(i => i.AmountAfterDiscount > 0 &&
                            i.DueDate.HasValue &&
                            i.DueDate.Value < today)
                .ToList();

            decimal totalOutstandingOverdue = overdueInvoices.Sum(i => i.AmountAfterDiscount);

            var invoiceAging = agingBuckets.Select(bucket =>
            {
                var group = overdueInvoices.Where(i =>
                {
                    int days = (int)(today - i.DueDate!.Value).TotalDays;
                    return days >= bucket.MinDays &&
                           (bucket.MaxDays == int.MaxValue || days <= bucket.MaxDays);
                }).ToList();

                decimal bucketOutstanding = group.Sum(i => i.AmountAfterDiscount);
                return new InvoiceAgingBucketDto
                {
                    Bucket = bucket.Label,
                    Count = group.Count,
                    Outstanding = bucketOutstanding,
                    Percentage = totalOutstandingOverdue > 0
                        ? Math.Round((double)(bucketOutstanding / totalOutstandingOverdue) * 100, 1) : 0
                };
            }).ToList();

            // Client outstanding
            var clientOutstanding = invoices
                .Where(i => !string.IsNullOrEmpty(i.ClientID))
                .GroupBy(i => i.ClientID!)
                .Select(g =>
                {
                    var list = g.ToList();
                    decimal cOverdue = list
                        .Where(i => i.AmountAfterDiscount > 0 &&
                                    i.DueDate.HasValue &&
                                    i.DueDate.Value < today)
                        .Sum(i => i.AmountAfterDiscount);

                    Guid.TryParse(g.Key, out var clientGuid);
                    return new InvoiceClientOutstandingDto
                    {
                        ClientId = clientGuid,
                        CompanyName = clientMap.TryGetValue(g.Key, out var cn) ? cn : "—",
                        TotalInvoices = list.Count,
                        TotalInvoiced = list.Sum(i => i.GrandTotal),
                        TotalPaid = list.Sum(i => i.PaidAmount),
                        Outstanding = list.Sum(i => i.AmountAfterDiscount),
                        Overdue = cOverdue
                    };
                })
                .OrderByDescending(c => c.Outstanding)
                .ToList();

            // Overdue invoice detail list
            var overdueInvoiceList = overdueInvoices
                .Select(i => new InvoiceOverdueRowDto
                {
                    InvoiceNo = i.InvoiceNo,
                    CompanyName = clientMap.TryGetValue(i.ClientID ?? string.Empty, out var cn) ? cn : "—",
                    GrandTotal = i.GrandTotal,
                    Outstanding = i.AmountAfterDiscount,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate!.Value,
                    DaysOverdue = (int)(today - i.DueDate!.Value).TotalDays,
                    StatusName = GetInvoiceStatus(i.InvoiceStatusID)
                })
                .OrderByDescending(o => o.DaysOverdue)
                .ToList();

            // ════════════════════════════════════════════════════════════════════
            // SECTION C — PAYMENT BREAKDOWNS
            // ════════════════════════════════════════════════════════════════════

            // Payment mode breakdown
            var paymentModeBreakdown = payments
                .GroupBy(p => p.PaymentMode)
                .Select(g =>
                {
                    decimal amt = g.Sum(p => p.Amount);
                    return new PaymentModeBreakdownDto
                    {
                        PaymentMode = g.Key,
                        Count = g.Count(),
                        TotalAmount = amt,
                        Percentage = totalCollected > 0
                            ? Math.Round((double)(amt / totalCollected) * 100, 1) : 0
                    };
                })
                .OrderByDescending(p => p.TotalAmount)
                .ToList();

            // Recent payments — latest 20
            var invoiceNoMap = invoices.ToDictionary(i => i.InvoiceID, i => i.InvoiceNo);

            var recentPayments = payments
                .OrderByDescending(p => p.PaymentDate)
                .Take(20)
                .Select(p =>
                {
                    var invoiceNo = invoiceNoMap.TryGetValue(p.InvoiceID, out var no) ? no : "—";
                    var inv = invoices.FirstOrDefault(i => i.InvoiceID == p.InvoiceID);
                    var clientName = inv != null && clientMap.ContainsKey(inv.ClientID ?? string.Empty)
                        ? clientMap[inv.ClientID!] : "—";

                    return new RecentPaymentRowDto
                    {
                        InvoiceNo = invoiceNo,
                        CompanyName = clientName,
                        Amount = p.Amount,
                        PaymentMode = p.PaymentMode,
                        TransactionRef = p.TransactionRef ?? string.Empty,
                        ReceivedBy = userMap.TryGetValue(p.ReceivedBy, out var fn) ? fn : p.ReceivedBy,
                        PaymentDate = p.PaymentDate
                    };
                })
                .ToList();

            // ════════════════════════════════════════════════════════════════════
            // SECTION D — EXPENSE BREAKDOWNS
            // ════════════════════════════════════════════════════════════════════

            // Category breakdown
            var expenseCategoryBreakdown = expenses
                .GroupBy(e => e.CategoryId)
                .Select(g =>
                {
                    decimal amt = g.Sum(e => e.Amount);
                    return new ExpenseCategoryBreakdownDto
                    {
                        CategoryName = expenseCategoryMap.TryGetValue(g.Key, out var cn) ? cn : "Unknown",
                        Count = g.Count(),
                        TotalAmount = amt,
                        Percentage = totalExpenses > 0
                            ? Math.Round((double)(amt / totalExpenses) * 100, 1) : 0
                    };
                })
                .OrderByDescending(e => e.TotalAmount)
                .ToList();

            // Expense payment mode breakdown
            var expensePaymentBreakdown = expenses
                .Where(e => !string.IsNullOrEmpty(e.PaymentMode))
                .GroupBy(e => e.PaymentMode!)
                .Select(g =>
                {
                    decimal amt = g.Sum(e => e.Amount);
                    return new ExpensePaymentModeBreakdownDto
                    {
                        PaymentMode = g.Key,
                        Count = g.Count(),
                        TotalAmount = amt,
                        Percentage = totalExpenses > 0
                            ? Math.Round((double)(amt / totalExpenses) * 100, 1) : 0
                    };
                })
                .OrderByDescending(e => e.TotalAmount)
                .ToList();

            // Expense monthly trend
            var expenseMonthlyTrend = expenses
                .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new ExpenseMonthlyTrendDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    TotalAmount = g.Sum(e => e.Amount),
                    Count = g.Count()
                })
                .ToList();

            // Top 20 individual expenses by amount
            var topExpenses = expenses
                .OrderByDescending(e => e.Amount)
                .Take(20)
                .Select(e => new TopExpenseRowDto
                {
                    CategoryName = expenseCategoryMap.TryGetValue(e.CategoryId, out var cn) ? cn : "Unknown",
                    Description = e.Description ?? string.Empty,
                    Amount = e.Amount,
                    PaymentMode = e.PaymentMode ?? string.Empty,
                    AddedBy = userMap.TryGetValue(e.CreatedBy.ToString(), out var fn) ? fn : "—",
                    ExpenseDate = e.ExpenseDate,
                    Status = e.Status
                })
                .ToList();

            // ════════════════════════════════════════════════════════════════════
            // COMBINED MONTHLY TREND
            // ════════════════════════════════════════════════════════════════════

            // Build a unified month list across invoices, payments, and expenses
            var allMonths = invoices.Select(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Union(payments.Select(p => new { p.PaymentDate.Year, p.PaymentDate.Month }))
                .Union(expenses.Select(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month }))
                .Distinct()
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            var monthlyTrend = allMonths.Select(m =>
            {
                decimal mInvoiced = invoices
                    .Where(i => i.InvoiceDate.Year == m.Year && i.InvoiceDate.Month == m.Month)
                    .Sum(i => i.GrandTotal);

                decimal mCollected = payments
                    .Where(p => p.PaymentDate.Year == m.Year && p.PaymentDate.Month == m.Month)
                    .Sum(p => p.Amount);

                decimal mExpenses = expenses
                    .Where(e => e.ExpenseDate.Year == m.Year && e.ExpenseDate.Month == m.Month)
                    .Sum(e => e.Amount);

                return new PaymentMonthlyTrendDto
                {
                    Year = m.Year,
                    Month = m.Month,
                    MonthName = new DateTime(m.Year, m.Month, 1).ToString("MMM yyyy"),
                    TotalInvoiced = mInvoiced,
                    TotalCollected = mCollected,
                    TotalExpenses = mExpenses,
                    NetPosition = mCollected - mExpenses
                };
            }).ToList();

            return new FinanceReportDto
            {
                Summary = summary,
                InvoiceStatusBreakdown = invoiceStatusBreakdown,
                InvoiceAging = invoiceAging,
                ClientOutstanding = clientOutstanding,
                OverdueInvoices = overdueInvoiceList,
                PaymentModeBreakdown = paymentModeBreakdown,
                RecentPayments = recentPayments,
                ExpenseCategoryBreakdown = expenseCategoryBreakdown,
                ExpensePaymentBreakdown = expensePaymentBreakdown,
                ExpenseMonthlyTrend = expenseMonthlyTrend,
                TopExpenses = topExpenses,
                MonthlyTrend = monthlyTrend,
                AppliedFilters = filter
            };
        }
    }
}

using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Repositories.ReportRepository
{
    public class OrderReportRepository : IOrderReportRepository
    {
        private readonly AppDbContext _context;

        public OrderReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderReportDto> GetOrderReportAsync(OrderReportFilterDto filter)
        {
            var today = DateTime.Now;

            // ── Master lookups ────────────────────────────────────────────────────
            var orderStatusMap = await _context.OrderStatusMasters
                .ToDictionaryAsync(s => s.StatusID, s => s.StatusName);


            var clientMap = await _context.Clients
                .Where(c => !c.IsDeleted && c.TenantId == filter.TenantId && c.IsCustomer)
                .ToDictionaryAsync(c => c.ClientID, c => c.CompanyName);

            var productMap = await _context.Products
                .Where(p => !p.IsDeleted && p.TenantId == filter.TenantId)
                .ToDictionaryAsync(p => p.ProductID, p => new { p.ProductName, p.Category });

            var userMap = await _context.Users
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            // ── Orders base query ─────────────────────────────────────────────────
            var ordersQuery = _context.Orders
                .Where(o => !o.IsDeleted && o.TenantId == filter.TenantId);

            if (filter.DateFrom.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= filter.DateTo.Value);
            if (filter.OrderStatusId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.Status == filter.OrderStatusId.Value);
            if (filter.DesignStatusId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.DesignStatusID == filter.DesignStatusId.Value);
            if (filter.ClientId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.ClientID == filter.ClientId.Value);
            if (!string.IsNullOrEmpty(filter.AssignedDesignTo))
                ordersQuery = ordersQuery.Where(o => o.AssignedDesignTo == filter.AssignedDesignTo);
            if (filter.FirmId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.FirmID == filter.FirmId.Value);
            if (filter.OverdueOnly)
                ordersQuery = ordersQuery.Where(o =>
                    o.ExpectedDeliveryDate.HasValue &&
                    o.ExpectedDeliveryDate.Value < today &&
                    o.Status != 5); // 5 = Delivered — adjust to your StatusID

            var orders = await ordersQuery
                .Select(o => new
                {
                    o.OrderID,
                    o.OrderNo,
                    o.ClientID,
                    o.OrderDate,
                    o.ExpectedDeliveryDate,
                    o.Status,
                    o.SubTotal,
                    o.TotalTaxes,
                    o.GrandTotal,
                    o.IsDeleted,
                    o.isInvoiceCreated,
                    o.FirmID
                })
                .ToListAsync();

            var orderIds = orders.Select(o => o.OrderID).ToList();

            // ── Order items ───────────────────────────────────────────────────────
            var orderItems = await _context.OrderItems
                .Where(oi => orderIds.Contains(oi.OrderID))
                .Select(oi => new
                {
                    oi.OrderItemID,
                    oi.OrderID,
                    oi.ProductID,
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.LineTotal
                })
                .ToListAsync();

            // ── Helpers ───────────────────────────────────────────────────────────
            string GetOrderStatus(int statusId) =>
                orderStatusMap.TryGetValue(statusId, out var s) ? s : string.Empty;

            bool IsDelivered(int statusId) => GetOrderStatus(statusId) == "Delivered";

            bool IsOverdue(DateTime? expDate, int statusId) =>
                expDate.HasValue && expDate.Value < today && !IsDelivered(statusId);

            // ── KPIs ──────────────────────────────────────────────────────────────
            int total = orders.Count;
            int delivered = orders.Count(o => IsDelivered(o.Status));
            int overdue = orders.Count(o => IsOverdue(o.ExpectedDeliveryDate, o.Status));
            int pending = orders.Count(o => GetOrderStatus(o.Status) == "Pending");
            int inProgress = orders.Count(o => GetOrderStatus(o.Status) == "In Progress");
            int ready = orders.Count(o => GetOrderStatus(o.Status) == "Ready");

            decimal totalValue = orders.Sum(o => o.GrandTotal);
            decimal invoicedVal = orders.Where(o => o.isInvoiceCreated == true).Sum(o => o.GrandTotal);
            decimal pendingInvVal = orders.Where(o => o.isInvoiceCreated != true).Sum(o => o.GrandTotal);

            // On-time delivery: delivered orders where delivery happened before or on ExpectedDeliveryDate
            // We don't have an actual delivery date column — proxy: if Status = Delivered and no overdue flag
            // In a real system you'd track ActualDeliveryDate; for now we count non-overdue delivered orders
            int onTimeDelivered = orders.Count(o =>
                IsDelivered(o.Status) &&
                (!o.ExpectedDeliveryDate.HasValue || o.ExpectedDeliveryDate.Value >= o.OrderDate));

            // Avg days to deliver for delivered orders that have an ExpectedDeliveryDate
            double avgDaysToDeliver = 0;
            var deliveredWithDates = orders
                .Where(o => IsDelivered(o.Status) && o.ExpectedDeliveryDate.HasValue)
                .ToList();
            if (deliveredWithDates.Any())
            {
                avgDaysToDeliver = Math.Round(
                    deliveredWithDates.Average(o =>
                        (o.ExpectedDeliveryDate!.Value - o.OrderDate).TotalDays), 1);
            }

            var kpi = new OrderReportKpiDto
            {
                TotalOrders = total,
                PendingOrders = pending,
                InProgressOrders = inProgress,
                ReadyOrders = ready,
                DeliveredOrders = delivered,
                OverdueOrders = overdue,
                DeliveryRate = total > 0 ? Math.Round((double)delivered / total * 100, 1) : 0,
                OnTimeDeliveryRate = delivered > 0 ? Math.Round((double)onTimeDelivered / delivered * 100, 1) : 0,
                TotalOrderValue = totalValue,
                TotalInvoicedValue = invoicedVal,
                PendingInvoiceValue = pendingInvVal,
                AvgOrderValue = total > 0 ? Math.Round(totalValue / total, 2) : 0,
                AvgDaysToDeliver = avgDaysToDeliver
            };

            // ── Order status breakdown ────────────────────────────────────────────
            var statusBreakdown = orderStatusMap.Select(sm =>
            {
                var group = orders.Where(o => o.Status == sm.Key).ToList();
                return new OrderStatusBreakdownDto
                {
                    StatusName = sm.Value,
                    Count = group.Count,
                    TotalValue = group.Sum(o => o.GrandTotal),
                    Percentage = total > 0 ? Math.Round((double)group.Count / total * 100, 1) : 0
                };
            })
            .OrderByDescending(s => s.Count)
            .ToList();

            // ── Product breakdown ─────────────────────────────────────────────────
            var productBreakdown = orderItems
                .GroupBy(oi => oi.ProductID)
                .Select(g =>
                {
                    var prod = productMap.TryGetValue(g.Key, out var p) ? p : null;
                    decimal lineTotal = g.Sum(oi => oi.LineTotal);

                    return new OrderProductBreakdownDto
                    {
                        ProductName = prod?.ProductName ?? "Unknown",
                        Category = prod?.Category ?? string.Empty,
                        TotalOrders = g.Select(oi => oi.OrderID).Distinct().Count(),
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        TotalRevenue = lineTotal,
                        RevenueShare = totalValue > 0 ? Math.Round((lineTotal / totalValue) * 100m, 1) : 0m
                    };
                })
                .OrderByDescending(p => p.TotalRevenue)
                .ToList();

            // ── Client summary ────────────────────────────────────────────────────
            var clientSummary = orders
                .GroupBy(o => o.ClientID)
                .Select(g =>
                {
                    var list = g.ToList();
                    return new OrderClientSummaryDto
                    {
                        ClientId = g.Key,
                        CompanyName = clientMap.TryGetValue(g.Key, out var cn) ? cn : "—",
                        TotalOrders = list.Count,
                        DeliveredOrders = list.Count(o => IsDelivered(o.Status)),
                        OverdueOrders = list.Count(o => IsOverdue(o.ExpectedDeliveryDate, o.Status)),
                        TotalValue = list.Sum(o => o.GrandTotal),
                        IsInvoiceCreated = list.All(o => o.isInvoiceCreated == true)
                    };
                })
                .OrderByDescending(c => c.TotalValue)
                .ToList();

            // ── Overdue list ──────────────────────────────────────────────────────
            var overdueList = orders
                .Where(o => IsOverdue(o.ExpectedDeliveryDate, o.Status))
                .Select(o => new OrderOverdueRowDto
                {
                    OrderNo = o.OrderNo,
                    CompanyName = clientMap.TryGetValue(o.ClientID, out var cn) ? cn : "—",
                    GrandTotal = o.GrandTotal,
                    OrderDate = o.OrderDate,
                    ExpectedDeliveryDate = o.ExpectedDeliveryDate!.Value,
                    DaysOverdue = (int)(today - o.ExpectedDeliveryDate!.Value).TotalDays,
                    OrderStatus = GetOrderStatus(o.Status),
                    IsInvoiceCreated = o.isInvoiceCreated
                })
                .OrderByDescending(o => o.DaysOverdue)
                .ToList();

            // ── Pending invoice list ──────────────────────────────────────────────
            var pendingInvoiceList = orders
                .Where(o => o.isInvoiceCreated != true)
                .Select(o => new OrderPendingInvoiceRowDto
                {
                    OrderNo = o.OrderNo,
                    CompanyName = clientMap.TryGetValue(o.ClientID, out var cn) ? cn : "—",
                    GrandTotal = o.GrandTotal,
                    OrderDate = o.OrderDate,
                    OrderStatus = GetOrderStatus(o.Status),
                    DaysSinceOrder = (int)(today - o.OrderDate).TotalDays
                })
                .OrderByDescending(o => o.DaysSinceOrder)
                .ToList();

            // ── Monthly trend ─────────────────────────────────────────────────────
            var monthlyTrend = orders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g =>
                {
                    var list = g.ToList();
                    return new OrderMonthlyTrendDto
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        TotalOrders = list.Count,
                        Delivered = list.Count(o => IsDelivered(o.Status)),
                        Overdue = list.Count(o => IsOverdue(o.ExpectedDeliveryDate, o.Status)),
                        TotalValue = list.Sum(o => o.GrandTotal)
                    };
                })
                .ToList();

            return new OrderReportDto
            {
                Kpi = kpi,
                StatusBreakdown = statusBreakdown,
                ProductBreakdown = productBreakdown,
                ClientSummary = clientSummary,
                OverdueList = overdueList,
                PendingInvoiceList = pendingInvoiceList,
                MonthlyTrend = monthlyTrend,
                AppliedFilters = filter
            };
        }

        public async Task<PagedResult<OrderLifecycleReportDto>> GetOrderLifecycleReportAsync(OrderReportFilterDto filter)
        {
            var ordersQuery = _context.Orders
                .Where(o => !o.IsDeleted && o.TenantId == filter.TenantId);

            if (filter.DateFrom.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= filter.DateTo.Value);
            if (filter.OrderStatusId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.Status == filter.OrderStatusId.Value);
            if (filter.DesignStatusId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.DesignStatusID == filter.DesignStatusId.Value);
            if (filter.ClientId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.ClientID == filter.ClientId.Value);
            if (!string.IsNullOrEmpty(filter.AssignedDesignTo))
                ordersQuery = ordersQuery.Where(o => o.AssignedDesignTo == filter.AssignedDesignTo);
            if (filter.FirmId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.FirmID == filter.FirmId.Value);

            var totalRecords = await ordersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalRecords / filter.PageSize);

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var orderIds = orders.Select(o => o.OrderID).ToList();
            var orderIdStrings = orderIds.Select(id => id.ToString()).ToList();

            var orderItems = await _context.OrderItems
                .Where(oi => orderIds.Contains(oi.OrderID))
                .Include(oi => oi.Product)
                .ToListAsync();

            var invoices = await _context.Invoices
                .Where(i => !i.IsDeleted && orderIdStrings.Contains(i.OrderID))
                .ToListAsync();

            var invoiceIds = invoices.Select(i => i.InvoiceID).ToList();

            var payments = await _context.Payments
                .Where(p => invoiceIds.Contains(p.InvoiceID))
                .ToListAsync();

            // Masters
            var orderStatusMap = await _context.OrderStatusMasters.ToDictionaryAsync(s => s.StatusID, s => s.StatusName);
            var invoiceStatusMap = await _context.InvoiceStatuses.ToDictionaryAsync(s => s.InvoiceStatusID, s => s.InvoiceStatusName);
            var clientMap = await _context.Clients.Where(c => !c.IsDeleted && c.IsCustomer).ToDictionaryAsync(c => c.ClientID, c => c.CompanyName);

            var reportData = orders.Select(o => new OrderLifecycleReportDto
            {
                OrderID = o.OrderID,
                OrderNo = o.OrderNo,
                OrderDate = o.OrderDate,
                GrandTotal = o.GrandTotal,
                ClientName = clientMap.TryGetValue(o.ClientID, out var cn) ? cn : "—",
                StatusName = orderStatusMap.TryGetValue(o.Status, out var sn) ? sn : "—",

                Items = orderItems.Where(oi => oi.OrderID == o.OrderID).Select(oi => new LifecycleOrderItemDto
                {
                    ProductID = oi.ProductID,
                    ProductName = oi.Product?.ProductName ?? "—",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    LineTotal = oi.LineTotal
                }).ToList(),

                Invoices = invoices.Where(i => i.OrderID == o.OrderID.ToString()).Select(i => new OrderInvoiceDto
                {
                    InvoiceID = i.InvoiceID,
                    InvoiceNo = i.InvoiceNo,
                    InvoiceDate = i.InvoiceDate,
                    GrandTotal = i.GrandTotal,
                    PaidAmount = i.PaidAmount,
                    StatusName = invoiceStatusMap.TryGetValue(i.InvoiceStatusID, out var isn) ? isn : "—"
                }).ToList(),

                Payments = payments.Where(p => invoices.Any(i => i.InvoiceID == p.InvoiceID && i.OrderID == o.OrderID.ToString())).Select(p => new OrderPaymentDto
                {
                    PaymentID = p.PaymentID,
                    InvoiceID = p.InvoiceID,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    PaymentMode = p.PaymentMode,
                    TransactionRef = p.TransactionRef
                }).ToList()
            }).ToList();

            return new PagedResult<OrderLifecycleReportDto>
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

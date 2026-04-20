using AvinyaAICRM.Application.AI.Models;
using System;
using System.Collections.Generic;

namespace AvinyaAICRM.Application.AI.Pipeline
{
    public class SqlTemplateEngine
    {
        public string? TryGetTemplateSql(
            ClassificationResult intent,
            FilterResult filters,
            Guid tenantId,
            string userId)
        {
            var dateFilter = BuildDateFilter(filters);
            var statusFilter = BuildStatusFilter(intent.Intent, filters);
            var sourceFilter = BuildSourceFilter(intent.Intent, filters);
            var top = BuildTop(filters);

            return intent.Intent switch
            {
                // --- LEADS ---
                "query_leads" when filters.IsCountQuery =>
                    $@"SELECT ls.StatusName AS Status, COUNT(*) AS Count
                       FROM dbo.Leads l
                       JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID
                       WHERE l.TenantId = '{tenantId}' AND l.IsDeleted = 0
                       {dateFilter.Replace("{col}", "l.Date")}
                       {BuildSearchFilter(filters, new[] { "l.LeadNo", "l.Notes" })}
                       GROUP BY ls.StatusName",

                "query_leads" =>
                    $@"SELECT {top} l.LeadNo, c.CompanyName, ls.StatusName, lsrc.SourceName, u.FullName AS AssignedTo, l.Date, l.Notes
                       FROM dbo.Leads l
                       JOIN dbo.Clients c ON l.ClientID = c.ClientID
                       JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID
                       LEFT JOIN dbo.LeadSourceMaster lsrc ON l.LeadSourceID = lsrc.LeadSourceID
                       LEFT JOIN dbo.AspNetUsers u ON l.AssignedTo = u.Id
                       WHERE l.TenantId = '{tenantId}' AND l.IsDeleted = 0
                       {(filters.IsPersonalQuery ? $"AND l.AssignedTo = '{userId}'" : "")}
                       {dateFilter.Replace("{col}", "l.Date")}
                       {statusFilter}
                       {sourceFilter}
                       {BuildSearchFilter(filters, new[] { "l.LeadNo", "c.CompanyName", "l.Notes" })}
                       ORDER BY l.Date DESC",

                "query_leads_source" =>
                    $@"SELECT lsm.SourceName, COUNT(*) AS Count
                       FROM dbo.Leads l
                       JOIN dbo.LeadSourceMaster lsm ON l.LeadSourceID = lsm.LeadSourceID
                       WHERE l.TenantId = '{tenantId}' AND l.IsDeleted = 0
                       {BuildSearchFilter(filters, new[] { "l.LeadNo" })}
                       GROUP BY lsm.SourceName",

                // --- INVOICES & PAYMENTS ---
                "query_invoices" when filters.IsSumQuery =>
                    $@"SELECT SUM(GrandTotal) AS TotalBilled, SUM(PaidAmount) AS TotalCollected, 
                              SUM(OutstandingAmount) AS TotalDue
                       FROM dbo.Invoices
                       WHERE TenantId = '{tenantId}' AND IsDeleted = 0
                       {dateFilter.Replace("{col}", "InvoiceDate")}",

                "query_invoices" when filters.IsCountQuery =>
                    $@"SELECT ism.InvoiceStatusName AS Status, COUNT(*) AS Count
                       FROM dbo.Invoices i
                       JOIN dbo.InvoiceStatuses ism ON i.InvoiceStatusID = ism.InvoiceStatusID
                       WHERE i.TenantId = '{tenantId}' AND i.IsDeleted = 0
                       {dateFilter.Replace("{col}", "i.InvoiceDate")}
                       GROUP BY ism.InvoiceStatusName",

                "query_invoices" =>
                    $@"SELECT {top} i.InvoiceNo, c.CompanyName, i.GrandTotal, i.OutstandingAmount, i.DueDate
                       FROM dbo.Invoices i
                       JOIN dbo.Clients c ON i.ClientID = CAST(c.ClientID AS nvarchar(max))
                       WHERE i.TenantId = '{tenantId}' AND i.IsDeleted = 0
                       {dateFilter.Replace("{col}", "i.InvoiceDate")}
                       {statusFilter}
                       {BuildSearchFilter(filters, new[] { "i.InvoiceNo", "c.CompanyName" })}
                       ORDER BY i.DueDate ASC",

                "query_payments" =>
                    $@"SELECT {top} i.InvoiceNo, p.Amount, p.PaymentDate, p.PaymentMode, p.ReceivedBy
                       FROM dbo.Payments p
                       JOIN dbo.Invoices i ON p.InvoiceID = i.InvoiceID
                       WHERE i.TenantId = '{tenantId}' AND i.IsDeleted = 0
                       {dateFilter.Replace("{col}", "p.PaymentDate")}
                       {BuildSearchFilter(filters, new[] { "i.InvoiceNo", "p.TransactionRef", "p.ReceivedBy" })}
                       ORDER BY p.PaymentDate DESC",

                // --- PROJECTS ---
                "query_projects" when filters.IsCountQuery =>
                    $@"SELECT psm.StatusName, COUNT(*) AS ProjectCount
                       FROM dbo.Projects p
                       LEFT JOIN dbo.ProjectStatusMaster psm ON p.Status = psm.StatusID
                       WHERE p.TenantId = '{tenantId}' AND p.IsDeleted = 0
                       GROUP BY psm.StatusName",

                "query_projects" =>
                    $@"SELECT {top} p.ProjectName, c.CompanyName, psm.StatusName, 
                              p.ProgressPercent, p.Deadline, u.FullName AS Manager
                       FROM dbo.Projects p
                       LEFT JOIN dbo.Clients c ON p.ClientID = c.ClientID
                       LEFT JOIN dbo.ProjectStatusMaster psm ON p.Status = psm.StatusID
                       LEFT JOIN dbo.AspNetUsers u ON p.ProjectManagerId = u.Id
                       WHERE p.TenantId = '{tenantId}' AND p.IsDeleted = 0
                       {dateFilter.Replace("{col}", "p.CreatedDate")}
                       {statusFilter}
                       {BuildSearchFilter(filters, new[] { "p.ProjectName", "c.CompanyName" })}
                       ORDER BY p.Deadline ASC",

                // --- EXPENSES ---
                "query_expenses" when filters.IsSumQuery =>
                    $@"SELECT ec.CategoryName, SUM(e.Amount) AS TotalSpent
                       FROM dbo.Expenses e
                       JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
                       WHERE e.TenantId = '{tenantId}' AND e.IsDeleted = 0
                       {dateFilter.Replace("{col}", "e.ExpenseDate")}
                       GROUP BY ec.CategoryName",

                "query_expenses" =>
                    $@"SELECT {top} e.ExpenseDate, ec.CategoryName, e.Amount, e.PaymentMode, e.Description
                       FROM dbo.Expenses e
                       JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
                       WHERE e.TenantId = '{tenantId}' AND e.IsDeleted = 0
                       {dateFilter.Replace("{col}", "e.ExpenseDate")}
                       {BuildSearchFilter(filters, new[] { "e.Description", "ec.CategoryName" })}
                       ORDER BY e.ExpenseDate DESC",

                // --- PRODUCTS & INVENTORY ---
                "query_products" =>
                    $@"SELECT {top} p.ProductName, p.Category, p.DefaultRate, ut.UnitName, p.HSNCode
                       FROM dbo.Products p
                       LEFT JOIN dbo.UnitTypeMaster ut ON p.UnitTypeID = ut.UnitTypeID
                       WHERE p.TenantId = '{tenantId}' AND p.IsDeleted = 0
                       {BuildSearchFilter(filters, new[] { "p.ProductName", "p.Category" })}
                       ORDER BY p.ProductName ASC",

                "query_low_stock" =>
                    $@"SELECT p.ProductName, p.Status, ut.UnitName
                       FROM dbo.Products p
                       LEFT JOIN dbo.UnitTypeMaster ut ON p.UnitTypeID = ut.UnitTypeID
                       WHERE p.TenantId = '{tenantId}' AND p.IsDeleted = 0 AND p.Status = 0
                       ORDER BY p.ProductName ASC",

                // --- SALES PERFORMANCE ---
                "query_revenue" =>
                    $@"SELECT 
                        (SELECT SUM(GrandTotal) FROM dbo.Orders WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "OrderDate")}) AS TotalOrders,
                        (SELECT SUM(GrandTotal) FROM dbo.Invoices WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "InvoiceDate")}) AS TotalInvoiced,
                        (SELECT SUM(Amount) FROM dbo.Expenses WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "ExpenseDate")}) AS TotalExpenses",

                "query_revenue_trend" =>
                    $@"SELECT FORMAT(InvoiceDate, 'yyyy-MM') AS Month,
                              SUM(GrandTotal) AS MonthlyRevenue,
                              COUNT(*) AS InvoiceCount
                       FROM dbo.Invoices
                       WHERE TenantId = '{tenantId}' AND IsDeleted = 0
                       GROUP BY FORMAT(InvoiceDate, 'yyyy-MM')
                       ORDER BY Month DESC",

                // --- STAFF & PERFORMANCE ---
                "query_staff_performance" =>
                    $@"SELECT u.FullName, 
                               COUNT(DISTINCT l.LeadID) AS LeadsHandled,
                               COUNT(DISTINCT o.OrderID) AS OrdersCreated,
                               (SELECT COUNT(*) FROM dbo.Projects WHERE ProjectManagerId = u.Id OR AssignedToUserId = u.Id) AS ProjectsManaged
                       FROM dbo.AspNetUsers u
                       LEFT JOIN dbo.Leads l ON (l.CreatedBy = u.Id OR l.AssignedTo = u.Id)
                       LEFT JOIN dbo.Orders o ON o.CreatedBy = u.FullName
                       WHERE u.TenantId = '{tenantId}' AND u.IsActive = 1
                       GROUP BY u.FullName
                       ORDER BY LeadsHandled DESC",

                // --- TAX & GOVERNMENT ---
                "query_tax_summary" =>
                    $@"SELECT SUM(Taxes) AS TotalTaxesPaid, 
                               SUM(GrandTotal) AS TaxableTurnover,
                               (SUM(Taxes) / NULLIF(SUM(GrandTotal), 0)) * 100 AS EffectiveTaxRate
                       FROM dbo.Invoices
                       WHERE TenantId = '{tenantId}' AND IsDeleted = 0
                       {dateFilter.Replace("{col}", "InvoiceDate")}",

                // --- FOLLOWUPS ---
                "query_followups" =>
                    $@"SELECT {top} c.CompanyName, l.LeadNo, lf.NextFollowupDate, lf.Notes, lfs.StatusName
                       FROM dbo.LeadFollowups lf
                       JOIN dbo.Leads l ON lf.LeadID = l.LeadID
                       JOIN dbo.Clients c ON l.ClientID = c.ClientID
                       LEFT JOIN dbo.LeadFollowupStatus lfs ON lf.Status = lfs.LeadFollowupStatusID
                       WHERE l.TenantId = '{tenantId}' AND l.IsDeleted = 0
                       {dateFilter.Replace("{col}", "lf.NextFollowupDate")}
                       {statusFilter}
                       {BuildSearchFilter(filters, new[] { "l.LeadNo", "c.CompanyName", "lf.Notes" })}
                       ORDER BY lf.NextFollowupDate ASC",

                // --- OVERDUE & URGENT ---
                "query_overdue_items" =>
                    $@"SELECT 'Invoice' AS Type, InvoiceNo AS Ref, DueDate, OutstandingAmount AS Amount
                       FROM dbo.Invoices WHERE TenantId = '{tenantId}' AND IsDeleted = 0 AND DueDate < GETDATE() AND OutstandingAmount > 0
                       UNION ALL
                       SELECT 'Project' AS Type, ProjectName AS Ref, Deadline, 0 AS Amount
                       FROM dbo.Projects WHERE TenantId = '{tenantId}' AND IsDeleted = 0 AND Deadline < GETDATE() AND Status != 3
                       ORDER BY DueDate ASC",

                // --- CLIENT ENGAGEMENT ---
                "query_inactive_clients" =>
                    $@"SELECT {top} CompanyName, Mobile, Email, CreatedDate
                       FROM dbo.Clients c
                       WHERE c.TenantId = '{tenantId}' AND c.IsDeleted = 0
                       AND NOT EXISTS (SELECT 1 FROM dbo.Orders o WHERE o.ClientID = c.ClientID AND o.OrderDate > DATEADD(MONTH, -3, GETDATE()))
                       ORDER BY c.CreatedDate ASC",

                "query_clients" =>
                    $@"SELECT {top} c.CompanyName, c.ContactPerson,
                               c.Mobile, c.Email, c.Status,
                               s.StateName, ci.CityName
                       FROM dbo.Clients c
                       LEFT JOIN dbo.States s ON c.StateID = s.StateID
                       LEFT JOIN dbo.Cities ci ON c.CityID = ci.CityID
                       WHERE c.TenantId = '{tenantId}' AND c.IsDeleted = 0
                         AND c.CompanyName IS NOT NULL AND c.CompanyName <> ''
                       {BuildSearchFilter(filters, new[] { "c.CompanyName", "c.ContactPerson", "c.Email", "c.Mobile", "ci.CityName" })}
                       ORDER BY c.CompanyName ASC",

                // --- CLIENT 360 ---
                "query_client_360" =>
                    $@"SELECT 
                        c.CompanyName, c.ContactPerson, c.Mobile, c.Email, c.GSTNo, c.BillingAddress,
                        (SELECT '₹ ' + FORMAT(ISNULL(SUM(GrandTotal), 0), 'N2') FROM dbo.Invoices WHERE ClientID = CAST(c.ClientID AS nvarchar(max)) AND IsDeleted = 0) AS TotalRevenue,
                        (SELECT '₹ ' + FORMAT(ISNULL(SUM(OutstandingAmount), 0), 'N2') FROM dbo.Invoices WHERE ClientID = CAST(c.ClientID AS nvarchar(max)) AND IsDeleted = 0) AS TotalOutstanding,
                        (SELECT TOP 10 LeadNo, ls.StatusName, CONVERT(varchar(10), Date, 120) AS Date FROM dbo.Leads l JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID WHERE l.ClientID = c.ClientID AND l.IsDeleted = 0 ORDER BY l.Date DESC FOR JSON PATH) AS RecentLeads,
                        (SELECT TOP 10 OrderNo, osm.StatusName, '₹ ' + FORMAT(GrandTotal, 'N2') AS Amount, CONVERT(varchar(10), OrderDate, 120) AS Date FROM dbo.Orders o JOIN dbo.OrderStatusMaster osm ON o.Status = osm.StatusID WHERE o.ClientID = c.ClientID AND o.IsDeleted = 0 ORDER BY o.OrderDate DESC FOR JSON PATH) AS RecentOrders,
                        (SELECT TOP 10 InvoiceNo, GrandTotal, OutstandingAmount, CONVERT(varchar(10), InvoiceDate, 120) AS Date FROM dbo.Invoices WHERE ClientID = CAST(c.ClientID AS nvarchar(max)) AND IsDeleted = 0 ORDER BY InvoiceDate DESC FOR JSON PATH) AS RecentInvoices,
                        (SELECT TOP 10 lf.Notes, lf.NextFollowupDate, lfs.StatusName FROM dbo.LeadFollowups lf JOIN dbo.Leads l ON lf.LeadID = l.LeadID LEFT JOIN dbo.LeadFollowupStatus lfs ON lf.Status = lfs.LeadFollowupStatusID WHERE l.ClientID = c.ClientID ORDER BY lf.CreatedDate DESC FOR JSON PATH) AS RecentActivity
                       FROM dbo.Clients c
                       WHERE c.TenantId = '{tenantId}' AND c.IsDeleted = 0
                       {BuildSearchFilter(filters, new[] { "c.CompanyName", "c.ContactPerson", "c.Email", "c.Mobile" })}
                       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER",

                "query_high_value_clients" =>
                    $@"SELECT TOP 10 
                         c.CompanyName AS [Client Name], 
                         c.Mobile,
                         '₹ ' + FORMAT(ISNULL((SELECT SUM(GrandTotal) FROM dbo.Invoices WHERE ClientID = CAST(c.ClientID AS nvarchar(max)) AND IsDeleted = 0), 0), 'N2') AS [Total Revenue],
                         (SELECT COUNT(*) FROM dbo.Orders WHERE ClientID = c.ClientID AND IsDeleted = 0) AS [Order Count]
                       FROM dbo.Clients c
                       WHERE c.TenantId = '{tenantId}' AND c.IsDeleted = 0 
                         AND c.CompanyName IS NOT NULL AND c.CompanyName <> ''
                         AND EXISTS (SELECT 1 FROM dbo.Invoices i WHERE i.ClientID = CAST(c.ClientID AS nvarchar(max)) AND i.IsDeleted = 0)
                       ORDER BY (SELECT ISNULL(SUM(GrandTotal), 0) FROM dbo.Invoices WHERE ClientID = CAST(c.ClientID AS nvarchar(max)) AND IsDeleted = 0) DESC",

                // --- ORDERS ---
                 "query_highest_outstanding" =>
                    $@"SELECT TOP 10 
                         c.CompanyName AS [Client Name], 
                         c.Mobile,
                         '₹ ' + FORMAT(ISNULL((SELECT SUM(OutstandingAmount) FROM dbo.Invoices WHERE ClientID = CAST(c.ClientID AS nvarchar(max)) AND IsDeleted = 0), 0), 'N2') AS [Total Outstanding]
                       FROM dbo.Clients c
                       WHERE c.TenantId = '{tenantId}' AND c.IsDeleted = 0 
                         AND c.CompanyName IS NOT NULL AND c.CompanyName <> ''
                         AND EXISTS (SELECT 1 FROM dbo.Invoices i WHERE i.ClientID = CAST(c.ClientID AS nvarchar(max)) AND i.IsDeleted = 0 AND i.OutstandingAmount > 0)
                       ORDER BY (SELECT ISNULL(SUM(OutstandingAmount), 0) FROM dbo.Invoices WHERE ClientID = CAST(c.ClientID AS nvarchar(max)) AND IsDeleted = 0) DESC",

                 "query_orders" when filters.IsCountQuery =>
                    $@"SELECT osm.StatusName, COUNT(*) AS Count
                       FROM dbo.Orders o
                       JOIN dbo.OrderStatusMaster osm ON o.Status = osm.StatusID
                       WHERE o.TenantId = '{tenantId}' AND o.IsDeleted = 0
                       GROUP BY osm.StatusName",

                "query_orders" =>
                    $@"SELECT {top} o.OrderNo, c.CompanyName, osm.StatusName, o.GrandTotal, o.OrderDate
                       FROM dbo.Orders o
                       JOIN dbo.Clients c ON o.ClientID = c.ClientID
                       JOIN dbo.OrderStatusMaster osm ON o.Status = osm.StatusID
                       WHERE o.TenantId = '{tenantId}' AND o.IsDeleted = 0
                       {dateFilter.Replace("{col}", "o.OrderDate")}
                       {statusFilter}
                       {BuildSearchFilter(filters, new[] { "o.OrderNo", "c.CompanyName" })}
                       ORDER BY o.OrderDate DESC",

                // --- QUOTATIONS ---
                "query_quotations" when filters.IsCountQuery || filters.IsSumQuery =>
                    $@"SELECT 
                        qs.StatusName, 
                        COUNT(*) AS Count,
                        SUM(q.GrandTotal) AS TotalValue
                       FROM dbo.Quotations q
                       JOIN dbo.QuotationStatusMaster qs ON q.QuotationStatusID = qs.QuotationStatusID
                       WHERE q.TenantId = '{tenantId}' AND q.IsDeleted = 0
                       {dateFilter.Replace("{col}", "q.QuotationDate")}
                       GROUP BY qs.StatusName",

                "query_quotations" =>
                    $@"SELECT {top} q.QuotationNo, c.CompanyName,
                               q.GrandTotal, qs.StatusName, q.QuotationDate
                       FROM dbo.Quotations q
                       JOIN dbo.Clients c ON q.ClientID = c.ClientID
                       JOIN dbo.QuotationStatusMaster qs ON q.QuotationStatusID = qs.QuotationStatusID
                       WHERE q.TenantId = '{tenantId}' AND q.IsDeleted = 0
                       {dateFilter.Replace("{col}", "q.QuotationDate")}
                       {statusFilter}
                       {BuildSearchFilter(filters, new[] { "q.QuotationNo", "c.CompanyName" })}
                       ORDER BY q.QuotationDate DESC",

                // --- TASKS ---
                "query_tasks" when filters.IsCountQuery =>
                    $@"SELECT to2.Status, COUNT(*) AS Count
                       FROM dbo.TaskOccurrences to2
                       JOIN dbo.TaskSeries ts ON to2.TaskSeriesId = ts.Id
                       LEFT JOIN dbo.Projects p ON ts.ProjectId = p.ProjectID
                       WHERE ts.IsActive = 1
                       AND (p.TenantId = '{tenantId}' OR ts.TeamId IN (SELECT Id FROM dbo.Teams WHERE TenantId = '{tenantId}'))
                       {dateFilter.Replace("{col}", "to2.DueDateTime")}
                       GROUP BY to2.Status",

                "query_tasks" =>
                    $@"SELECT {top} ts.Title, to2.DueDateTime, to2.Status
                       FROM dbo.TaskOccurrences to2
                       JOIN dbo.TaskSeries ts ON to2.TaskSeriesId = ts.Id
                       LEFT JOIN dbo.Projects pj ON ts.ProjectId = pj.ProjectID
                       WHERE ts.IsActive = 1
                       AND (pj.TenantId = '{tenantId}' OR ts.TeamId IN (SELECT Id FROM dbo.Teams WHERE TenantId = '{tenantId}'))
                       {(filters.IsPersonalQuery ? $"AND to2.AssignedTo = '{userId}'" : "")}
                       {dateFilter.Replace("{col}", "to2.DueDateTime")}
                       {statusFilter}
                       {BuildSearchFilter(filters, new[] { "ts.Title", "ts.Description" })}
                       ORDER BY to2.DueDateTime ASC",

                // --- MASTER DASHBOARD ---
                "report_summary" =>
                    $@"SELECT 
                        (SELECT COUNT(*) FROM dbo.Clients WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "CreatedDate")}) AS ClientsCount,
                        (SELECT COUNT(*) FROM dbo.Leads WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "Date")}) AS LeadsCount,
                        (SELECT COUNT(*) FROM dbo.Orders WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "OrderDate")}) AS OrdersCount,
                        (SELECT COUNT(*) FROM dbo.Projects WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "CreatedDate")}) AS ProjectsCount,
                        (SELECT '₹ ' + FORMAT(ISNULL(SUM(GrandTotal), 0), 'N2') FROM dbo.Invoices WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "InvoiceDate")}) AS TotalRevenue,
                        (SELECT '₹ ' + FORMAT(ISNULL(SUM(Amount), 0), 'N2') FROM dbo.Expenses WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "ExpenseDate")}) AS TotalExpenses,
                        (SELECT TOP 10 l.LeadNo, c.CompanyName, ls.StatusName, CONVERT(varchar(10), l.Date, 120) AS Date
                         FROM dbo.Leads l LEFT JOIN dbo.Clients c ON l.ClientID = c.ClientID JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID
                         WHERE l.TenantId = '{tenantId}' AND l.IsDeleted = 0 ORDER BY l.Date DESC FOR JSON PATH) AS RecentLeads,

                        (SELECT TOP 10 o.OrderNo, c.CompanyName, '₹ ' + FORMAT(o.GrandTotal, 'N2') AS Amount, osm.StatusName AS Status
                         FROM dbo.Orders o JOIN dbo.Clients c ON o.ClientID = c.ClientID JOIN dbo.OrderStatusMaster osm ON o.Status = osm.StatusID
                         WHERE o.TenantId = '{tenantId}' AND o.IsDeleted = 0 ORDER BY o.OrderDate DESC FOR JSON PATH) AS RecentOrders,

                        (SELECT TOP 10 p.ProjectName, p.ProgressPercent, psm.StatusName AS Status, CONVERT(varchar(10), p.Deadline, 120) AS Deadline
                         FROM dbo.Projects p LEFT JOIN dbo.ProjectStatusMaster psm ON p.Status = psm.StatusID
                         WHERE p.TenantId = '{tenantId}' AND p.IsDeleted = 0 ORDER BY p.CreatedDate DESC FOR JSON PATH) AS RecentProjects,

                        (SELECT TOP 10 ts.Title, to2.Status, CONVERT(varchar(10), to2.DueDateTime, 120) AS DueDate
                         FROM dbo.TaskOccurrences to2 
                         JOIN dbo.TaskSeries ts ON to2.TaskSeriesId = ts.Id
                         LEFT JOIN dbo.Projects pj ON ts.ProjectId = pj.ProjectID
                         WHERE (pj.TenantId = '{tenantId}' OR ts.TeamId IN (SELECT Id FROM dbo.Teams WHERE TenantId = '{tenantId}'))
                         AND ts.IsActive = 1 ORDER BY to2.DueDateTime DESC FOR JSON PATH) AS RecentTasks
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER",

                _ => null
            };
        }

        public string GetTemplateMessage(string intent, FilterResult filters, int count)
        {
            if (count == 0)
            {
                return intent switch
                {
                    "query_leads" => "I couldn't find any leads matching those criteria.",
                    "query_followups" => "You're all caught up! No pending follow-ups found.",
                    "query_invoices" => "No invoices were found for the selected period.",
                    "query_payments" => "No payment records matched your search.",
                    "query_projects" => "I didn't find any projects matching your request.",
                    "query_expenses" => "No expense records were found for this period.",
                    "query_orders" => "No orders were found for the selected criteria.",
                    "query_tasks" => "You have no tasks scheduled matching this filter.",
                    "report_summary" => "I'm sorry, the master summary is empty as there's no business data currently.",
                    _ => "I couldn't find any records for your request."
                };
            }

            return (intent switch
            {
                "query_leads" => filters.IsCountQuery 
                    ? "You have {count} leads in total, organized by their current status."
                    : "I've pulled the latest {count} leads for you.",
                "query_followups" => "You have {count} follow-ups scheduled. The next one is for {CompanyName} on {NextFollowupDate}.",
                "query_invoices" => filters.IsSumQuery 
                    ? "Financial Check: Total billed is {TotalBilled} with {TotalDue} still outstanding."
                    : "I've listed the {count} matching invoices for your review.",
                "query_orders" => filters.IsCountQuery
                    ? "Order Breakdown: Currently tracking {count} orders by status."
                    : "Listing {count} orders. The most recent one is {OrderNo} for {CompanyName}.",
                "query_projects" => "I found {count} projects. {ProjectName} is currently {ProgressPercent}% complete.",
                "query_tasks" => "Here are your {count} tasks. {Title} is the next priority.",
                "query_client_360" => "Client 360° Report for {CompanyName}: Total revenue generated is {TotalRevenue} with {TotalOutstanding} currently outstanding.",
                "report_summary" => "Universal Business Summary: You have {LeadsCount} leads, {OrdersCount} orders, and {ProjectsCount} active projects. Total revenue is {TotalRevenue}.",
                _ => "I've found {count} results for you based on the database records."
            }).Replace("{count}", count.ToString());
        }

        private string BuildSearchFilter(FilterResult filters, string[] searchCols)
        {
            if (string.IsNullOrEmpty(filters.SearchTerm) || searchCols == null || searchCols.Length == 0)
                return "";

            var conditions = new List<string>();
            foreach (var col in searchCols)
            {
                conditions.Add($"{col} LIKE '%{filters.SearchTerm.Replace("'", "''")}%'");
            }
            return $" AND ({string.Join(" OR ", conditions)})";
        }

        private string BuildTop(FilterResult filters, int defaultLimit = 50)
        {
            return $"TOP {filters.Limit ?? defaultLimit}";
        }

        private string BuildStatusFilter(string intent, FilterResult filters)
        {
            var st = !string.IsNullOrEmpty(filters.ExplicitStatus) ? filters.ExplicitStatus : filters.Status;
            if (string.IsNullOrEmpty(st)) return "";

            return intent switch
            {
                "query_leads" => $" AND ls.StatusName LIKE '%{st}%'",
                "query_orders" => $" AND osm.StatusName LIKE '%{st}%'",
                "query_followups" => $" AND lfs.StatusName LIKE '%{st}%'",
                "query_projects" => $" AND psm.StatusName LIKE '%{st}%'",
                "query_tasks" => $" AND to2.Status LIKE '%{st}%'",
                "query_quotations" => $" AND qs.StatusName LIKE '%{st}%'",
                "query_invoices" => st == "Pending" ? " AND i.OutstandingAmount > 0" : "",
                _ => ""
            };
        }

        private string BuildSourceFilter(string intent, FilterResult filters)
        {
            if (string.IsNullOrEmpty(filters.ExplicitSource)) return "";
            if (intent == "query_leads" || intent == "query_leads_source")
                return $" AND lsrc.SourceName LIKE '%{filters.ExplicitSource}%'";
            return "";
        }

        private string BuildDateFilter(FilterResult filters) 
        {
            if (filters.ExplicitDate.HasValue)
            {
                return $@"AND CAST({{col}} AS DATE) = '{filters.ExplicitDate.Value:yyyy-MM-dd}'";
            }

            var period = filters.TimePeriod;
            if (period?.StartsWith("last_") == true && period.EndsWith("_days"))
            {
                var days = period.Split('_')[1];
                return $@"AND {{col}} >= DATEADD(DAY, -{days}, GETDATE())";
            }

            return period switch
            {
                "today"       => "AND CAST({col} AS DATE) = CAST(GETDATE() AS DATE)",
                "yesterday"   => "AND CAST({col} AS DATE) = CAST(DATEADD(DAY,-1,GETDATE()) AS DATE)",
                "this_week"   => "AND {col} >= DATEADD(DAY, -7, GETDATE())",
                "this_month"  => "AND MONTH({col}) = MONTH(GETDATE()) AND YEAR({col}) = YEAR(GETDATE())",
                "last_month"  => "AND MONTH({col}) = MONTH(DATEADD(MONTH,-1,GETDATE()))",
                "this_year"   => "AND YEAR({col}) = YEAR(GETDATE())",
                _             => "" 
            };
        }
    }
}

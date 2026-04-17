using AvinyaAICRM.Application.AI.Models;
using System;

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
            var dateFilter = BuildDateFilter(filters.TimePeriod);

            return intent.Intent switch
            {
                "query_leads" when filters.IsCountQuery =>
                    $@"SELECT ls.StatusName AS Status, COUNT(*) AS Count
                       FROM dbo.Leads l
                       JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID
                       WHERE l.TenantId = '{tenantId}' AND l.IsDeleted = 0
                       {dateFilter.Replace("{col}", "l.Date")}
                       GROUP BY ls.StatusName",

                "query_leads_source" =>
                    $@"SELECT lsm.SourceName, COUNT(*) AS Count
                       FROM dbo.Leads l
                       JOIN dbo.LeadSourceMaster lsm ON l.LeadSourceID = lsm.LeadSourceID
                       WHERE l.TenantId = '{tenantId}' AND l.IsDeleted = 0
                       GROUP BY lsm.SourceName",

                "query_followups" =>
                    $@"SELECT TOP 50 c.CompanyName, l.LeadNo,
                              lf.NextFollowupDate, lf.Notes, lfs.StatusName
                   FROM dbo.LeadFollowups lf
                   JOIN dbo.Leads l ON lf.LeadID = l.LeadID
                   JOIN dbo.Clients c ON l.ClientID = c.ClientID
                   LEFT JOIN dbo.LeadFollowupStatus lfs ON lf.Status = lfs.LeadFollowupStatusID
                   WHERE l.TenantId = '{tenantId}' AND l.IsDeleted = 0
                   {(filters.Status == "Pending" ? "AND lf.Status = 1" : "")} 
                   ORDER BY lf.NextFollowupDate ASC",

                // --- INVOICES & PAYMENTS ---
                "query_invoices" when filters.IsSumQuery =>
                    $@"SELECT SUM(GrandTotal) AS TotalBilled, SUM(PaidAmount) AS TotalCollected, 
                              SUM(OutstandingAmount) AS TotalDue
                       FROM dbo.Invoices
                       WHERE TenantId = '{tenantId}' AND IsDeleted = 0
                       {dateFilter.Replace("{col}", "InvoiceDate")}",

                "query_invoices" when filters.Status == "Pending" =>
                    $@"SELECT TOP 50 i.InvoiceNo, c.CompanyName, i.GrandTotal, i.OutstandingAmount, i.DueDate
                       FROM dbo.Invoices i
                       JOIN dbo.Clients c ON i.ClientID = CAST(c.ClientID AS nvarchar(max))
                       WHERE i.TenantId = '{tenantId}' AND i.IsDeleted = 0 AND i.OutstandingAmount > 0
                       ORDER BY i.DueDate ASC",

                "query_payments" =>
                    $@"SELECT TOP 50 i.InvoiceNo, p.Amount, p.PaymentDate, p.PaymentMode, p.ReceivedBy
                       FROM dbo.Payments p
                       JOIN dbo.Invoices i ON p.InvoiceID = i.InvoiceID
                       WHERE i.TenantId = '{tenantId}' AND i.IsDeleted = 0
                       {dateFilter.Replace("{col}", "p.PaymentDate")}
                       ORDER BY p.PaymentDate DESC",

                // --- PROJECTS ---
                "query_projects" when filters.IsCountQuery =>
                    $@"SELECT psm.StatusName, COUNT(*) AS ProjectCount
                       FROM dbo.Projects p
                       LEFT JOIN dbo.ProjectStatusMaster psm ON p.Status = psm.StatusID
                       WHERE p.TenantId = '{tenantId}' AND p.IsDeleted = 0
                       GROUP BY psm.StatusName",

                "query_projects" =>
                    $@"SELECT TOP 50 p.ProjectName, c.CompanyName, psm.StatusName, 
                              p.ProgressPercent, p.Deadline, u.FullName AS Manager
                       FROM dbo.Projects p
                       LEFT JOIN dbo.Clients c ON p.ClientID = c.ClientID
                       LEFT JOIN dbo.ProjectStatusMaster psm ON p.Status = psm.StatusID
                       LEFT JOIN dbo.AspNetUsers u ON p.ProjectManagerId = u.Id
                       WHERE p.TenantId = '{tenantId}' AND p.IsDeleted = 0
                       {dateFilter.Replace("{col}", "p.CreatedDate")}
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
                    $@"SELECT TOP 50 e.ExpenseDate, ec.CategoryName, e.Amount, e.PaymentMode, e.Description
                       FROM dbo.Expenses e
                       JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
                       WHERE e.TenantId = '{tenantId}' AND e.IsDeleted = 0
                       {dateFilter.Replace("{col}", "e.ExpenseDate")}
                       ORDER BY e.ExpenseDate DESC",

                // --- PRODUCTS & INVENTORY ---
                "query_products" =>
                    $@"SELECT TOP 50 p.ProductName, p.Category, p.DefaultRate, ut.UnitName, p.HSNCode
                       FROM dbo.Products p
                       LEFT JOIN dbo.UnitTypeMaster ut ON p.UnitTypeID = ut.UnitTypeID
                       WHERE p.TenantId = '{tenantId}' AND p.IsDeleted = 0
                       ORDER BY p.ProductName ASC",

                // --- SALES PERFORMANCE ---
                "query_revenue" =>
                    $@"SELECT 
                        (SELECT SUM(GrandTotal) FROM dbo.Orders WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "OrderDate")}) AS TotalOrders,
                        (SELECT SUM(GrandTotal) FROM dbo.Invoices WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "InvoiceDate")}) AS TotalInvoiced,
                        (SELECT SUM(Amount) FROM dbo.Expenses WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "ExpenseDate")}) AS TotalExpenses",

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
                    $@"SELECT TOP 50 CompanyName, Mobile, Email, CreatedDate
                       FROM dbo.Clients c
                       WHERE c.TenantId = '{tenantId}' AND c.IsDeleted = 0
                       AND NOT EXISTS (SELECT 1 FROM dbo.Orders o WHERE o.ClientID = c.ClientID AND o.OrderDate > DATEADD(MONTH, -3, GETDATE()))
                       ORDER BY c.CreatedDate ASC",

                // --- ORDERS & ITEMS ---
                "query_orders" when filters.IsCountQuery =>
                    $@"SELECT osm.StatusName, COUNT(*) AS Count
                       FROM dbo.Orders o
                       JOIN dbo.OrderStatusMaster osm ON o.Status = osm.StatusID
                       WHERE o.TenantId = '{tenantId}' AND o.IsDeleted = 0
                       GROUP BY osm.StatusName",

                "query_orders" =>
                    $@"SELECT TOP 50 o.OrderNo, c.CompanyName, osm.StatusName, o.GrandTotal, o.OrderDate
                       FROM dbo.Orders o
                       JOIN dbo.Clients c ON o.ClientID = c.ClientID
                       JOIN dbo.OrderStatusMaster osm ON o.Status = osm.StatusID
                       WHERE o.TenantId = '{tenantId}' AND o.IsDeleted = 0
                       {dateFilter.Replace("{col}", "o.OrderDate")}
                       ORDER BY o.OrderDate DESC",

                // --- DEEP ANALYTICS & TRENDS ---
                "query_revenue_trend" =>
                    $@"SELECT FORMAT(InvoiceDate, 'yyyy-MM') AS Month,
                              SUM(GrandTotal) AS MonthlyRevenue,
                              COUNT(*) AS InvoiceCount
                       FROM dbo.Invoices
                       WHERE TenantId = '{tenantId}' AND IsDeleted = 0
                       GROUP BY FORMAT(InvoiceDate, 'yyyy-MM')
                       ORDER BY Month DESC",

                // --- STATUS SUMMARIES ---
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

                "query_invoices" when filters.IsCountQuery =>
                    $@"SELECT ism.InvoiceStatusName AS Status, COUNT(*) AS Count
                       FROM dbo.Invoices i
                       JOIN dbo.InvoiceStatuses ism ON i.InvoiceStatusID = ism.InvoiceStatusID
                       WHERE i.TenantId = '{tenantId}' AND i.IsDeleted = 0
                       {dateFilter.Replace("{col}", "i.InvoiceDate")}
                       GROUP BY ism.InvoiceStatusName",

                "query_tasks" when filters.IsCountQuery =>
                    $@"SELECT Status, COUNT(*) AS Count
                       FROM dbo.TaskOccurrences to2
                       JOIN dbo.TaskSeries ts ON to2.TaskSeriesId = ts.Id
                       LEFT JOIN dbo.Projects p ON ts.ProjectId = p.ProjectID
                       WHERE ts.IsActive = 1
                       AND (p.TenantId = '{tenantId}' OR ts.TeamId IN (SELECT Id FROM dbo.Teams WHERE TenantId = '{tenantId}'))
                       {dateFilter.Replace("{col}", "to2.DueDateTime")}
                       GROUP BY Status",

                // --- MASTER DASHBOARD (JSON Format for Mapping) ---
                "report_summary" =>
                    $@"SELECT 
                        -- Core Counts
                        (SELECT COUNT(*) FROM dbo.Clients WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "CreatedDate")}) AS ClientsCount,
                        (SELECT COUNT(*) FROM dbo.Leads WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "Date")}) AS LeadsCount,
                        (SELECT COUNT(*) FROM dbo.Orders WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "OrderDate")}) AS OrdersCount,
                        (SELECT COUNT(*) FROM dbo.Projects WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "CreatedDate")}) AS ProjectsCount,

                        -- Financial Summary
                        (SELECT '₹ ' + FORMAT(ISNULL(SUM(GrandTotal), 0), 'N2') FROM dbo.Invoices WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "InvoiceDate")}) AS TotalRevenue,
                        (SELECT '₹ ' + FORMAT(ISNULL(SUM(Amount), 0), 'N2') FROM dbo.Expenses WHERE TenantId = '{tenantId}' AND IsDeleted = 0 {dateFilter.Replace("{col}", "ExpenseDate")}) AS TotalExpenses,

                        -- Detailed Listings
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

                "query_recent_activity" =>
                    $@"SELECT 'Lead' AS Type, LeadNo AS Ref, CreatedDate AS ActivityDate FROM dbo.Leads WHERE TenantId = '{tenantId}' AND IsDeleted = 0
                       UNION ALL
                       SELECT 'Order' AS Type, OrderNo AS Ref, CreatedDate AS ActivityDate FROM dbo.Orders WHERE TenantId = '{tenantId}' AND IsDeleted = 0
                       UNION ALL
                       SELECT 'Invoice' AS Type, InvoiceNo AS Ref, CreatedDate AS ActivityDate FROM dbo.Invoices WHERE TenantId = '{tenantId}' AND IsDeleted = 0
                       ORDER BY ActivityDate DESC",

                "query_top_clients" =>
                    $@"SELECT TOP 10 c.CompanyName, 
                              SUM(i.GrandTotal) AS TotalBilled,
                              COUNT(DISTINCT o.OrderID) AS TotalOrders
                       FROM dbo.Clients c
                       LEFT JOIN dbo.Orders o ON o.ClientID = c.ClientID
                       LEFT JOIN dbo.Invoices i ON i.ClientID = CAST(c.ClientID AS nvarchar(max))
                       WHERE c.TenantId = '{tenantId}' AND c.IsDeleted = 0
                       GROUP BY c.CompanyName
                       ORDER BY TotalBilled DESC",

                "query_expiring_quotations" =>
                    $@"SELECT TOP 20 q.QuotationNo, c.CompanyName, q.GrandTotal, q.ValidTill
                       FROM dbo.Quotations q
                       JOIN dbo.Clients c ON q.ClientID = c.ClientID
                       WHERE q.TenantId = '{tenantId}' AND q.IsDeleted = 0 
                       AND q.ValidTill BETWEEN GETDATE() AND DATEADD(DAY, 7, GETDATE())
                       ORDER BY q.ValidTill ASC",

                "query_design_orders" =>
                    $@"SELECT TOP 30 o.OrderNo, c.CompanyName, o.DesigningCharge, 
                              o.ExpectedDeliveryDate, ds.DesignStatusName
                       FROM dbo.Orders o
                       JOIN dbo.Clients c ON o.ClientID = c.ClientID
                       LEFT JOIN dbo.DesignStatusMaster ds ON o.DesignStatusID = ds.DesignStatusID
                       WHERE o.TenantId = '{tenantId}' AND o.IsDeleted = 0 AND o.IsDesignByUs = 1
                       ORDER BY o.ExpectedDeliveryDate ASC",

                // Existing ones for fallback support
                "query_leads" when filters.IsPersonalQuery =>
                    $@"SELECT TOP 50 l.LeadNo, c.CompanyName, ls.StatusName, l.Date, l.Notes
                       FROM dbo.Leads l
                       JOIN dbo.Clients c ON l.ClientID = c.ClientID
                       JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID
                       WHERE l.TenantId = '{tenantId}' AND l.IsDeleted = 0
                       AND l.AssignedTo = '{userId}'
                       {dateFilter.Replace("{col}", "l.Date")}
                       ORDER BY l.Date DESC",

                "query_leads" =>
                    $@"SELECT TOP 50 l.LeadNo, c.CompanyName, ls.StatusName,
                              lsrc.SourceName, u.FullName AS AssignedTo, l.Date
                       FROM dbo.Leads l
                       JOIN dbo.Clients c ON l.ClientID = c.ClientID
                       JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID
                       LEFT JOIN dbo.LeadSourceMaster lsrc ON l.LeadSourceID = lsrc.LeadSourceID
                       LEFT JOIN dbo.AspNetUsers u ON l.AssignedTo = u.Id
                       WHERE l.TenantId = '{tenantId}' AND l.IsDeleted = 0
                       {dateFilter.Replace("{col}", "l.Date")}
                       ORDER BY l.Date DESC",

                "query_tasks" when filters.IsPersonalQuery =>
                    $@"SELECT TOP 50 ts.Title, to2.DueDateTime, to2.Status
                       FROM dbo.TaskOccurrences to2
                       JOIN dbo.TaskSeries ts ON to2.TaskSeriesId = ts.Id
                       WHERE to2.AssignedTo = '{userId}' AND ts.IsActive = 1
                       {(filters.Status == "Pending" ? "AND to2.Status = 'Pending'" : "")}
                       ORDER BY to2.DueDateTime ASC",

                "query_clients" =>
                    $@"SELECT TOP 50 c.CompanyName, c.ContactPerson,
                               c.Mobile, c.Email, c.Status,
                               s.StateName, ci.CityName
                       FROM dbo.Clients c
                       LEFT JOIN dbo.States s ON c.StateID = s.StateID
                       LEFT JOIN dbo.Cities ci ON c.CityID = ci.CityID
                       WHERE c.TenantId = '{tenantId}' AND c.IsDeleted = 0
                       ORDER BY c.CompanyName ASC",

                "query_quotations" =>
                    $@"SELECT TOP 50 q.QuotationNo, c.CompanyName,
                               q.GrandTotal, qs.StatusName, q.QuotationDate
                       FROM dbo.Quotations q
                       JOIN dbo.Clients c ON q.ClientID = c.ClientID
                       JOIN dbo.QuotationStatusMaster qs ON q.QuotationStatusID = qs.QuotationStatusID
                       WHERE q.TenantId = '{tenantId}' AND q.IsDeleted = 0
                       {dateFilter.Replace("{col}", "q.QuotationDate")}
                       ORDER BY q.QuotationDate DESC",

                _ => null // No template
            };
        }

        private string BuildDateFilter(string period) => period switch
        {
            "today"       => "AND CAST({col} AS DATE) = CAST(GETDATE() AS DATE)",
            "yesterday"   => "AND CAST({col} AS DATE) = CAST(DATEADD(DAY,-1,GETDATE()) AS DATE)",
            "this_week"   => "AND {col} >= DATEADD(DAY, -7, GETDATE())",
            "this_month"  => "AND MONTH({col}) = MONTH(GETDATE()) AND YEAR({col}) = YEAR(GETDATE())",
            "last_month"  => "AND MONTH({col}) = MONTH(DATEADD(MONTH,-1,GETDATE()))",
            "this_year"   => "AND YEAR({col}) = YEAR(GETDATE())",
            _             => "" // no filter
        };

        private string BuildDateFilterVariable(string period) => period switch
        {
            "today"       => "GETDATE()",
            "yesterday"   => "DATEADD(DAY,-1,GETDATE())",
            "this_week"   => "DATEADD(DAY,-7,GETDATE())",
            "this_month"  => "DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE()), 0)",
            "last_month"  => "DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE()) - 1, 0)",
            "this_year"   => "DATEADD(YEAR, DATEDIFF(YEAR, 0, GETDATE()), 0)",
            _             => "DATEADD(YEAR, -100, GETDATE())"
        };
    }
}

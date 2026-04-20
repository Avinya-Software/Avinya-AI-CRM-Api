namespace AvinyaAICRM.Application.AI.Knowledge
{
    public static class AIKnowledgeBase
    {
        public static string GetFullContext()
        {
            return @"
=== YOUR CRM BUSINESS RULES ===

CLIENTS / CUSTOMERS:
- 'Client' and 'Customer' are SYNONYMS. Always use dbo.Clients for both.
- Active client/customer = Status = 'Active' (1)
- Inactive client/customer = no orders in last 3 months
- New client/customer = created in last 30 days
- Top clients/customers = order by SUM(GrandTotal) DESC
- Growing client/customer = order count this month > last month
- Lost client/customer = no activity in last 6 months but had business before

LEADS:
- A lead is 'new' when LeadStatusMaster.StatusName = 'New'
- A lead is 'converted' when StatusName = 'Converted'
- A lead is 'lost' when StatusName = 'Lost'
- Conversion rate = (Converted / Total) * 100
- Lead Source breakdown = Group by SourceName
- Lead Trend = Group by FORMAT(Date, 'yyyy-MM')

FINANCE:
- Revenue = SUM(Invoices.GrandTotal) WHERE IsDeleted = 0
- Collected = SUM(PaidAmount) FROM Invoices
- Outstanding = SUM(OutstandingAmount) FROM Invoices
- Profit = Revenue - Expenses
- Revenue Trend = Group by MONTH of InvoiceDate

STAFF:
- Performance = Leads Handled, Orders Closed, Revenue Generated
- Workload = Count of 'Pending' Tasks or 'In Progress' Projects assigned to user

=== GOLDEN SQL PATTERNS (Copy these exactly) ===

-- 1. Client 360 (Powerful Summary)
SELECT 
    c.CompanyName, c.ContactPerson, c.Mobile, c.Email, c.GSTNo, c.BillingAddress,
    (SELECT '₹ ' + FORMAT(ISNULL(SUM(GrandTotal), 0), 'N2') FROM dbo.Invoices WHERE ClientID = CAST(c.ClientID AS nvarchar(max)) AND IsDeleted = 0) AS TotalRevenue,
    (SELECT '₹ ' + FORMAT(ISNULL(SUM(OutstandingAmount), 0), 'N2') FROM dbo.Invoices WHERE ClientID = CAST(c.ClientID AS nvarchar(max)) AND IsDeleted = 0) AS TotalOutstanding,
    (SELECT TOP 10 LeadNo, ls.StatusName, CONVERT(varchar(10), Date, 120) AS Date FROM dbo.Leads l JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID WHERE l.ClientID = c.ClientID AND l.IsDeleted = 0 ORDER BY l.Date DESC FOR JSON PATH) AS RecentLeads,
    (SELECT TOP 10 OrderNo, osm.StatusName, '₹ ' + FORMAT(GrandTotal, 'N2') AS Amount, CONVERT(varchar(10), OrderDate, 120) AS Date FROM dbo.Orders o JOIN dbo.OrderStatusMaster osm ON o.Status = osm.StatusID WHERE o.ClientID = c.ClientID AND o.IsDeleted = 0 ORDER BY o.OrderDate DESC FOR JSON PATH) AS RecentOrders,
    (SELECT TOP 10 InvoiceNo, GrandTotal, OutstandingAmount, CONVERT(varchar(10), InvoiceDate, 120) AS Date FROM dbo.Invoices WHERE ClientID = CAST(c.ClientID AS nvarchar(max)) AND IsDeleted = 0 ORDER BY InvoiceDate DESC FOR JSON PATH) AS RecentInvoices,
    (SELECT TOP 10 lf.Notes, lf.NextFollowupDate, lfs.StatusName FROM dbo.LeadFollowups lf JOIN dbo.Leads l ON lf.LeadID = l.LeadID LEFT JOIN dbo.LeadFollowupStatus lfs ON lf.Status = lfs.LeadFollowupStatusID WHERE l.ClientID = c.ClientID ORDER BY lf.CreatedDate DESC FOR JSON PATH) AS RecentActivity
FROM dbo.Clients c WHERE c.CompanyName LIKE '%ABC%'
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER

-- 2. Staff Performance
SELECT u.FullName, 
    COUNT(DISTINCT l.LeadID) AS Leads,
    COUNT(DISTINCT o.OrderID) AS Orders,
    SUM(i.GrandTotal) AS Revenue
FROM dbo.AspNetUsers u
LEFT JOIN dbo.Leads l ON l.AssignedTo = u.Id
LEFT JOIN dbo.Orders o ON o.CreatedBy = u.FullName
LEFT JOIN dbo.Invoices i ON i.OrderID = o.OrderID
WHERE u.TenantId = @TenantId
GROUP BY u.FullName

-- 3. Lead Conversion Rate
SELECT 
    lsrc.SourceName,
    COUNT(*) AS TotalLeads,
    SUM(CASE WHEN ls.StatusName = 'Converted' THEN 1 ELSE 0 END) AS Converted,
    (CAST(SUM(CASE WHEN ls.StatusName = 'Converted' THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*)) * 100 AS ConversionRate
FROM dbo.Leads l
JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID
JOIN dbo.LeadSourceMaster lsrc ON l.LeadSourceID = lsrc.LeadSourceID
GROUP BY lsrc.SourceName

-- 4. Expenses vs Revenue
SELECT 
    (SELECT SUM(GrandTotal) FROM dbo.Invoices WHERE TenantId = @TenantId) AS TotalRevenue,
    (SELECT SUM(Amount) FROM dbo.Expenses WHERE TenantId = @TenantId) AS TotalExpenses,
    (SELECT SUM(GrandTotal) FROM dbo.Invoices WHERE TenantId = @TenantId) - (SELECT SUM(Amount) FROM dbo.Expenses WHERE TenantId = @TenantId) AS NetProfit
";
        }
    }
}

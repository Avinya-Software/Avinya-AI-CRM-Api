namespace AvinyaAICRM.Application.AI.Knowledge
{
    public static class AIKnowledgeBase
    {
        public static string GetFullContext()
        {
            return @"
=== YOUR CRM BUSINESS RULES ===

LEADS:
- A lead is 'new' when LeadStatusMaster.StatusName = 'New'
- A lead is 'converted' when StatusName = 'Converted'
- A lead is 'lost' when StatusName = 'Lost'
- Lead age = DATEDIFF(day, Date, GETDATE())
- Hot leads = created in last 7 days + status is 'New' or 'Quotation Sent'
- Follow-up overdue = LeadFollowups.NextFollowupDate < GETDATE() AND Status != 'Completed'

CLIENTS:
- Active client = Status = 'Active'
- Client has business = has at least 1 Order or Quotation

ORDERS:
- Revenue = SUM(Orders.GrandTotal) WHERE IsDeleted = 0
- Pending orders = OrderStatusMaster.StatusName = 'Pending'
- Completed orders = StatusName = 'Delivered'
- This month revenue = MONTH(OrderDate) = MONTH(GETDATE()) AND YEAR(OrderDate) = YEAR(GETDATE())

TASKS:
- Overdue task = TaskOccurrences.DueDateTime < GETDATE() AND Status = 'Pending'
- My tasks = TaskOccurrences.AssignedTo = @UserId
- Team tasks = TaskSeries.TaskScope = 'Team'

QUOTATIONS:
- Conversion rate = Accepted / Total * 100
- Won = QuotationStatusMaster.StatusName = 'Accepted'
- Lost = StatusName = 'Rejected'

DATE SHORTCUTS (Always use these patterns):
- Today        = CAST(GETDATE() AS DATE)
- This week    = Date >= DATEADD(DAY, -7, GETDATE())
- This month   = MONTH(Date) = MONTH(GETDATE()) AND YEAR(Date) = YEAR(GETDATE())
- This year    = YEAR(Date) = YEAR(GETDATE())
- Last month   = MONTH(Date) = MONTH(DATEADD(MONTH,-1,GETDATE()))
- Last 30 days = Date >= DATEADD(DAY, -30, GETDATE())

=== GOLDEN SQL PATTERNS (Copy these exactly) ===

-- 1. Lead list with status and client
SELECT TOP 50
    l.LeadNo, c.CompanyName, ls.StatusName, lsrc.SourceName,
    u.FullName AS AssignedTo, l.Date, l.Notes
FROM dbo.Leads l
JOIN dbo.Clients c ON l.ClientID = c.ClientID
JOIN dbo.LeadStatusMaster ls ON l.LeadStatusID = ls.LeadStatusID
LEFT JOIN dbo.LeadSourceMaster lsrc ON l.LeadSourceID = lsrc.LeadSourceID
LEFT JOIN dbo.AspNetUsers u ON l.AssignedTo = u.Id
WHERE l.TenantId = @TenantId AND l.IsDeleted = 0
ORDER BY l.Date DESC

-- 2. Revenue summary
SELECT 
    COUNT(*) AS TotalOrders,
    SUM(GrandTotal) AS TotalRevenue,
    AVG(GrandTotal) AS AvgOrderValue
FROM dbo.Orders
WHERE TenantId = @TenantId AND IsDeleted = 0
AND YEAR(OrderDate) = YEAR(GETDATE())

-- 3. Pending follow-ups
SELECT 
    c.CompanyName, l.LeadNo, lf.NextFollowupDate,
    lf.Notes, lf.Status, u.FullName AS AssignedTo
FROM dbo.LeadFollowups lf
JOIN dbo.Leads l ON lf.LeadID = l.LeadID
JOIN dbo.Clients c ON l.ClientID = c.ClientID
LEFT JOIN dbo.AspNetUsers u ON lf.FollowUpBy = u.Id
WHERE l.TenantId = @TenantId AND l.IsDeleted = 0
AND lf.Status != 'Completed'
ORDER BY lf.NextFollowupDate ASC
";
        }
    }
}

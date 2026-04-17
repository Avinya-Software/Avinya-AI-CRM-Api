using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Constant
{
    public static class AISchema
    {
        public static Dictionary<string, string> Tables = new Dictionary<string, string>
        {
            { "Leads", "dbo.Leads(LeadID [PK], LeadNo [Auto-gen], ClientID [ref: dbo.Clients], Date [Lead Date], RequirementDetails, LeadSourceID [ref: dbo.LeadSourceMaster], OtherSources, LeadStatusID [ref: dbo.LeadStatusMaster], CreatedBy [ref: dbo.AspNetUsers], AssignedTo [ref: dbo.AspNetUsers], CreatedDate, Notes, Links, TenantId)" },
            { "LeadFollowups", "dbo.LeadFollowups(FollowUpID [PK], LeadID [ref: dbo.Leads], UpdatedDate, Notes [Followup Details], NextFollowupDate, Status [Values: Pending, In Progress, Completed], FollowUpBy [ref: dbo.AspNetUsers], CreatedDate)" },
            { "LeadSourceMaster", "dbo.LeadSourceMaster(LeadSourceID [PK], SourceName [Values: Walk-in, Call, Referral, WhatsApp, Other Sources], IsActive, CreatedDate, SortOrder)" },
            { "LeadStatusMaster", "dbo.LeadStatusMaster(LeadStatusID [PK], StatusName [Values: New, Quotation Sent, Converted, JobWork In Process, Dispatched To Customer, Delivered/Done, Lost], IsActive, CreatedDate, SortOrder)" },
            { "LeadFollowupStatus", "dbo.LeadFollowupStatus(LeadFollowupStatusID [PK], StatusName [Values: Pending, In Progress, Completed])" },
            { "Clients", "dbo.Clients(ClientID [PK], CompanyName, ContactPerson, Mobile, Email, GSTNo, BillingAddress, Status, Notes, CreatedBy [ref: dbo.AspNetUsers], CreatedDate, StateID [ref: dbo.States], CityID [ref: dbo.Cities], TenantId)" },
            { "Quotations", "dbo.Quotations(QuotationID [PK], QuotationNo, ClientID [ref: dbo.Clients], LeadID [ref: dbo.Leads], QuotationDate, ValidTill, TotalAmount, Taxes, GrandTotal, QuotationStatusID [ref: dbo.QuotationStatusMaster], CreatedBy [ref: dbo.AspNetUsers], CreatedDate, RejectedNotes, TermsAndConditions, TenantId)" },
            { "QuotationItems", "dbo.QuotationItems(QuotationItemID [PK], QuotationID [ref: dbo.Quotations], ProductID [ref: dbo.Products], Description, Quantity, UnitPrice, LineTotal)" },
            { "QuotationStatusMaster", "dbo.QuotationStatusMaster(QuotationStatusID [PK], StatusName [Values: Sent, Accepted, Rejected], IsActive, CreatedDate)" },
            { "Orders", "dbo.Orders(OrderID [PK], OrderNo, ClientID [ref: dbo.Clients], QuotationID [ref: dbo.Quotations], OrderDate, ExpectedDeliveryDate, Status [ref: dbo.OrderStatusMaster], DesignStatusID [ref: dbo.DesignStatusMaster], CreatedBy [ref: dbo.AspNetUsers], AssignedDesignTo [ref: dbo.AspNetUsers], CreatedDate, SubTotal, TotalTaxes, GrandTotal, ShippingAddress, TenantId)" },
            { "OrderItems", "dbo.OrderItems(OrderItemID [PK], OrderID [ref: dbo.Orders], ProductID [ref: dbo.Products], Description, Quantity, UnitPrice, LineTotal)" },
            { "OrderStatusMaster", "dbo.OrderStatusMaster(StatusID [PK], StatusName [Values: Pending, In Progress, Inward Done, Ready, Delivered])" },
            { "DesignStatusMaster", "dbo.DesignStatusMaster(DesignStatusID [PK], DesignStatusName)" },
            { "Products", "dbo.Products(ProductID [PK], ProductName, Category, DefaultRate, PurchasePrice, HSNCode, TaxCategoryID [ref: dbo.TaxCategoryMaster], UnitTypeID [ref: dbo.UnitTypeMaster], Description, Status, CreatedBy [ref: dbo.AspNetUsers], CreatedDate, TenantId)" },
            { "TaxCategoryMaster", "dbo.TaxCategoryMaster(TaxCategoryID [PK], TaxName [e.g. GST 18%%], Rate, IsCompound)" },
            { "Expenses", "dbo.Expenses(ExpenseId [PK], ExpenseDate, CategoryId [ref: dbo.ExpenseCategories], Amount, PaymentMode, Description, Status, CreatedBy [ref: dbo.AspNetUsers], CreatedDate, TenantId)" },
            { "ExpenseCategories", "dbo.ExpenseCategories(CategoryId [PK], CategoryName, IsActive, CreatedDate)" },
            { "Invoices", "dbo.Invoices(InvoiceID [PK], InvoiceNo, OrderID [ref: dbo.Orders], ClientID [ref: dbo.Clients], InvoiceDate, SubTotal, Taxes, Discount, GrandTotal, InvoiceStatusID [ref: dbo.InvoiceStatuses], RemainingPayment, PaidAmount, OutstandingAmount, DueDate, TenantId)" },
            { "InvoiceStatuses", "dbo.InvoiceStatuses(InvoiceStatusID [PK], InvoiceStatusName [Values: Unpaid, Partially Paid, Paid, Overdue])" },
            { "Payments", "dbo.Payments(PaymentID [PK], InvoiceID [ref: dbo.Invoices], PaymentDate, Amount, PaymentMode [Values: Online, UPI, Card, Cash], TransactionRef, ReceivedBy)" },
            { "BankDetails", "dbo.BankDetails(BankAccountId [PK], BankName, AccountHolderName, AccountNumber, IFSCCode, BranchName, IsActive, TenantId)" },
            { "Projects", "dbo.Projects(ProjectID [PK], ProjectName, Description, ClientID [ref: dbo.Clients], Location, Status [ref: dbo.ProjectStatusMaster], ProgressPercent, ProjectManagerId [ref: dbo.AspNetUsers], AssignedToUserId [ref: dbo.AspNetUsers], TeamId [ref: dbo.Teams], StartDate, EndDate, Deadline, EstimatedValue, CreatedBy [ref: dbo.AspNetUsers], CreatedDate, PriorityID [ref: dbo.ProjectPriorityMaster], TenantId)" },
            { "ProjectStatusMaster", "dbo.ProjectStatusMaster(StatusID [PK], StatusName [Values: Not Started, In Progress, Completed, On Hold, Cancelled])" },
            { "ProjectPriorityMaster", "dbo.ProjectPriorityMaster(PriorityID [PK], PriorityName [Values: Low, Medium, High, Urgent])" },
            { "AspNetUsers", "dbo.AspNetUsers(Id [PK], FullName, Email, PhoneNumber, IsActive, CreatedAt, TenantId)" },
            { "Teams", "dbo.Teams(Id [PK], Name, ManagerId [ref: dbo.AspNetUsers], IsActive, CreatedAt, TenantId)" },
            { "TeamMembers", "dbo.TeamMembers(Id [PK], TeamId [ref: dbo.Teams], UserId [ref: dbo.AspNetUsers], JoinedAt)" },
            { "States", "dbo.States(StateID [PK], StateName)" },
            { "Cities", "dbo.Cities(CityID [PK], StateID [ref: dbo.States], CityName)" },
            { "UnitTypeMaster", "dbo.UnitTypeMaster(UnitTypeID [PK], UnitName [Values: Page, Design, Banner, Pcs, Set, Roll, Sheet, Hour, Job, Inch, Cm, Sqft, Sqmtr, Pack], Description, Status, CreatedDate)" },
            { "TaskSeries", "dbo.TaskSeries(Id [PK], Title, Description, Notes, IsRecurring, RecurrenceRule, StartDate, EndDate, CreatedBy [ref: dbo.AspNetUsers], TeamId [ref: dbo.Teams], IsActive, CreatedAt, TaskScope [Values: Personal, Team], Priority, ProjectId [ref: dbo.Projects])" },
            { "TaskOccurrences", "dbo.TaskOccurrences(Id [PK], TaskSeriesId [ref: dbo.TaskSeries], DueDateTime, StartDateTime, EndDateTime, Status [Values: Pending, Completed, Deferred], AssignedTo [ref: dbo.AspNetUsers], CreatedAt)" },
            { "TaskLists", "dbo.TaskLists(Id [PK], Name, OwnerId [ref: dbo.AspNetUsers], CreatedAt)" },
            { "Tenants", "dbo.Tenants(TenantId [PK], CompanyName, IndustryType, CompanyEmail, CompanyPhone, Address, IsApproved, IsActive, CreatedAt)" },
            { "Modules", "dbo.Modules(ModuleId [PK], ModuleKey, ModuleName, IsActive)" },
            { "Permissions", "dbo.Permissions(PermissionId [PK], ModuleId [ref: dbo.Modules], ActionId [ref: dbo.Actions])" },
            { "Actions", "dbo.Actions(ActionId [PK], ActionKey, ActionName)" },
            { "Settings", "dbo.Settings(SettingID [PK], EntityType, Value, PreFix, Digits, TenantId)" }
        };

        public static string CRM => GetTables(Tables.Keys);

        private static readonly Dictionary<string, string[]> IntentTables = new()
        {
            { "query_leads",      new[] { "Leads", "Clients", "LeadStatusMaster", "LeadSourceMaster", "AspNetUsers" } },
            { "query_followups",  new[] { "LeadFollowups", "Leads", "Clients", "AspNetUsers" } },
            { "query_orders",     new[] { "Orders", "Clients", "OrderStatusMaster" } },
            { "query_revenue",    new[] { "Orders" } },
            { "query_quotations", new[] { "Quotations", "Clients", "QuotationStatusMaster" } },
            { "query_tasks",      new[] { "TaskSeries", "TaskOccurrences", "AspNetUsers" } },
            { "query_clients",    new[] { "Clients", "States", "Cities" } },
            { "query_expenses",   new[] { "Expenses", "ExpenseCategories" } },
            { "query_projects",   new[] { "Projects", "Clients", "ProjectStatusMaster", "AspNetUsers" } },
            { "report_summary",   new[] { "Leads", "Quotations", "Orders", "Expenses", "Projects", "TaskSeries" } },
        };

        public static string GetForIntent(string intent)
        {
            if (!IntentTables.TryGetValue(intent, out var tables))
                return CRM; 

            return GetTables(tables);
        }

        public static string GetTables(IEnumerable<string> tableNames)
        {
            var systemColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { 
                "IsDeleted", "ModifiedBy", "ModifiedDate", 
                "DeletedBy", "DeletedDate", "UpdatedAt", "ConcurrencyStamp", 
                "SecurityStamp", "PasswordHash", "NormalizedUserName", "NormalizedEmail", 
                "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount",
                "FirmID", "RealmId", "AccessToken", "RefreshToken", "TokenExpiry"
            };

            var sb = new StringBuilder();
            sb.AppendLine("Schema (Table: Columns with relationships/types):");
            foreach (var name in tableNames)
            {
                if (Tables.TryGetValue(name.Trim(), out var schema))
                {
                    // Basic column pruning: Remove specific system columns if they appear in dictionary
                    var parts = schema.Split(new[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var tableName = parts[0].Trim();
                    var columns = parts.Skip(1)
                                      .Select(c => c.Trim())
                                      .Where(c => !systemColumns.Contains(c.Split(' ')[0]))
                                      .ToList();

                    sb.AppendLine($"{tableName}({string.Join(", ", columns)})");
                }
            }
            var allRelationships = new[] { 
                "Leads.ClientID=Clients.ClientID", 
                "Leads.LeadStatusID=LeadStatusMaster.LeadStatusID", 
                "Leads.LeadSourceID=LeadSourceMaster.LeadSourceID", 
                "Leads.AssignedTo=AspNetUsers.Id", 
                "Leads.CreatedBy=AspNetUsers.Id",
                "LeadFollowups.LeadID=Leads.LeadID", 
                "LeadFollowups.FollowUpBy=AspNetUsers.Id", 
                "Clients.StateID=States.StateID",
                "Clients.CityID=Cities.CityID",
                "Cities.StateID=States.StateID",
                "Clients.CreatedBy=AspNetUsers.Id",
                "Quotations.ClientID=Clients.ClientID",
                "Quotations.LeadID=Leads.LeadID", 
                "Quotations.QuotationStatusID=QuotationStatusMaster.QuotationStatusID",
                "Quotations.CreatedBy=AspNetUsers.Id",
                "QuotationItems.QuotationID=Quotations.QuotationID",
                "QuotationItems.ProductID=Products.ProductID",
                "Orders.ClientID=Clients.ClientID", 
                "Orders.QuotationID=Quotations.QuotationID",
                "Orders.Status=OrderStatusMaster.StatusID", 
                "Orders.DesignStatusID=DesignStatusMaster.DesignStatusID",
                "Orders.CreatedBy=AspNetUsers.Id",
                "Orders.AssignedDesignTo=AspNetUsers.Id",
                "OrderItems.OrderID=Orders.OrderID",
                "OrderItems.ProductID=Products.ProductID",
                "Invoices.InvoiceStatusID=InvoiceStatuses.InvoiceStatusID",
                "Payments.InvoiceID=Invoices.InvoiceID",
                "Products.TaxCategoryID=TaxCategoryMaster.TaxCategoryID",
                "Products.UnitTypeID=UnitTypeMaster.UnitTypeID",
                "Products.CreatedBy=AspNetUsers.Id",
                "Expenses.CategoryId=ExpenseCategories.CategoryId",
                "Expenses.CreatedBy=AspNetUsers.Id",
                "Projects.ClientID=Clients.ClientID",
                "Projects.Status=ProjectStatusMaster.StatusID", 
                "Projects.PriorityID=ProjectPriorityMaster.PriorityID",
                "Projects.ProjectManagerId=AspNetUsers.Id",
                "Projects.AssignedToUserId=AspNetUsers.Id",
                "Projects.TeamId=Teams.Id",
                "Projects.CreatedBy=AspNetUsers.Id",
                "Teams.ManagerId=AspNetUsers.Id", 
                "TeamMembers.TeamId=Teams.Id",
                "TeamMembers.UserId=AspNetUsers.Id",
                "TaskSeries.CreatedBy=AspNetUsers.Id",
                "TaskSeries.TeamId=Teams.Id",
                "TaskSeries.ListId=TaskLists.Id",
                "TaskSeries.ProjectId=Projects.ProjectID",
                "TaskSeries.ParentTaskSeriesId=TaskSeries.Id",
                "TaskOccurrences.TaskSeriesId=TaskSeries.Id",
                "TaskOccurrences.ParentOccurrenceId=TaskOccurrences.Id",
                "TaskOccurrences.AssignedTo=AspNetUsers.Id",
                "TaskLists.OwnerId=AspNetUsers.Id",
                "Permissions.ModuleId=Modules.ModuleId",
                "Permissions.ActionId=Actions.ActionId"
            };

            var validRels = allRelationships.Where(rel => {
                var sides = rel.Split('=');
                if (sides.Length != 2) return false;

                var table1 = sides[0].Split('.').FirstOrDefault()?.Trim();
                var table2 = sides[1].Split('.').FirstOrDefault()?.Trim();

                return tableNames.Any(t => t.Equals(table1, StringComparison.OrdinalIgnoreCase)) && 
                       tableNames.Any(t => t.Equals(table2, StringComparison.OrdinalIgnoreCase));
            }).ToList();

            if (validRels.Any())
            {
                sb.AppendLine("\nRelationships: " + string.Join(", ", validRels));
            }

            // HINTS for AI
            sb.AppendLine("\nSQL Generation Hints:");
            sb.AppendLine("- Use 'CompanyName' for clients, not 'ClientName'.");
            sb.AppendLine("- Use 'StatusName' from Master tables for readable statuses.");
            sb.AppendLine("- Always filter by TenantId = @TenantId unless SuperAdmin.");
            sb.AppendLine("- Join TaskSeries with TaskOccurrences to get actual task instances.");
            sb.AppendLine("- For Leads, join with LeadStatusMaster to get the status name.");

            return sb.ToString();
        }
    }

}

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
            { "Leads", "dbo.Leads(LeadID, LeadNo, ClientID [ref: dbo.Clients], Date, RequirementDetails, LeadSourceID [ref: dbo.LeadSourceMaster], OtherSources, LeadStatusID [ref: dbo.LeadStatusMaster], CreatedBy [ref: dbo.AspNetUsers], AssignedTo [ref: dbo.AspNetUsers], CreatedDate, Notes, Links, TenantId)" },
            { "LeadFollowups", "dbo.LeadFollowups(FollowUpID, LeadID [ref: dbo.Leads], UpdatedDate, Notes, NextFollowupDate, Status, FollowUpBy [ref: dbo.AspNetUsers], CreatedDate)" },
            { "LeadSourceMaster", "dbo.LeadSourceMaster(LeadSourceID, SourceName [Values: Walk-in, Call, Referral, WhatsApp, Other Sources], IsActive, CreatedDate, SortOrder)" },
            { "LeadStatusMaster", "dbo.LeadStatusMaster(LeadStatusID, StatusName [Values: New, Quotation Sent, Converted, JobWork In Process, Dispatched To Customer, Delivered/Done, Lost], IsActive, CreatedDate, SortOrder)" },
            { "LeadFollowupStatus", "dbo.LeadFollowupStatus(LeadFollowupStatusID, StatusName [Values: Pending, In Progress, Completed])" },
            { "Clients", "dbo.Clients(ClientID, CompanyName, ContactPerson, Mobile, Email, GSTNo, BillingAddress, Status, Notes, CreatedBy [ref: dbo.AspNetUsers], CreatedDate, StateID [ref: dbo.States], CityID [ref: dbo.Cities], TenantId)" },
            { "Quotations", "dbo.Quotations(QuotationID, QuotationNo, ClientID [ref: dbo.Clients], LeadID [ref: dbo.Leads], QuotationDate, ValidTill, TotalAmount, Taxes, GrandTotal, QuotationStatusID [ref: dbo.QuotationStatusMaster], CreatedBy [ref: dbo.AspNetUsers], CreatedDate, RejectedNotes, TermsAndConditions, TenantId)" },
            { "QuotationItems", "dbo.QuotationItems(QuotationItemID, QuotationID [ref: dbo.Quotations], ProductID [ref: dbo.Products], Description, Quantity, UnitPrice, LineTotal)" },
            { "QuotationStatusMaster", "dbo.QuotationStatusMaster(QuotationStatusID, StatusName [Values: Sent, Accepted, Rejected], IsActive, CreatedDate)" },
            { "Orders", "dbo.Orders(OrderID, OrderNo, ClientID [ref: dbo.Clients], QuotationID [ref: dbo.Quotations], OrderDate, ExpectedDeliveryDate, Status [ref: dbo.OrderStatusMaster], DesignStatusID [ref: dbo.DesignStatusMaster], CreatedBy [ref: dbo.AspNetUsers], AssignedDesignTo [ref: dbo.AspNetUsers], CreatedDate, SubTotal, TotalTaxes, GrandTotal, ShippingAddress, TenantId)" },
            { "OrderItems", "dbo.OrderItems(OrderItemID, OrderID [ref: dbo.Orders], ProductID [ref: dbo.Products], Description, Quantity, UnitPrice, LineTotal)" },
            { "OrderStatusMaster", "dbo.OrderStatusMaster(StatusID, StatusName [Values: Pending, In Progress, Inward Done, Ready, Delivered])" },
            { "DesignStatusMaster", "dbo.DesignStatusMaster(DesignStatusID, DesignStatusName)" },
            { "Products", "dbo.Products(ProductID, ProductName, Category, DefaultRate, PurchasePrice, HSNCode, TaxCategoryID [ref: dbo.TaxCategoryMaster], UnitTypeID [ref: dbo.UnitTypeMaster], Description, Status, CreatedBy [ref: dbo.AspNetUsers], CreatedDate, TenantId)" },
            { "TaxCategoryMaster", "dbo.TaxCategoryMaster(TaxCategoryID, TaxName, Rate, IsCompound)" },
            { "Expenses", "dbo.Expenses(ExpenseId, ExpenseDate, CategoryId [ref: dbo.ExpenseCategories], Amount, PaymentMode, Description, Status, CreatedBy [ref: dbo.AspNetUsers], CreatedDate, TenantId)" },
            { "ExpenseCategories", "dbo.ExpenseCategories(CategoryId, CategoryName, IsActive, CreatedDate)" },
            { "Projects", "dbo.Projects(ProjectID, ProjectName, Description, ClientID [ref: dbo.Clients], Location, Status [ref: dbo.ProjectStatusMaster], ProgressPercent, ProjectManagerId [ref: dbo.AspNetUsers], AssignedToUserId [ref: dbo.AspNetUsers], TeamId [ref: dbo.Teams], StartDate, EndDate, Deadline, EstimatedValue, CreatedBy [ref: dbo.AspNetUsers], CreatedDate, PriorityID [ref: dbo.ProjectPriorityMaster], TenantId)" },
            { "ProjectStatusMaster", "dbo.ProjectStatusMaster(StatusID, StatusName)" },
            { "ProjectPriorityMaster", "dbo.ProjectPriorityMaster(PriorityID, PriorityName)" },
            { "AspNetUsers", "dbo.AspNetUsers(Id, FullName, Email, PhoneNumber, IsActive, CreatedAt, TenantId)" },
            { "Teams", "dbo.Teams(Id, Name, ManagerId [ref: dbo.AspNetUsers], IsActive, CreatedAt, TenantId)" },
            { "TeamMembers", "dbo.TeamMembers(Id, TeamId [ref: dbo.Teams], UserId [ref: dbo.AspNetUsers], JoinedAt)" },
            { "States", "dbo.States(StateID, StateName)" },
            { "Cities", "dbo.Cities(CityID, StateID [ref: dbo.States], CityName)" },
            { "UnitTypeMaster", "dbo.UnitTypeMaster(UnitTypeID, UnitName [Values: Page, Design, Banner, Pcs, Set, Roll, Sheet, Hour, Job, Inch, Cm, Sqft, Sqmtr, Pack], Description, Status, CreatedDate)" },
            { "TaskSeries", "dbo.TaskSeries(Id, Title, Description, Notes, IsRecurring, RecurrenceRule, StartDate, EndDate, CreatedBy [ref: dbo.AspNetUsers], TeamId [ref: dbo.Teams], IsActive, CreatedAt, TaskScope, Priority, ProjectId [ref: dbo.Projects])" },
            { "TaskOccurrences", "dbo.TaskOccurrences(Id, TaskSeriesId [ref: dbo.TaskSeries], DueDateTime, StartDateTime, EndDateTime, Status, AssignedTo [ref: dbo.AspNetUsers], CreatedAt)" },
            { "TaskLists", "dbo.TaskLists(Id, Name, OwnerId [ref: dbo.AspNetUsers], CreatedAt)" },
            { "Tenants", "dbo.Tenants(TenantId, CompanyName, IndustryType, CompanyEmail, CompanyPhone, Address, IsApproved, IsActive, CreatedAt)" }
        };

        public static string CRM => GetTables(Tables.Keys);

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
                "TaskLists.OwnerId=AspNetUsers.Id"
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

            return sb.ToString();
        }
    }
}

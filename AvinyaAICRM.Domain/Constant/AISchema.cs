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
            { "Leads", "Leads(LeadID, LeadNo, ClientID, Date, RequirementDetails, LeadSource [nvarchar], OtherSources, Status [nvarchar], CreatedBy, AssignedTo, CreatedDate, IsDeleted, TenantId)" },
            { "LeadFollowups", "LeadFollowups(FollowUpID, LeadID, UpdatedDate, Notes, NextFollowupDate, Status, FollowUpBy, CreatedDate)" },
            { "LeadSourceMaster", "LeadSourceMaster(LeadSourceID, SourceName, IsActive, CreatedDate, SortOrder)" },
            { "LeadStatusMaster", "LeadStatusMaster(LeadStatusID, StatusName, IsActive, CreatedDate, SortOrder)" },
            { "LeadFollowupStatus", "LeadFollowupStatus(LeadFollowupStatusID, StatusName)" },
            { "Clients", "Clients(ClientID, CompanyName, ContactPerson, Mobile, Email, GSTNo, BillingAddress, ClientType, Status, CreatedDate, StateID, CityID, TenantId)" },
            { "Quotations", "Quotations(QuotationID, QuotationNo, ClientID, LeadID, QuotationDate, ValidTill, Status, TotalAmount, Taxes, GrandTotal, CreatedBy, CreatedDate, TenantId)" },
            { "QuotationItems", "QuotationItems(QuotationItemID, QuotationID, ProductID, Description, Quantity, UnitPrice, LineTotal)" },
            { "QuotationStatusMaster", "QuotationStatusMaster(QuotationStatusID, StatusName, IsActive, CreatedDate)" },
            { "Orders", "Orders(OrderID, OrderNo, ClientID, QuotationID, OrderDate, IsDesignByUs, DesigningCharge, ExpectedDeliveryDate, Status, DesignStatus, CreatedBy, AssignedDesignTo, CreatedDate, TenantId, SubTotal, TotalTaxes, GrandTotal)" },
            { "OrderItems", "OrderItems(OrderItemID, OrderID, ProductID, Description, Quantity, UnitPrice, LineTotal)" },
            { "OrderStatusMaster", "OrderStatusMaster(StatusID, StatusName)" },
            { "DesignStatusMaster", "DesignStatusMaster(DesignStatusID, DesignStatusName)" },
            { "Products", "Products(ProductID, ProductName, Category, UnitType, DefaultRate, PurchasePrice, HSNCode, Description, Status, CreatedBy, CreatedDate, TenantId)" },
            { "TaxCategoryMaster", "TaxCategoryMaster(TaxCategoryID, TaxName, Rate, IsCompound)" },
            { "Expenses", "Expenses(ExpenseId, TenantId, ExpenseDate, CategoryId, Amount, PaymentMode, Description, Status, CreatedDate)" },
            { "ExpenseCategories", "ExpenseCategories(CategoryId, CategoryName, IsActive, CreatedDate)" },
            { "Projects", "Projects(ProjectID, TenantId, ProjectName, ClientID, Status, Priority, ProgressPercent, ProjectManagerId, AssignedToUserId, StartDate, EndDate, Deadline, EstimatedValue, CreatedDate)" },
            { "ProjectStatusMaster", "ProjectStatusMaster(StatusID, StatusName)" },
            { "ProjectPriorityMaster", "ProjectPriorityMaster(PriorityID, PriorityName)" },
            { "AspNetUsers", "AspNetUsers(Id, FullName, Email, PhoneNumber, IsActive, TenantId, CreatedAt)" },
            { "Teams", "Teams(Id, Name, TenantId, ManagerId, IsActive, CreatedAt)" },
            { "States", "States(StateID, StateName)" },
            { "Cities", "Cities(CityID, StateID, CityName)" }
        };

        public static string CRM => GetTables(Tables.Keys);

        public static string GetTables(IEnumerable<string> tableNames)
        {
            var systemColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { 
                "IsDeleted", "CreatedBy", "ModifiedBy", "ModifiedDate", 
                "DeletedBy", "DeletedDate", "UpdatedAt", "ConcurrencyStamp", 
                "SecurityStamp", "PasswordHash", "NormalizedUserName", "NormalizedEmail", 
                "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount"
            };

            var sb = new StringBuilder();
            sb.AppendLine("Schema (Table: Cols):");
            foreach (var name in tableNames)
            {
                if (Tables.TryGetValue(name.Trim(), out var schema))
                {
                    // Basic column pruning: Remove common audit/system columns
                    var parts = schema.Split(new[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var tableName = parts[0].Trim();
                    var columns = parts.Skip(1)
                                      .Select(c => c.Trim())
                                      .Where(c => !systemColumns.Contains(c) || 
                                                 c.Contains("Date", StringComparison.OrdinalIgnoreCase) || 
                                                 c.Contains("At", StringComparison.OrdinalIgnoreCase) || 
                                                 c.Equals("Date", StringComparison.OrdinalIgnoreCase))
                                      .ToList();

                    sb.AppendLine($"{tableName}({string.Join(",", columns)})");
                }
            }
            var allRelationships = new[] { 
                "Leads.ClientID=Clients.ClientID", 
                "Leads.Status=LeadStatusMaster.LeadStatusID", 
                "Leads.LeadSource=LeadSourceMaster.LeadSourceID", 
                "Leads.AssignedTo=AspNetUsers.Id", 
                "LeadFollowups.LeadID=Leads.LeadID", 
                "LeadFollowups.Status=LeadFollowupStatus.LeadFollowupStatusID", 
                "LeadFollowups.FollowUpBy=AspNetUsers.Id", 
                "Quotations.LeadID=Leads.LeadID", 
                "Quotations.Status=QuotationStatusMaster.QuotationStatusID",
                "Orders.ClientID=Clients.ClientID", 
                "Orders.Status=OrderStatusMaster.StatusID", 
                "Orders.DesignStatus=DesignStatusMaster.DesignStatusID",
                "Projects.Status=ProjectStatusMaster.StatusID", 
                "Projects.Priority=ProjectPriorityMaster.PriorityID",
                "Projects.ProjectID=Orders.OrderID", 
                "Teams.ManagerId=AspNetUsers.Id" 
            };

            var validRels = allRelationships.Where(rel => {
                // Relationship format: Table1.Col1=Table2.Col2
                var sides = rel.Split('=');
                if (sides.Length != 2) return false;

                var table1 = sides[0].Split('.').FirstOrDefault()?.Trim();
                var table2 = sides[1].Split('.').FirstOrDefault()?.Trim();

                return tableNames.Any(t => t.Equals(table1, StringComparison.OrdinalIgnoreCase)) && 
                       tableNames.Any(t => t.Equals(table2, StringComparison.OrdinalIgnoreCase));
            }).ToList();

            if (validRels.Any())
            {
                sb.AppendLine("\nRel: " + string.Join(", ", validRels));
            }

            return sb.ToString();
        }
    }
}

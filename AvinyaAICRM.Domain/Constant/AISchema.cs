using System.Text.Json;
using System.Text.Json.Serialization;

namespace AvinyaAICRM.Domain.Constant
{
    #region Models

    public class TableSchema
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ColumnSchema> Columns { get; set; } = new();
        public bool HasTenantId { get; set; }
        public string SecurityHint { get; set; } = string.Empty;
    }

    public class ColumnSchema
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public string? ForeignKey { get; set; }
        public string? Description { get; set; }
        public bool IsImportant { get; set; }
    }

    public class RelationshipSchema
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
    }

    public class SemanticMap
    {
        public string Term { get; set; } = string.Empty;
        public string MapsTo { get; set; } = string.Empty;
    }

    public class AiRule
    {
        public string Rule { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class AiExample
    {
        public string Question { get; set; } = string.Empty;
        public string Analysis { get; set; } = string.Empty;
        public string Sql { get; set; } = string.Empty;
    }

    public class AiConstraints
    {
        public int MaxLimit { get; set; } = 50;
        public bool RequireTenantFilter { get; set; } = true;
        public bool AvoidSelectAll { get; set; } = true;
    }

    public class FullAiContext
    {
        public List<TableSchema> Tables { get; set; } = new();
        public List<RelationshipSchema> Relationships { get; set; } = new();
        public List<SemanticMap> SemanticMappings { get; set; } = new();
        public List<AiRule> IntentRules { get; set; } = new();
        public List<AiExample> Examples { get; set; } = new();
        public AiConstraints Constraints { get; set; } = new();
    }

    public class IntentConfig
    {
        public string[] Tables { get; set; } = Array.Empty<string>();
        public List<AiRule> Rules { get; set; } = new();
        public List<AiExample> Examples { get; set; } = new();
    }

    #endregion

    public static class AISchema
    {
        public static string GetContextForIntent(IEnumerable<string> intents)
        {
            var activeIntents = intents.Any() ? intents : new[] { "default" };
            var tables = new HashSet<string>();
            var rules = new List<AiRule>();
            var examples = new List<AiExample>();

            foreach (var intent in activeIntents)
            {
                var config = IntentConfigs.GetValueOrDefault(intent, DefaultConfig);
                foreach (var t in config.Tables) tables.Add(t);
                rules.AddRange(config.Rules);
                examples.AddRange(config.Examples);
            }

            // ─── EXPAND TO RELATIVE TABLES (FKs) ──────────────────────────────────
            var allTables = GetAllTables();
            var expandedTables = new HashSet<string>(tables);
            foreach (var tName in tables)
            {
                var schema = allTables.FirstOrDefault(x => x.Name == tName);
                if (schema == null) continue;
                foreach (var col in schema.Columns)
                {
                    if (!string.IsNullOrEmpty(col.ForeignKey))
                    {
                        var refTable = col.ForeignKey.Split('.')[0];
                        expandedTables.Add(refTable);
                    }
                }
            }

            // ─── MINIFY SCHEMA TO SAVE TOKENS ──────────────────────────────────────
            var finalTables = allTables.Where(t => expandedTables.Contains(t.Name)).ToList();
            
            var minifiedTables = finalTables.Select(t => new
            {
                n = t.Name,
                cols = t.Columns.Where(c => c.IsImportant).Select(c => new
                {
                    n = c.Name,
                    t = c.Type,
                    pk = c.IsPrimary ? (bool?)true : null,
                    fk = c.ForeignKey
                }).ToList()
            }).ToList();

            var minifiedRelationships = GetAllRelationships().Where(r =>
                    expandedTables.Any(t => r.From.StartsWith(t + ".")) &&
                    expandedTables.Any(t => r.To.StartsWith(t + "."))
                ).Select(r => new { f = r.From, t = r.To }).ToList();

            var context = new
            {
                tbls = minifiedTables,
                rels = minifiedRelationships,
                maps = new List<SemanticMap>
                {
                    new() { Term = "customer",    MapsTo = "Clients" },
                    new() { Term = "client",      MapsTo = "Clients" },
                    new() { Term = "lead",        MapsTo = "Leads" },
                    new() { Term = "followup",    MapsTo = "LeadFollowups" },
                    new() { Term = "quotation",   MapsTo = "Quotations" },
                    new() { Term = "quote",       MapsTo = "Quotations" },
                    new() { Term = "order",       MapsTo = "Orders" },
                    new() { Term = "invoice",     MapsTo = "Invoices" },
                    new() { Term = "payment",     MapsTo = "Payments" },
                    new() { Term = "sales",       MapsTo = "Orders + Invoices" },
                    new() { Term = "revenue",     MapsTo = "Invoices.GrandTotal" },
                    new() { Term = "expense",     MapsTo = "Expenses" },
                    new() { Term = "product",     MapsTo = "Products" },
                    new() { Term = "item",        MapsTo = "Products" },
                    new() { Term = "project",     MapsTo = "Projects" },
                    new() { Term = "task",        MapsTo = "TaskSeries + TaskOccurrences" },
                    new() { Term = "team",        MapsTo = "Teams" },
                    new() { Term = "user",        MapsTo = "AspNetUsers" },
                    new() { Term = "role",        MapsTo = "AspNetRoles" },
                    new() { Term = "permission",  MapsTo = "Permissions + RolePermissions + UserPermissions" },
                    new() { Term = "tax",         MapsTo = "TaxCategoryMaster" },
                    new() { Term = "bank",        MapsTo = "BankDetails" },
                    new() { Term = "setting",     MapsTo = "Settings" }
                },
                rules = rules.DistinctBy(r => r.Rule).Select(r => r.Rule).ToList(),
                ex = examples.DistinctBy(e => e.Question).ToList()
            };

            return JsonSerializer.Serialize(context, new JsonSerializerOptions
            {
                WriteIndented = false, // Critical for saving tokens!
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }

        public static HashSet<string> TableNames => GetAllTables().Select(t => t.Name).ToHashSet();

        public static List<TableSchema> GetAllTables() => new()
        {
            // ─── LEADS ───────────────────────────────────────────────────────────────
            new TableSchema {
                Name = "Leads", HasTenantId = true,
                Description = "Primary sales leads with client requirements and status.",
                Columns = new() {
                    new() { Name = "LeadID",        Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "LeadNo",        Type = "varchar(50)",                         IsImportant = true,  Description = "Unique human-readable lead number" },
                    new() { Name = "ClientID",      Type = "uniqueidentifier", ForeignKey = "Clients.ClientID",            IsImportant = true },
                    new() { Name = "LeadStatusID",  Type = "uniqueidentifier", ForeignKey = "LeadStatusMaster.LeadStatusID", IsImportant = true },
                    new() { Name = "LeadSourceID",  Type = "uniqueidentifier", ForeignKey = "LeadSourceMaster.LeadSourceID", IsImportant = true },
                    new() { Name = "Date",          Type = "datetime",                            IsImportant = true },
                    new() { Name = "AssignedTo",    Type = "nvarchar(50)",                        IsImportant = true },
                    new() { Name = "RequirementDetails", Type = "nvarchar(max)",                  IsImportant = false },
                    new() { Name = "Notes",         Type = "nvarchar(100)",                       IsImportant = false },
                    new() { Name = "IsDeleted",     Type = "bit",                                 IsImportant = true,  Description = "Always filter IsDeleted = 0" },
                    new() { Name = "TenantId",      Type = "uniqueidentifier",                    IsImportant = true }
                }
            },

            new TableSchema {
                Name = "LeadFollowups", HasTenantId = false,
                Description = "Interactions and scheduled follow-up dates per lead.",
                SecurityHint = "⚠️ NO TenantId. Always JOIN dbo.Leads ON LeadID to apply TenantId filter.",
                Columns = new() {
                    new() { Name = "FollowUpID",       Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "LeadID",           Type = "uniqueidentifier", ForeignKey = "Leads.LeadID", IsImportant = true },
                    new() { Name = "Status",           Type = "int",              ForeignKey = "LeadFollowupStatus.LeadFollowupStatusID", IsImportant = true, Description = "1=Pending,2=In Progress,3=Completed" },
                    new() { Name = "NextFollowupDate", Type = "datetime",                           IsImportant = true },
                    new() { Name = "Notes",            Type = "nvarchar(max)",                      IsImportant = true },
                    new() { Name = "FollowUpBy",       Type = "nvarchar(50)",                       IsImportant = true },
                    new() { Name = "CreatedDate",      Type = "datetime",                           IsImportant = false }
                }
            },

            new TableSchema {
                Name = "LeadStatusMaster",
                Description = "Lookup for lead statuses (New, Quotation Sent, Converted, etc.).",
                Columns = new() {
                    new() { Name = "LeadStatusID", Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "StatusName",   Type = "nvarchar(100)",                       IsImportant = true },
                    new() { Name = "SortOrder",    Type = "int",                                 IsImportant = false },
                    new() { Name = "IsActive",     Type = "bit",                                 IsImportant = true }
                }
            },

            new TableSchema {
                Name = "LeadSourceMaster",
                Description = "Lookup for lead sources (Call, Walk-in, WhatsApp, Referral, etc.).",
                Columns = new() {
                    new() { Name = "LeadSourceID", Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "SourceName",   Type = "nvarchar(100)",                       IsImportant = true },
                    new() { Name = "IsActive",     Type = "bit",                                 IsImportant = true }
                }
            },

            new TableSchema {
                Name = "LeadFollowupStatus",
                Description = "Lookup table for follow-up statuses (1=Pending, 2=In Progress, 3=Completed).",
                Columns = new() {
                    new() { Name = "LeadFollowupStatusID", Type = "int",          IsPrimary = true,  IsImportant = true },
                    new() { Name = "StatusName",           Type = "nvarchar(100)",                   IsImportant = true }
                }
            },

            // ─── CLIENTS ─────────────────────────────────────────────────────────────
            new TableSchema {
                Name = "Clients", HasTenantId = true,
                Description = "Customer/company profiles.",
                Columns = new() {
                    new() { Name = "ClientID",      Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "CompanyName",   Type = "nvarchar(200)",                       IsImportant = true,  Description = "Primary name of the client" },
                    new() { Name = "ContactPerson", Type = "nvarchar(150)",                       IsImportant = true },
                    new() { Name = "Mobile",        Type = "nvarchar(20)",                        IsImportant = true },
                    new() { Name = "Email",         Type = "nvarchar(150)",                       IsImportant = true },
                    new() { Name = "GSTNo",         Type = "nvarchar(50)",                        IsImportant = false },
                    new() { Name = "ClientType",    Type = "int",                                 IsImportant = true,  Description = "Type of client" },
                    new() { Name = "Status",        Type = "bit",                                 IsImportant = true,  Description = "1=Active, 0=Inactive" },
                    new() { Name = "StateID",       Type = "int",              ForeignKey = "States.StateID",           IsImportant = false },
                    new() { Name = "CityID",        Type = "int",              ForeignKey = "Cities.CityID",            IsImportant = false },
                    new() { Name = "IsDeleted",     Type = "bit",                                 IsImportant = true },
                    new() { Name = "TenantId",      Type = "uniqueidentifier",                    IsImportant = true }
                }
            },

            // ─── QUOTATIONS ──────────────────────────────────────────────────────────
            new TableSchema {
                Name = "Quotations", HasTenantId = true,
                Description = "Sales quotations sent to clients, linked to leads.",
                Columns = new() {
                    new() { Name = "QuotationID",       Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "QuotationNo",       Type = "nvarchar(50)",                        IsImportant = true },
                    new() { Name = "ClientID",          Type = "uniqueidentifier", ForeignKey = "Clients.ClientID",                IsImportant = true },
                    new() { Name = "LeadID",            Type = "uniqueidentifier", ForeignKey = "Leads.LeadID",                    IsImportant = true },
                    new() { Name = "QuotationStatusID", Type = "uniqueidentifier", ForeignKey = "QuotationStatusMaster.QuotationStatusID", IsImportant = true },
                    new() { Name = "QuotationDate",     Type = "datetime2",                           IsImportant = true },
                    new() { Name = "ValidTill",         Type = "datetime2",                           IsImportant = true },
                    new() { Name = "GrandTotal",        Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "Taxes",             Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "TotalAmount",       Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "IsDeleted",         Type = "bit",                                 IsImportant = true },
                    new() { Name = "TenantId",          Type = "uniqueidentifier",                    IsImportant = true }
                }
            },

            new TableSchema {
                Name = "QuotationItems", HasTenantId = false,
                Description = "Line items within a quotation.",
                SecurityHint = "⚠️ NO TenantId. JOIN dbo.Quotations ON QuotationID for tenant security.",
                Columns = new() {
                    new() { Name = "QuotationItemID", Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "QuotationID",     Type = "uniqueidentifier", ForeignKey = "Quotations.QuotationID", IsImportant = true },
                    new() { Name = "ProductID",       Type = "uniqueidentifier", ForeignKey = "Products.ProductID",     IsImportant = true },
                    new() { Name = "Description",     Type = "nvarchar(max)",                        IsImportant = false },
                    new() { Name = "Quantity",        Type = "decimal(18,2)",                        IsImportant = true },
                    new() { Name = "UnitPrice",       Type = "decimal(18,2)",                        IsImportant = true },
                    new() { Name = "LineTotal",       Type = "decimal(18,2)",                        IsImportant = true },
                    new() { Name = "TaxCategoryID",   Type = "uniqueidentifier", ForeignKey = "TaxCategoryMaster.TaxCategoryID", IsImportant = false }
                }
            },

            new TableSchema {
                Name = "QuotationStatusMaster",
                Description = "Lookup for quotation statuses (Sent, Accepted, Rejected).",
                Columns = new() {
                    new() { Name = "QuotationStatusID", Type = "uniqueidentifier", IsPrimary = true, IsImportant = true },
                    new() { Name = "StatusName",        Type = "nvarchar(50)",                       IsImportant = true },
                    new() { Name = "IsActive",          Type = "bit",                                IsImportant = true }
                }
            },

            // ─── ORDERS ──────────────────────────────────────────────────────────────
            new TableSchema {
                Name = "Orders", HasTenantId = true,
                Description = "Confirmed sales orders, optionally linked to quotations.",
                Columns = new() {
                    new() { Name = "OrderID",               Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "OrderNo",               Type = "nvarchar(50)",                        IsImportant = true },
                    new() { Name = "ClientID",              Type = "uniqueidentifier", ForeignKey = "Clients.ClientID",        IsImportant = true },
                    new() { Name = "QuotationID",           Type = "uniqueidentifier", ForeignKey = "Quotations.QuotationID",  IsImportant = false },
                    new() { Name = "Status",                Type = "int",              ForeignKey = "OrderStatusMaster.StatusID", IsImportant = true },
                    new() { Name = "OrderDate",             Type = "datetime",                            IsImportant = true },
                    new() { Name = "ExpectedDeliveryDate",  Type = "datetime",                            IsImportant = true },
                    new() { Name = "GrandTotal",            Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "SubTotal",              Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "TotalTaxes",            Type = "decimal(18,2)",                       IsImportant = false },
                    new() { Name = "AssignedDesignTo",      Type = "nvarchar(50)",                        IsImportant = false },
                    new() { Name = "DesignStatusID",        Type = "int",              ForeignKey = "DesignStatusMaster.DesignStatusID", IsImportant = false },
                    new() { Name = "isInvoiceCreated",      Type = "bit",                                 IsImportant = true },
                    new() { Name = "IsDeleted",             Type = "bit",                                 IsImportant = true },
                    new() { Name = "TenantId",              Type = "uniqueidentifier",                    IsImportant = true }
                }
            },

            new TableSchema {
                Name = "OrderItems", HasTenantId = false,
                Description = "Line items (products) within an order.",
                SecurityHint = "⚠️ NO TenantId. JOIN dbo.Orders ON OrderID for tenant security.",
                Columns = new() {
                    new() { Name = "OrderItemID",   Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "OrderID",       Type = "uniqueidentifier", ForeignKey = "Orders.OrderID",             IsImportant = true },
                    new() { Name = "ProductID",     Type = "uniqueidentifier", ForeignKey = "Products.ProductID",         IsImportant = true },
                    new() { Name = "Quantity",      Type = "int",                                  IsImportant = true },
                    new() { Name = "UnitPrice",     Type = "decimal(18,2)",                        IsImportant = true },
                    new() { Name = "LineTotal",     Type = "decimal(18,2)",                        IsImportant = true },
                    new() { Name = "Description",   Type = "nvarchar(255)",                        IsImportant = false },
                    new() { Name = "TaxCategoryID", Type = "uniqueidentifier", ForeignKey = "TaxCategoryMaster.TaxCategoryID", IsImportant = false }
                }
            },

            new TableSchema {
                Name = "OrderStatusMaster",
                Description = "Lookup for order statuses (1=Pending,2=In Progress,3=Inward Done,4=Ready,5=Delivered).",
                Columns = new() {
                    new() { Name = "StatusID",   Type = "int",          IsPrimary = true, IsImportant = true },
                    new() { Name = "StatusName", Type = "nvarchar(50)",                   IsImportant = true }
                }
            },

            new TableSchema {
                Name = "DesignStatusMaster",
                Description = "Lookup for design statuses (1=Pending,2=In Progress,3=Approved by Client,4=Rejected).",
                Columns = new() {
                    new() { Name = "DesignStatusID",   Type = "int",          IsPrimary = true, IsImportant = true },
                    new() { Name = "DesignStatusName", Type = "nvarchar(50)",                   IsImportant = true }
                }
            },

            // ─── INVOICES & PAYMENTS ─────────────────────────────────────────────────
            new TableSchema {
                Name = "Invoices", HasTenantId = true,
                Description = "Billing records for orders, tracking paid and outstanding amounts.",
                Columns = new() {
                    new() { Name = "InvoiceID",        Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "InvoiceNo",        Type = "nvarchar(50)",                        IsImportant = true },
                    new() { Name = "OrderID",          Type = "nvarchar(max)",                       IsImportant = true,  Description = "References Orders.OrderID (stored as string)" },
                    new() { Name = "ClientID",         Type = "nvarchar(max)",                       IsImportant = true },
                    new() { Name = "InvoiceStatusID",  Type = "int",              ForeignKey = "InvoiceStatuses.InvoiceStatusID", IsImportant = true },
                    new() { Name = "InvoiceDate",      Type = "datetime",                            IsImportant = true },
                    new() { Name = "DueDate",          Type = "datetime",                            IsImportant = true },
                    new() { Name = "GrandTotal",       Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "PaidAmount",       Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "RemainingPayment", Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "OutstandingAmount",Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "SubTotal",         Type = "decimal(18,2)",                       IsImportant = false },
                    new() { Name = "Taxes",            Type = "decimal(18,2)",                       IsImportant = false },
                    new() { Name = "IsDeleted",        Type = "bit",                                 IsImportant = true },
                    new() { Name = "TenantId",         Type = "nvarchar(max)",                       IsImportant = true }
                }
            },

            new TableSchema {
                Name = "InvoiceStatuses",
                Description = "Lookup for invoice statuses (1=Pending, 2=Partial, 3=Receive).",
                Columns = new() {
                    new() { Name = "InvoiceStatusID",   Type = "int",           IsPrimary = true, IsImportant = true },
                    new() { Name = "InvoiceStatusName", Type = "nvarchar(100)",                   IsImportant = true }
                }
            },

            new TableSchema {
                Name = "Payments", HasTenantId = false,
                Description = "Payment receipts against invoices.",
                SecurityHint = "⚠️ NO TenantId. JOIN dbo.Invoices ON InvoiceID for tenant security.",
                Columns = new() {
                    new() { Name = "PaymentID",      Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "InvoiceID",      Type = "uniqueidentifier", ForeignKey = "Invoices.InvoiceID", IsImportant = true },
                    new() { Name = "PaymentDate",    Type = "datetime2",                           IsImportant = true },
                    new() { Name = "Amount",         Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "PaymentMode",    Type = "nvarchar(50)",                        IsImportant = true,  Description = "Cash, Card, UPI, Online" },
                    new() { Name = "TransactionRef", Type = "nvarchar(100)",                       IsImportant = false },
                    new() { Name = "ReceivedBy",     Type = "nvarchar(max)",                       IsImportant = true }
                }
            },

            // ─── EXPENSES ────────────────────────────────────────────────────────────
            new TableSchema {
                Name = "Expenses", HasTenantId = true,
                Description = "Business spending records categorised by type.",
                Columns = new() {
                    new() { Name = "ExpenseId",    Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "CategoryId",   Type = "int",              ForeignKey = "ExpenseCategories.CategoryId", IsImportant = true },
                    new() { Name = "Amount",       Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "ExpenseDate",  Type = "date",                                IsImportant = true },
                    new() { Name = "PaymentMode",  Type = "nvarchar(50)",                        IsImportant = true,  Description = "Cash, Card, UPI, Online" },
                    new() { Name = "Status",       Type = "nvarchar(20)",                        IsImportant = true,  Description = "Paid, Unpaid, Partial" },
                    new() { Name = "Description",  Type = "nvarchar(max)",                       IsImportant = false },
                    new() { Name = "IsDeleted",    Type = "bit",                                 IsImportant = true },
                    new() { Name = "TenantId",     Type = "uniqueidentifier",                    IsImportant = true }
                }
            },

            new TableSchema {
                Name = "ExpenseCategories",
                Description = "Lookup for expense categories (Travel, Food, Office Supplies, Utilities, Software, etc.).",
                Columns = new() {
                    new() { Name = "CategoryId",   Type = "int",           IsPrimary = true, IsImportant = true },
                    new() { Name = "CategoryName", Type = "nvarchar(150)",                   IsImportant = true },
                    new() { Name = "IsActive",     Type = "bit",                             IsImportant = true },
                    new() { Name = "IsDeleted",    Type = "bit",                             IsImportant = true }
                }
            },

            // ─── PRODUCTS ────────────────────────────────────────────────────────────
            new TableSchema {
                Name = "Products", HasTenantId = true,
                Description = "Product/service catalogue with pricing and tax info.",
                Columns = new() {
                    new() { Name = "ProductID",     Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "ProductName",   Type = "nvarchar(200)",                       IsImportant = true },
                    new() { Name = "Category",      Type = "nvarchar(100)",                       IsImportant = true },
                    new() { Name = "DefaultRate",   Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "PurchasePrice", Type = "decimal(18,2)",                       IsImportant = false },
                    new() { Name = "HSNCode",       Type = "nvarchar(50)",                        IsImportant = false },
                    new() { Name = "TaxCategoryID", Type = "uniqueidentifier", ForeignKey = "TaxCategoryMaster.TaxCategoryID", IsImportant = false },
                    new() { Name = "UnitTypeID",    Type = "uniqueidentifier", ForeignKey = "UnitTypeMaster.UnitTypeID",       IsImportant = false },
                    new() { Name = "Status",        Type = "int",                                 IsImportant = true,  Description = "1=Active" },
                    new() { Name = "IsDeleted",     Type = "bit",                                 IsImportant = true },
                    new() { Name = "TenantId",      Type = "uniqueidentifier",                    IsImportant = true }
                }
            },

            new TableSchema {
                Name = "TaxCategoryMaster",
                Description = "Tax rates (GST 5%, 12%, 18%, 28%, IGST, CGST, SGST, compound).",
                Columns = new() {
                    new() { Name = "TaxCategoryID", Type = "uniqueidentifier", IsPrimary = true, IsImportant = true },
                    new() { Name = "TaxName",       Type = "nvarchar(100)",                      IsImportant = true },
                    new() { Name = "Rate",          Type = "decimal(5,2)",                       IsImportant = true },
                    new() { Name = "IsCompound",    Type = "bit",                                IsImportant = false }
                }
            },

            new TableSchema {
                Name = "UnitTypeMaster",
                Description = "Lookup for measurement units (Pcs, Sqft, Sqmtr, Sheet, Roll, etc.).",
                Columns = new() {
                    new() { Name = "UnitTypeID",  Type = "uniqueidentifier", IsPrimary = true, IsImportant = true },
                    new() { Name = "UnitName",    Type = "nvarchar(50)",                       IsImportant = true },
                    new() { Name = "Description", Type = "nvarchar(200)",                      IsImportant = false },
                    new() { Name = "Status",      Type = "bit",                                IsImportant = true }
                }
            },

            // ─── PROJECTS ────────────────────────────────────────────────────────────
            new TableSchema {
                Name = "Projects", HasTenantId = true,
                Description = "Project tracking linked to clients with deadlines and progress.",
                Columns = new() {
                    new() { Name = "ProjectID",         Type = "uniqueidentifier", IsPrimary = true,  IsImportant = true },
                    new() { Name = "ProjectName",       Type = "nvarchar(200)",                       IsImportant = true },
                    new() { Name = "ClientID",          Type = "uniqueidentifier", ForeignKey = "Clients.ClientID",               IsImportant = true },
                    new() { Name = "Status",            Type = "int",              ForeignKey = "ProjectStatusMaster.StatusID",   IsImportant = true },
                    new() { Name = "PriorityID",        Type = "int",              ForeignKey = "ProjectPriorityMaster.PriorityID", IsImportant = true },
                    new() { Name = "ProgressPercent",   Type = "int",                                 IsImportant = true },
                    new() { Name = "StartDate",         Type = "date",                                IsImportant = true },
                    new() { Name = "EndDate",           Type = "date",                                IsImportant = true },
                    new() { Name = "Deadline",          Type = "date",                                IsImportant = true },
                    new() { Name = "EstimatedValue",    Type = "decimal(18,2)",                       IsImportant = true },
                    new() { Name = "ProjectManagerId",  Type = "nvarchar(450)",    ForeignKey = "AspNetUsers.Id",                 IsImportant = false },
                    new() { Name = "AssignedToUserId",  Type = "nvarchar(450)",    ForeignKey = "AspNetUsers.Id",                 IsImportant = false },
                    new() { Name = "TeamId",            Type = "bigint",           ForeignKey = "Teams.Id",                       IsImportant = false },
                    new() { Name = "IsDeleted",         Type = "bit",                                 IsImportant = true },
                    new() { Name = "TenantId",          Type = "uniqueidentifier",                    IsImportant = true }
                }
            },

            new TableSchema {
                Name = "ProjectStatusMaster",
                Description = "Lookup for project statuses (1=Planning, 2=Active, 3=Completed).",
                Columns = new() {
                    new() { Name = "StatusID",   Type = "int",          IsPrimary = true, IsImportant = true },
                    new() { Name = "StatusName", Type = "nvarchar(50)",                   IsImportant = true }
                }
            },

            new TableSchema {
                Name = "ProjectPriorityMaster",
                Description = "Lookup for project priorities (1=Low, 2=Medium, 3=High).",
                Columns = new() {
                    new() { Name = "PriorityID",   Type = "int",          IsPrimary = true, IsImportant = true },
                    new() { Name = "PriorityName", Type = "nvarchar(50)",                   IsImportant = true }
                }
            },

            // ─── TASKS ───────────────────────────────────────────────────────────────
            new TableSchema {
                Name = "TaskSeries", HasTenantId = false,
                Description = "Task definitions (recurring or one-off), linked to lists and optionally projects.",
                SecurityHint = "⚠️ NO TenantId. Filter via TaskLists.OwnerId or Teams.TenantId.",
                Columns = new() {
                    new() { Name = "Id",              Type = "bigint",        IsPrimary = true,  IsImportant = true },
                    new() { Name = "Title",           Type = "nvarchar(255)",                    IsImportant = true },
                    new() { Name = "ListId",          Type = "bigint",        ForeignKey = "TaskLists.Id",     IsImportant = true },
                    new() { Name = "ProjectId",       Type = "uniqueidentifier", ForeignKey = "Projects.ProjectID", IsImportant = true },
                    new() { Name = "IsRecurring",     Type = "bit",                              IsImportant = true },
                    new() { Name = "RecurrenceRule",  Type = "nvarchar(500)",                    IsImportant = false },
                    new() { Name = "Priority",        Type = "varchar(20)",                      IsImportant = true,  Description = "Low, Medium, High" },
                    new() { Name = "TaskScope",       Type = "varchar(20)",                      IsImportant = true,  Description = "Personal or Team" },
                    new() { Name = "StartDate",       Type = "datetime2",                        IsImportant = true },
                    new() { Name = "EndDate",         Type = "datetime2",                        IsImportant = true },
                    new() { Name = "IsActive",        Type = "bit",                              IsImportant = true },
                    new() { Name = "TeamId",          Type = "bigint",        ForeignKey = "Teams.Id",         IsImportant = false }
                }
            },

            new TableSchema {
                Name = "TaskOccurrences", HasTenantId = false,
                Description = "Individual instances of a task series with status, due date, and assignment.",
                SecurityHint = "⚠️ NO TenantId. JOIN dbo.TaskSeries → dbo.TaskLists → OwnerId for user scope.",
                Columns = new() {
                    new() { Name = "Id",            Type = "bigint",       IsPrimary = true, IsImportant = true },
                    new() { Name = "TaskSeriesId",  Type = "bigint",       ForeignKey = "TaskSeries.Id", IsImportant = true },
                    new() { Name = "Status",        Type = "varchar(20)",                   IsImportant = true,  Description = "Pending, Completed, Skipped" },
                    new() { Name = "DueDateTime",   Type = "datetime2",                     IsImportant = true },
                    new() { Name = "StartDateTime", Type = "datetime2",                     IsImportant = false },
                    new() { Name = "EndDateTime",   Type = "datetime2",                     IsImportant = false },
                    new() { Name = "AssignedTo",    Type = "nvarchar(500)",                 IsImportant = true },
                    new() { Name = "CompletedAt",   Type = "datetime2",                     IsImportant = false }
                }
            },

            new TableSchema {
                Name = "TaskLists", HasTenantId = false,
                Description = "Named lists that group tasks, owned by a user or team.",
                Columns = new() {
                    new() { Name = "Id",       Type = "bigint",           IsPrimary = true, IsImportant = true },
                    new() { Name = "Name",     Type = "nvarchar(100)",                      IsImportant = true },
                    new() { Name = "OwnerId",  Type = "uniqueidentifier",                   IsImportant = true },
                    new() { Name = "TeamId",   Type = "bigint",           ForeignKey = "Teams.Id", IsImportant = false }
                }
            },

            new TableSchema {
                Name = "TaskDependencies", HasTenantId = false,
                Description = "Defines prerequisite relationships between task series.",
                Columns = new() {
                    new() { Name = "TaskSeriesId",          Type = "bigint", IsPrimary = true, IsImportant = true, ForeignKey = "TaskSeries.Id" },
                    new() { Name = "DependsOnTaskSeriesId", Type = "bigint", IsPrimary = true, IsImportant = true, ForeignKey = "TaskSeries.Id" }
                }
            },

            new TableSchema {
                Name = "TaskSLARules", HasTenantId = false,
                Description = "SLA duration rules per task series.",
                Columns = new() {
                    new() { Name = "Id",            Type = "bigint", IsPrimary = true, IsImportant = true },
                    new() { Name = "TaskSeriesId",  Type = "bigint", ForeignKey = "TaskSeries.Id", IsImportant = true },
                    new() { Name = "DurationHours", Type = "int",                                  IsImportant = true }
                }
            },

            new TableSchema {
                Name = "TaskSLAStatus", HasTenantId = false,
                Description = "Tracks SLA breach status per task occurrence.",
                Columns = new() {
                    new() { Name = "Id",                Type = "bigint",   IsPrimary = true, IsImportant = true },
                    new() { Name = "TaskOccurrenceId",  Type = "bigint",   ForeignKey = "TaskOccurrences.Id", IsImportant = true },
                    new() { Name = "Deadline",          Type = "datetime2",                                   IsImportant = true },
                    new() { Name = "BreachedAt",        Type = "datetime2",                                   IsImportant = true }
                }
            },

            // ─── TEAMS & USERS ───────────────────────────────────────────────────────
            new TableSchema {
                Name = "Teams", HasTenantId = true,
                Description = "Teams within a tenant managed by a user.",
                Columns = new() {
                    new() { Name = "Id",        Type = "bigint",           IsPrimary = true, IsImportant = true },
                    new() { Name = "Name",      Type = "nvarchar(max)",                      IsImportant = true },
                    new() { Name = "ManagerId", Type = "uniqueidentifier", ForeignKey = "AspNetUsers.Id", IsImportant = true },
                    new() { Name = "IsActive",  Type = "bit",                                IsImportant = true },
                    new() { Name = "TenantId",  Type = "uniqueidentifier",                   IsImportant = true }
                }
            },

            new TableSchema {
                Name = "TeamMembers", HasTenantId = false,
                Description = "Members belonging to a team.",
                SecurityHint = "⚠️ NO TenantId. JOIN dbo.Teams ON TeamId for tenant security.",
                Columns = new() {
                    new() { Name = "Id",       Type = "bigint",           IsPrimary = true, IsImportant = true },
                    new() { Name = "TeamId",   Type = "bigint",           ForeignKey = "Teams.Id",        IsImportant = true },
                    new() { Name = "UserId",   Type = "uniqueidentifier", ForeignKey = "AspNetUsers.Id",  IsImportant = true },
                    new() { Name = "JoinedAt", Type = "datetime2",                                        IsImportant = false }
                }
            },

            new TableSchema {
                Name = "AspNetUsers", HasTenantId = true,
                Description = "Application user accounts with ASP.NET Identity.",
                Columns = new() {
                    new() { Name = "Id",       Type = "nvarchar(450)",    IsPrimary = true, IsImportant = true },
                    new() { Name = "FullName", Type = "nvarchar(max)",                      IsImportant = true },
                    new() { Name = "Email",    Type = "nvarchar(256)",                      IsImportant = true },
                    new() { Name = "IsActive", Type = "bit",                                IsImportant = true },
                    new() { Name = "TenantId", Type = "uniqueidentifier",                   IsImportant = true }
                }
            },

            new TableSchema {
                Name = "AspNetRoles",
                Description = "Application roles (with hierarchy support).",
                Columns = new() {
                    new() { Name = "Id",             Type = "nvarchar(450)", IsPrimary = true, IsImportant = true },
                    new() { Name = "Name",           Type = "nvarchar(256)",                   IsImportant = true },
                    new() { Name = "RoleKey",        Type = "nvarchar(50)",                    IsImportant = true },
                    new() { Name = "HierarchyLevel", Type = "int",                             IsImportant = true },
                    new() { Name = "IsSystemRole",   Type = "bit",                             IsImportant = false }
                }
            },

            new TableSchema {
                Name = "AspNetUserRoles", HasTenantId = false,
                Description = "Many-to-many mapping of users to roles.",
                Columns = new() {
                    new() { Name = "UserId", Type = "nvarchar(450)", IsPrimary = true, IsImportant = true, ForeignKey = "AspNetUsers.Id" },
                    new() { Name = "RoleId", Type = "nvarchar(450)", IsPrimary = true, IsImportant = true, ForeignKey = "AspNetRoles.Id" }
                }
            },

            // ─── PERMISSIONS ─────────────────────────────────────────────────────────
            new TableSchema {
                Name = "Modules",
                Description = "Application modules available for permission control.",
                Columns = new() {
                    new() { Name = "ModuleId",     Type = "int",           IsPrimary = true, IsImportant = true },
                    new() { Name = "ModuleKey",    Type = "nvarchar(50)",                    IsImportant = true },
                    new() { Name = "ModuleName",   Type = "nvarchar(100)",                   IsImportant = true },
                    new() { Name = "DisplayOrder", Type = "int",                             IsImportant = false },
                    new() { Name = "IsActive",     Type = "bit",                             IsImportant = true }
                }
            },

            new TableSchema {
                Name = "Actions",
                Description = "Granular actions (e.g. Create, Read, Update, Delete) for permissions.",
                Columns = new() {
                    new() { Name = "ActionId",   Type = "int",          IsPrimary = true, IsImportant = true },
                    new() { Name = "ActionKey",  Type = "nvarchar(50)",                   IsImportant = true },
                    new() { Name = "ActionName", Type = "nvarchar(50)",                   IsImportant = true }
                }
            },

            new TableSchema {
                Name = "Permissions",
                Description = "Module + Action combinations that form a permission.",
                Columns = new() {
                    new() { Name = "PermissionId", Type = "int", IsPrimary = true, IsImportant = true },
                    new() { Name = "ModuleId",     Type = "int", ForeignKey = "Modules.ModuleId",  IsImportant = true },
                    new() { Name = "ActionId",     Type = "int", ForeignKey = "Actions.ActionId",  IsImportant = true }
                }
            },

            new TableSchema {
                Name = "RolePermissions",
                Description = "Permissions assigned to roles.",
                Columns = new() {
                    new() { Name = "Id",           Type = "int",           IsPrimary = true, IsImportant = true },
                    new() { Name = "RoleId",       Type = "nvarchar(450)", ForeignKey = "AspNetRoles.Id",     IsImportant = true },
                    new() { Name = "PermissionId", Type = "int",           ForeignKey = "Permissions.PermissionId", IsImportant = true }
                }
            },

            new TableSchema {
                Name = "UserPermissions",
                Description = "Permissions explicitly granted to individual users (overrides role permissions).",
                Columns = new() {
                    new() { Name = "Id",              Type = "int",           IsPrimary = true, IsImportant = true },
                    new() { Name = "UserId",          Type = "nvarchar(450)", ForeignKey = "AspNetUsers.Id",        IsImportant = true },
                    new() { Name = "PermissionId",    Type = "int",           ForeignKey = "Permissions.PermissionId", IsImportant = true },
                    new() { Name = "GrantedByUserId", Type = "nvarchar(450)",                                        IsImportant = false },
                    new() { Name = "GrantedAt",       Type = "datetime",                                            IsImportant = false }
                }
            },

            new TableSchema {
                Name = "RoleHierarchyRules",
                Description = "Defines which roles can create/assign other roles.",
                Columns = new() {
                    new() { Name = "Id",               Type = "int",           IsPrimary = true, IsImportant = true },
                    new() { Name = "CreatorRoleId",    Type = "nvarchar(450)", ForeignKey = "AspNetRoles.Id", IsImportant = true },
                    new() { Name = "AssignableRoleId", Type = "nvarchar(450)", ForeignKey = "AspNetRoles.Id", IsImportant = true }
                }
            },

            // ─── NOTIFICATIONS ───────────────────────────────────────────────────────
            new TableSchema {
                Name = "NotificationRules", HasTenantId = false,
                Description = "Rules that define when notifications should fire for task occurrences.",
                Columns = new() {
                    new() { Name = "Id",                Type = "bigint",      IsPrimary = true, IsImportant = true },
                    new() { Name = "TaskOccurrenceId",  Type = "bigint",      ForeignKey = "TaskOccurrences.Id", IsImportant = true },
                    new() { Name = "TriggerType",       Type = "varchar(20)",                                    IsImportant = true,  Description = "Before, After" },
                    new() { Name = "OffsetMinutes",     Type = "int",                                            IsImportant = true },
                    new() { Name = "Channel",           Type = "varchar(20)",                                    IsImportant = true,  Description = "Email, SMS, Push" }
                }
            },

            new TableSchema {
                Name = "NotificationLogs", HasTenantId = false,
                Description = "Log of sent notifications.",
                Columns = new() {
                    new() { Name = "Id",                 Type = "bigint",     IsPrimary = true, IsImportant = true },
                    new() { Name = "NotificationRuleId", Type = "bigint",     ForeignKey = "NotificationRules.Id", IsImportant = true },
                    new() { Name = "SentAt",             Type = "datetime2",                                       IsImportant = true },
                    new() { Name = "Status",             Type = "varchar(20)",                                     IsImportant = true }
                }
            },

            // ─── INFRASTRUCTURE / MISC ───────────────────────────────────────────────
            new TableSchema {
                Name = "Tenants",
                Description = "Registered companies/organisations using the CRM (multi-tenant root).",
                Columns = new() {
                    new() { Name = "TenantId",     Type = "uniqueidentifier", IsPrimary = true, IsImportant = true },
                    new() { Name = "CompanyName",  Type = "nvarchar(200)",                      IsImportant = true },
                    new() { Name = "IsApproved",   Type = "bit",                                IsImportant = true },
                    new() { Name = "IsActive",     Type = "bit",                                IsImportant = true },
                    new() { Name = "CreatedAt",    Type = "datetime",                           IsImportant = false }
                }
            },

            new TableSchema {
                Name = "BankDetails", HasTenantId = true,
                Description = "Company bank account details per tenant.",
                Columns = new() {
                    new() { Name = "BankAccountId",      Type = "uniqueidentifier", IsPrimary = true, IsImportant = true },
                    new() { Name = "BankName",           Type = "nvarchar(100)",                      IsImportant = true },
                    new() { Name = "AccountHolderName",  Type = "nvarchar(150)",                      IsImportant = true },
                    new() { Name = "AccountNumber",      Type = "nvarchar(30)",                       IsImportant = true },
                    new() { Name = "IFSCCode",           Type = "nvarchar(20)",                       IsImportant = true },
                    new() { Name = "IsActive",           Type = "bit",                                IsImportant = true },
                    new() { Name = "TenantId",           Type = "nvarchar(max)",                      IsImportant = true }
                }
            },

            new TableSchema {
                Name = "Settings", HasTenantId = true,
                Description = "Tenant-specific configuration values (prefixes, digits, etc.).",
                Columns = new() {
                    new() { Name = "SettingID",   Type = "uniqueidentifier", IsPrimary = true, IsImportant = true },
                    new() { Name = "EntityType",  Type = "nvarchar(50)",                       IsImportant = true,  Description = "e.g. Invoice, Order, Lead" },
                    new() { Name = "Value",       Type = "nvarchar(max)",                      IsImportant = true },
                    new() { Name = "PreFix",      Type = "varchar(50)",                        IsImportant = false },
                    new() { Name = "Digits",      Type = "int",                                IsImportant = false },
                    new() { Name = "TenantId",    Type = "nvarchar(max)",                      IsImportant = true }
                }
            },

            new TableSchema {
                Name = "States",
                Description = "Indian states lookup.",
                Columns = new() {
                    new() { Name = "StateID",   Type = "int",          IsPrimary = true, IsImportant = true },
                    new() { Name = "StateName", Type = "varchar(100)",                   IsImportant = true }
                }
            },

            new TableSchema {
                Name = "Cities",
                Description = "Cities lookup linked to states.",
                Columns = new() {
                    new() { Name = "CityID",   Type = "int",          IsPrimary = true, IsImportant = true },
                    new() { Name = "StateID",  Type = "int",          ForeignKey = "States.StateID", IsImportant = true },
                    new() { Name = "CityName", Type = "varchar(100)",                               IsImportant = true }
                }
            }
        };

        private static List<RelationshipSchema> GetAllRelationships() => new()
        {
            // Leads
            new() { From = "Leads.ClientID",         To = "Clients.ClientID" },
            new() { From = "Leads.LeadStatusID",     To = "LeadStatusMaster.LeadStatusID" },
            new() { From = "Leads.LeadSourceID",     To = "LeadSourceMaster.LeadSourceID" },

            // Lead Followups
            new() { From = "LeadFollowups.LeadID",   To = "Leads.LeadID" },
            new() { From = "LeadFollowups.Status",   To = "LeadFollowupStatus.LeadFollowupStatusID" },

            // Quotations
            new() { From = "Quotations.ClientID",          To = "Clients.ClientID" },
            new() { From = "Quotations.LeadID",            To = "Leads.LeadID" },
            new() { From = "Quotations.QuotationStatusID", To = "QuotationStatusMaster.QuotationStatusID" },
            new() { From = "QuotationItems.QuotationID",   To = "Quotations.QuotationID" },
            new() { From = "QuotationItems.ProductID",     To = "Products.ProductID" },
            new() { From = "QuotationItems.TaxCategoryID", To = "TaxCategoryMaster.TaxCategoryID" },

            // Orders
            new() { From = "Orders.ClientID",         To = "Clients.ClientID" },
            new() { From = "Orders.QuotationID",      To = "Quotations.QuotationID" },
            new() { From = "Orders.Status",           To = "OrderStatusMaster.StatusID" },
            new() { From = "Orders.DesignStatusID",   To = "DesignStatusMaster.DesignStatusID" },
            new() { From = "OrderItems.OrderID",      To = "Orders.OrderID" },
            new() { From = "OrderItems.ProductID",    To = "Products.ProductID" },
            new() { From = "OrderItems.TaxCategoryID",To = "TaxCategoryMaster.TaxCategoryID" },

            // Invoices & Payments
            new() { From = "Invoices.InvoiceStatusID", To = "InvoiceStatuses.InvoiceStatusID" },
            new() { From = "Payments.InvoiceID",       To = "Invoices.InvoiceID" },

            // Expenses
            new() { From = "Expenses.CategoryId",     To = "ExpenseCategories.CategoryId" },

            // Products
            new() { From = "Products.TaxCategoryID",  To = "TaxCategoryMaster.TaxCategoryID" },
            new() { From = "Products.UnitTypeID",     To = "UnitTypeMaster.UnitTypeID" },

            // Projects
            new() { From = "Projects.ClientID",       To = "Clients.ClientID" },
            new() { From = "Projects.Status",         To = "ProjectStatusMaster.StatusID" },
            new() { From = "Projects.PriorityID",     To = "ProjectPriorityMaster.PriorityID" },
            new() { From = "Projects.TeamId",         To = "Teams.Id" },

            // Tasks
            new() { From = "TaskSeries.ListId",                    To = "TaskLists.Id" },
            new() { From = "TaskSeries.ProjectId",                 To = "Projects.ProjectID" },
            new() { From = "TaskSeries.TeamId",                    To = "Teams.Id" },
            new() { From = "TaskOccurrences.TaskSeriesId",         To = "TaskSeries.Id" },
            new() { From = "TaskDependencies.TaskSeriesId",        To = "TaskSeries.Id" },
            new() { From = "TaskDependencies.DependsOnTaskSeriesId", To = "TaskSeries.Id" },
            new() { From = "TaskSLARules.TaskSeriesId",            To = "TaskSeries.Id" },
            new() { From = "TaskSLAStatus.TaskOccurrenceId",       To = "TaskOccurrences.Id" },
            new() { From = "TaskLists.TeamId",                     To = "Teams.Id" },

            // Notifications
            new() { From = "NotificationRules.TaskOccurrenceId",   To = "TaskOccurrences.Id" },
            new() { From = "NotificationLogs.NotificationRuleId",  To = "NotificationRules.Id" },

            // Teams & Users
            new() { From = "Teams.ManagerId",       To = "AspNetUsers.Id" },
            new() { From = "TeamMembers.TeamId",    To = "Teams.Id" },
            new() { From = "TeamMembers.UserId",    To = "AspNetUsers.Id" },
            new() { From = "AspNetUserRoles.UserId",To = "AspNetUsers.Id" },
            new() { From = "AspNetUserRoles.RoleId",To = "AspNetRoles.Id" },

            // Permissions
            new() { From = "Permissions.ModuleId",          To = "Modules.ModuleId" },
            new() { From = "Permissions.ActionId",          To = "Actions.ActionId" },
            new() { From = "RolePermissions.RoleId",        To = "AspNetRoles.Id" },
            new() { From = "RolePermissions.PermissionId",  To = "Permissions.PermissionId" },
            new() { From = "UserPermissions.UserId",        To = "AspNetUsers.Id" },
            new() { From = "UserPermissions.PermissionId",  To = "Permissions.PermissionId" },
            new() { From = "RoleHierarchyRules.CreatorRoleId",   To = "AspNetRoles.Id" },
            new() { From = "RoleHierarchyRules.AssignableRoleId",To = "AspNetRoles.Id" },

            // Geo
            new() { From = "Cities.StateID",   To = "States.StateID" },
            new() { From = "Clients.StateID",  To = "States.StateID" },
            new() { From = "Clients.CityID",   To = "Cities.CityID" }
        };

        private static readonly Dictionary<string, IntentConfig> IntentConfigs = new()
        {
            { "query_clients", new IntentConfig {
                Tables = new[] { "Clients", "Leads" },
                Rules = new() {
                    new() { Rule = "Filter IsDeleted = 0 on Clients", Type = "filter" }
                },
                Examples = new() {
                    new() {
                        Question = "give client with no lead",
                        Analysis = "User wants Clients who have NO corresponding records in Leads. Use LEFT JOIN on Leads and filter where Leads.LeadID IS NULL. Apply TenantId filter on Clients.",
                        Sql = "SELECT c.CompanyName, c.ContactPerson, c.Mobile FROM dbo.Clients c LEFT JOIN dbo.Leads l ON c.ClientID = l.ClientID WHERE c.TenantId = @TenantId AND c.IsDeleted = 0 AND l.LeadID IS NULL"
                    }
                }
            }},

            { "query_leads", new IntentConfig {
                Tables = new[] { "Leads", "Clients", "LeadStatusMaster", "LeadSourceMaster" },
                Rules = new() {
                    new() { Rule = "Always JOIN Clients for CompanyName", Type = "join" },
                    new() { Rule = "Always JOIN LeadStatusMaster for StatusName", Type = "join" },
                    new() { Rule = "Filter IsDeleted = 0 on Leads", Type = "filter" }
                },
                Examples = new() {
                    new() {
                        Question = "Show all leads with client name and status",
                        Analysis = "Join Leads → Clients (CompanyName and ContactPerson) and LeadStatusMaster (StatusName). Filter TenantId and IsDeleted.",
                        Sql = "SELECT TOP 50 l.LeadNo, COALESCE(NULLIF(c.CompanyName, ''), c.ContactPerson, 'Unknown') AS ClientName, s.StatusName, l.Date FROM dbo.Leads l JOIN dbo.Clients c ON l.ClientID = c.ClientID JOIN dbo.LeadStatusMaster s ON l.LeadStatusID = s.LeadStatusID WHERE l.TenantId = @TenantId AND l.IsDeleted = 0 ORDER BY l.Date DESC"
                    },
                    new() {
                        Question = "How many new leads this month?",
                        Analysis = "Filter Leads by TenantId, current month date range, and LeadStatusMaster where StatusName = 'New'.",
                        Sql = "SELECT COUNT(*) AS NewLeadCount FROM dbo.Leads l JOIN dbo.LeadStatusMaster s ON l.LeadStatusID = s.LeadStatusID WHERE l.TenantId = @TenantId AND l.IsDeleted = 0 AND s.StatusName = 'New' AND MONTH(l.Date) = MONTH(GETDATE()) AND YEAR(l.Date) = YEAR(GETDATE())"
                    }
                }
            }},

            { "query_followups", new IntentConfig {
                Tables = new[] { "LeadFollowups", "Leads", "Clients", "LeadFollowupStatus" },
                Rules = new() {
                    new() { Rule = "JOIN Leads to apply TenantId security (LeadFollowups has no TenantId)", Type = "security" },
                    new() { Rule = "JOIN LeadFollowupStatus for StatusName", Type = "join" }
                },
                Examples = new() {
                    new() {
                        Question = "Show pending followups for today",
                        Analysis = "Join LeadFollowups → Leads (TenantId) → Clients (CompanyName). Filter Status=1 (Pending) and today's date.",
                        Sql = "SELECT lf.NextFollowupDate, fs.StatusName, l.LeadNo, c.CompanyName, lf.Notes FROM dbo.LeadFollowups lf JOIN dbo.Leads l ON lf.LeadID = l.LeadID JOIN dbo.Clients c ON l.ClientID = c.ClientID JOIN dbo.LeadFollowupStatus fs ON lf.Status = fs.LeadFollowupStatusID WHERE l.TenantId = @TenantId AND lf.Status = 1 AND CAST(lf.NextFollowupDate AS DATE) = CAST(GETDATE() AS DATE) ORDER BY lf.NextFollowupDate"
                    }
                }
            }},

            { "query_orders", new IntentConfig {
                Tables = new[] { "Orders", "Clients", "OrderStatusMaster", "OrderItems", "Products" },
                Rules = new() {
                    new() { Rule = "Filter IsDeleted = 0 on Orders", Type = "filter" },
                    new() { Rule = "JOIN OrderStatusMaster for human-readable status", Type = "join" }
                },
                Examples = new() {
                    new() {
                        Question = "Show all pending orders with client names",
                        Analysis = "Join Orders → Clients → OrderStatusMaster. Filter TenantId, IsDeleted=0, Status=1.",
                        Sql = "SELECT o.OrderNo, c.CompanyName, s.StatusName, o.GrandTotal, o.OrderDate FROM dbo.Orders o JOIN dbo.Clients c ON o.ClientID = c.ClientID JOIN dbo.OrderStatusMaster s ON o.Status = s.StatusID WHERE o.TenantId = @TenantId AND o.IsDeleted = 0 AND o.Status = 1 ORDER BY o.OrderDate DESC"
                    }
                }
            }},

            { "query_invoices", new IntentConfig {
                Tables = new[] { "Invoices", "InvoiceStatuses", "Payments" },
                Rules = new() {
                    new() { Rule = "Filter IsDeleted = 0 on Invoices", Type = "filter" },
                    new() { Rule = "TenantId in Invoices is nvarchar — use CAST or direct compare", Type = "naming" }
                },
                Examples = new() {
                    new() {
                        Question = "Show outstanding invoices",
                        Analysis = "Filter Invoices where OutstandingAmount > 0 and IsDeleted = 0.",
                        Sql = "SELECT i.InvoiceNo, i.GrandTotal, i.PaidAmount, i.OutstandingAmount, i.DueDate, s.InvoiceStatusName FROM dbo.Invoices i JOIN dbo.InvoiceStatuses s ON i.InvoiceStatusID = s.InvoiceStatusID WHERE CAST(i.TenantId AS nvarchar(max)) = CAST(@TenantId AS nvarchar(max)) AND i.IsDeleted = 0 AND i.OutstandingAmount > 0 ORDER BY i.DueDate ASC"
                    }
                }
            }},

            { "query_expenses", new IntentConfig {
                Tables = new[] { "Expenses", "ExpenseCategories" },
                Rules = new() {
                    new() { Rule = "Always JOIN ExpenseCategories for CategoryName", Type = "join" },
                    new() { Rule = "Filter IsDeleted = 0", Type = "filter" }
                },
                Examples = new() {
                    new() {
                        Question = "Total expenses by category this month",
                        Analysis = "Group Expenses by CategoryId, join ExpenseCategories. Filter TenantId, IsDeleted=0, and current month.",
                        Sql = "SELECT ec.CategoryName, SUM(e.Amount) AS TotalAmount FROM dbo.Expenses e JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId WHERE e.TenantId = @TenantId AND e.IsDeleted = 0 AND MONTH(e.ExpenseDate) = MONTH(GETDATE()) AND YEAR(e.ExpenseDate) = YEAR(GETDATE()) GROUP BY ec.CategoryName ORDER BY TotalAmount DESC"
                    }
                }
            }},

            { "query_projects", new IntentConfig {
                Tables = new[] { "Projects", "Clients", "ProjectStatusMaster", "ProjectPriorityMaster", "Teams" },
                Rules = new() {
                    new() { Rule = "Filter IsDeleted = 0 on Projects", Type = "filter" },
                    new() { Rule = "JOIN ProjectStatusMaster and ProjectPriorityMaster for labels", Type = "join" }
                },
                Examples = new() {
                    new() {
                        Question = "List active projects with client and priority",
                        Analysis = "Join Projects → Clients → ProjectStatusMaster → ProjectPriorityMaster. Filter Status=2 (Active).",
                        Sql = "SELECT p.ProjectName, c.CompanyName, ps.StatusName, pp.PriorityName, p.ProgressPercent, p.Deadline FROM dbo.Projects p JOIN dbo.Clients c ON p.ClientID = c.ClientID JOIN dbo.ProjectStatusMaster ps ON p.Status = ps.StatusID JOIN dbo.ProjectPriorityMaster pp ON p.PriorityID = pp.PriorityID WHERE p.TenantId = @TenantId AND p.IsDeleted = 0 AND p.Status = 2 ORDER BY p.Deadline ASC"
                    }
                }
            }},

            { "query_tasks", new IntentConfig {
                Tables = new[] { "TaskSeries", "TaskOccurrences", "TaskLists", "Projects" },
                Rules = new() {
                    new() { Rule = "No TenantId on tasks — scope by TaskLists.OwnerId = @UserId or TeamId", Type = "security" },
                    new() { Rule = "TaskOccurrences.Status values: Pending, Completed, Skipped", Type = "filter" }
                },
                Examples = new() {
                    new() {
                        Question = "Show my pending tasks due today",
                        Analysis = "Join TaskOccurrences → TaskSeries → TaskLists. Filter AssignedTo = @UserId, Status='Pending', DueDateTime = today.",
                        Sql = "SELECT ts.Title, to2.DueDateTime, to2.Status FROM dbo.TaskOccurrences to2 JOIN dbo.TaskSeries ts ON to2.TaskSeriesId = ts.Id JOIN dbo.TaskLists tl ON ts.ListId = tl.Id WHERE to2.AssignedTo = @UserId AND to2.Status = 'Pending' AND CAST(to2.DueDateTime AS DATE) = CAST(GETDATE() AS DATE) ORDER BY to2.DueDateTime"
                    }
                }
            }},

            { "query_users", new IntentConfig {
                Tables = new[] { "AspNetUsers", "AspNetRoles", "AspNetUserRoles" },
                Rules = new() {
                    new() { Rule = "Filter by TenantId on AspNetUsers", Type = "security" },
                    new() { Rule = "JOIN AspNetUserRoles + AspNetRoles to get role names", Type = "join" }
                },
                Examples = new() {
                    new() {
                        Question = "List all active users with their roles",
                        Analysis = "Join AspNetUsers → AspNetUserRoles → AspNetRoles. Filter TenantId and IsActive=1.",
                        Sql = "SELECT u.FullName, u.Email, r.Name AS RoleName FROM dbo.AspNetUsers u JOIN dbo.AspNetUserRoles ur ON u.Id = ur.UserId JOIN dbo.AspNetRoles r ON ur.RoleId = r.Id WHERE u.TenantId = @TenantId AND u.IsActive = 1 ORDER BY u.FullName"
                    }
                }
            }}
        };

        private static readonly IntentConfig DefaultConfig = new IntentConfig {
            Tables = new[] {
                "Leads", "Clients", "LeadFollowups", "LeadStatusMaster", "LeadSourceMaster",
                "Quotations", "Orders", "Invoices", "Expenses", "ExpenseCategories",
                "Products", "Projects", "TaskSeries", "TaskOccurrences", "Teams", "AspNetUsers"
            }
        };
    }
}
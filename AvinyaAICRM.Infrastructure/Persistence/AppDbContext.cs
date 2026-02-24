using AvinyaAICRM.Domain.Entities;
using AvinyaAICRM.Domain.Entities.Action;
using AvinyaAICRM.Domain.Entities.City;
using AvinyaAICRM.Domain.Entities.Client;
using AvinyaAICRM.Domain.Entities.ErrorLogs;
using AvinyaAICRM.Domain.Entities.Leads;
using AvinyaAICRM.Domain.Entities.Master;
using AvinyaAICRM.Domain.Entities.Module;
using AvinyaAICRM.Domain.Entities.Orders;
using AvinyaAICRM.Domain.Entities.Permission;
using AvinyaAICRM.Domain.Entities.Product;
using AvinyaAICRM.Domain.Entities.Quotations;
using AvinyaAICRM.Domain.Entities.State;
using AvinyaAICRM.Domain.Entities.Tasks;
using AvinyaAICRM.Domain.Entities.TaxCategory;
using AvinyaAICRM.Domain.Entities.Team;
using AvinyaAICRM.Domain.Entities.TeamMember;
using AvinyaAICRM.Domain.Entities.Tenant;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<ErrorLogs> ErrorLogs { get; set; }
        public DbSet<Tenant> Tenants { get; set; }

        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<ActionMaster> Actions { get; set; }
        public DbSet<TaskList> TaskLists { get; set; }
        public DbSet<TaskSeries> TaskSeries { get; set; }
        public DbSet<TaskOccurrence> TaskOccurrences { get; set; }
        public DbSet<NotificationRule> NotificationRules { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<LeadFollowups> LeadFollowups { get; set; }
        public DbSet<LeadFollowupStatus> LeadFollowupStatuses { get; set; }
        public DbSet<LeadSourceMaster> leadSourceMasters { get; set; }
        public DbSet<LeadStatusMaster> leadStatusMasters { get; set; }
        public DbSet<Quotation> Quotations { get; set; }
        public DbSet<QuotationItem> QuotationItems { get; set; }
        public DbSet<QuotationStatusMaster> QuotationStatusMaster { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderStatusMaster> OrderStatusMasters { get; set; }
        public DbSet<DesignStatusMaster> DesignStatusMasters { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Cities> Cities { get; set; }
        public DbSet<States> States { get; set; }
        public DbSet<UnitType> UnitTypeMasters { get; set; }
        public DbSet<TaxCategoryMaster> TaxCategoryMasters { get; set; }
        public DbSet<Product> Products { get; set; }
    }
}

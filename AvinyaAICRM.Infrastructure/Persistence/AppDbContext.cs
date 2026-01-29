using AvinyaAICRM.Domain.Entities.Action;
using AvinyaAICRM.Domain.Entities.ErrorLogs;
using AvinyaAICRM.Domain.Entities.Module;
using AvinyaAICRM.Domain.Entities.Permission;
using AvinyaAICRM.Domain.Entities.Tasks;
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


    }
}

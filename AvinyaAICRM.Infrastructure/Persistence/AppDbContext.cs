using AvinyaAICRM.Domain.Entities.ErrorLogs;
using AvinyaAICRM.Domain.Entities.Tenant;
using AvinyaAICRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<ErrorLogs> ErrorLogs { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
    }
}

using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tenant;
using AvinyaAICRM.Infrastructure.Persistence;

namespace AvinyaAICRM.Infrastructure.Repositories.Tenant
{
    public class TenantRepository : ITenantRepository
    {
        private readonly AppDbContext _context;

        public TenantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AvinyaAICRM.Domain.Entities.Tenant.Tenant> CreateTenantAsync(AvinyaAICRM.Domain.Entities.Tenant.Tenant tenant)
        {
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
            return tenant;
        }

        public async Task<AvinyaAICRM.Domain.Entities.Tenant.Tenant?> GetByIdAsync(Guid tenantId)
            => await _context.Tenants.FindAsync(tenantId);

        public async Task ApproveTenantAsync(AvinyaAICRM.Domain.Entities.Tenant.Tenant tenant)
        {
            tenant.IsApproved = true;
            tenant.IsActive = true;
            tenant.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(AvinyaAICRM.Domain.Entities.Tenant.Tenant tenant)
        {
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();
        }
    }

}

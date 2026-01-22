
namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tenant
{
    public interface ITenantRepository
    {
        Task<AvinyaAICRM.Domain.Entities.Tenant.Tenant> CreateTenantAsync(AvinyaAICRM.Domain.Entities.Tenant.Tenant tenant);
        Task<AvinyaAICRM.Domain.Entities.Tenant.Tenant?> GetByIdAsync(Guid tenantId);
        Task ApproveTenantAsync(AvinyaAICRM.Domain.Entities.Tenant.Tenant tenant);
        Task UpdateAsync(AvinyaAICRM.Domain.Entities.Tenant.Tenant tenant);
    }
}

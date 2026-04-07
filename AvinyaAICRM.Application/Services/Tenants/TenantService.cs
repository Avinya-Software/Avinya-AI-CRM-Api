using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Team;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tenant;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tenants;
using AvinyaAICRM.Domain.Entities.Tenant;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;


namespace AvinyaAICRM.Application.Services.Tenants
{
    public class TenantService : ITenantService
    {
        private readonly ITenantRepository _tenantRepository;
        public TenantService(ITenantRepository tenantRepository) 
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<ResponseModel> GetByIdAsync(Guid tenantId)
        {
            var result = await _tenantRepository.GetByIdAsync(tenantId);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> UpdateAsync(AvinyaAICRM.Domain.Entities.Tenant.Tenant tenant)
        {
            var result = await _tenantRepository.UpdateAsync(tenant);
            return CommonHelper.GetResponseMessage(result);
        }
    }
}

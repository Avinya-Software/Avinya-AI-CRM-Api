using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tenant;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.SuperAdmin;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;

public class SuperAdminService : ISuperAdminService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;

    public SuperAdminService(
        ITenantRepository tenantRepository,
        IUserRepository userRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
    }

    public async Task<ResponseModel> ApproveAdminAsync(Guid tenantId)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null)
        {
            return CommonHelper.BadRequestResponseMessage("Tenant not found");
        }

        if (tenant.IsApproved)
        {
            return CommonHelper.BadRequestResponseMessage("Tenant already approved");
        }

        // Approve tenant
        tenant.IsApproved = true;
        tenant.IsActive = true;
        tenant.ApprovedAt = DateTime.UtcNow;

        await _tenantRepository.UpdateAsync(tenant);

        // Activate Admin user linked to tenant
        var adminUser = await _userRepository.GetAdminByTenantIdAsync(tenantId);
        if (adminUser == null)
        {
            return CommonHelper.BadRequestResponseMessage("Admin user not found for this tenant");
        }

        adminUser.IsActive = true;
        await _userRepository.UpdateAsync(adminUser);

        return CommonHelper.SuccessResponseMessage("Admin approved successfully",null);
    }
}

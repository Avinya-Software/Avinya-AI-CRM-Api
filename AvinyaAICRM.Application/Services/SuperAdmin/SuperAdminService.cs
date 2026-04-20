using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Settings;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tenant;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.SuperAdmin;
using AvinyaAICRM.Domain.Entities;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SuperAdminService : ISuperAdminService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUserCreditRepository _userCreditRepository;
    private readonly IConfiguration _configuration;

    public SuperAdminService(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        ISettingsRepository settingsRepository,
        IUserCreditRepository userCreditRepository,
        IConfiguration configuration)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _settingsRepository = settingsRepository;
        _userCreditRepository = userCreditRepository;
        _configuration = configuration;
    }

    private string GetFinancialYear()
    {
        var now = DateTime.Now;

        int startYear = now.Month >= 4 ? now.Year : now.Year - 1;
        int endYear = startYear + 1;

        return $"{startYear}-{endYear.ToString().Substring(2)}";
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

        // Activate Admin user linked to tenant
        var adminUser = await _userRepository.GetAdminByTenantIdAsync(tenantId);
        if (adminUser == null)
        {
            return CommonHelper.BadRequestResponseMessage("Admin user not found for this tenant");
        }

        adminUser.IsActive = true;
        tenant.ApprovedBySuperAdminId = adminUser.Id;

        await _tenantRepository.UpdateAsync(tenant);
        await _userRepository.UpdateAsync(adminUser);

        var fy = GetFinancialYear();

        var defaultSettings = new List<Setting>
        {
            new Setting { EntityType = "WorkOrderSecond", Value = "0" , TenantId = tenantId.ToString() },
            new Setting { EntityType = "PaymentQR", Value = "0", TenantId = tenantId.ToString() },
            new Setting { EntityType = "FollowUp", Value = "0", TenantId = tenantId.ToString() },
            new Setting { EntityType = "WorkOrderFirst", Value = "0", TenantId = tenantId.ToString() },
            new Setting { EntityType = "InvoiceNo", Value = $"{{'FinancialYear':'{fy}','LastNumber':0}}",PreFix = "IN-NO",Digits=4, TenantId = tenantId.ToString() },
            new Setting { EntityType = "TermsAndConditions", Value = "These are the default terms and conditions For your company. You can update this text from the admin panel.", TenantId = tenantId.ToString() },
            new Setting { EntityType = "QuotationNo", Value = $"{{'FinancialYear':'{fy}','LastNumber':0}}",PreFix = "Q-NO",Digits=4, TenantId = tenantId.ToString() },
            new Setting { EntityType = "OrderNo", Value = $"{{'FinancialYear':'{fy}','LastNumber':0}}",PreFix = "O-NO",Digits=4, TenantId = tenantId.ToString() },
            new Setting { EntityType = "PaymentUPIId", Value = "", TenantId = tenantId.ToString() },
            new Setting { EntityType = "LeadNo", Value = $"{{'FinancialYear':'{fy}','LastNumber':0}}",PreFix = "L-NO",Digits=4, TenantId = tenantId.ToString() }
        };

        await _settingsRepository.CreateSettingsAsync(defaultSettings);

        // Grant signup credits from configuration
        int signupCredit = 0;
        var scVal = _configuration["SignupCredit"];
        if (!string.IsNullOrEmpty(scVal) && int.TryParse(scVal, out var parsed))
            signupCredit = parsed;
        if (signupCredit > 0)
        {
            var userCredit = await _userCreditRepository.GetByUserIdAsync(adminUser.Id);
            if (userCredit == null)
            {
                var uc = new UserCredit
                {
                    UserId = adminUser.Id,
                    TenantId = tenantId,
                    Balance = signupCredit,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _userCreditRepository.AddUserCreditAsync(uc);
                await _userCreditRepository.AddTransactionAsync(new CreditTransaction
                {
                    UserCreditId = uc.Id,
                    Amount = signupCredit,
                    Action = "SignupCredit",
                    Description = "Signup credit on approval",
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                userCredit.Balance += signupCredit;
                userCredit.UpdatedAt = DateTime.UtcNow;
                await _userCreditRepository.UpdateUserCreditAsync(userCredit);
                await _userCreditRepository.AddTransactionAsync(new CreditTransaction
                {
                    UserCreditId = userCredit.Id,
                    Amount = signupCredit,
                    Action = "SignupCredit",
                    Description = "Signup credit added on approval",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        return CommonHelper.SuccessResponseMessage("Admin approved successfully", null);
    }
}

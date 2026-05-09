using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Permission;
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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.EmailService;
using AvinyaAICRM.Application.DTOs.EmailSetting;

public class SuperAdminService : ISuperAdminService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUserCreditRepository _userCreditRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;

    public SuperAdminService(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        ISettingsRepository settingsRepository,
        IUserCreditRepository userCreditRepository,
        IPermissionRepository permissionRepository,
        IUserPermissionRepository userPermissionRepository,
        IConfiguration configuration,
        IEmailService emailService,
        IOptions<EmailSettings> emailSettings)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _settingsRepository = settingsRepository;
        _userCreditRepository = userCreditRepository;
        _permissionRepository = permissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _configuration = configuration;
        _emailService = emailService;
        _emailSettings = emailSettings.Value;
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
        tenant.ApprovedAt = DateTime.Now;

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
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _userCreditRepository.AddUserCreditAsync(uc);
                await _userCreditRepository.AddTransactionAsync(new CreditTransaction
                {
                    UserCreditId = uc.Id,
                    Amount = signupCredit,
                    Action = "SignupCredit",
                    Description = "Signup credit on approval",
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                userCredit.Balance += signupCredit;
                userCredit.UpdatedAt = DateTime.Now;
                await _userCreditRepository.UpdateUserCreditAsync(userCredit);
                await _userCreditRepository.AddTransactionAsync(new CreditTransaction
                {
                    UserCreditId = userCredit.Id,
                    Amount = signupCredit,
                    Action = "SignupCredit",
                    Description = "Signup credit added on approval",
                    Timestamp = DateTime.Now
                });
            }
        }

        // Grant all permissions to the admin user
        var allPermissions = await _permissionRepository.GetAllPermissionsAsync();
        if (allPermissions != null && allPermissions.Any())
        {
            var userPermissions = allPermissions
                .SelectMany(m => m.Permissions)
                .Select(p => new UserPermission
                {
                    UserId = adminUser.Id.ToString(),
                    PermissionId = p.PermissionId,
                    GrantedByUserId = adminUser.Id.ToString(), // Or SuperAdmin Id if available
                    GrantedAt = DateTime.Now
                }).ToList();

            await _userPermissionRepository.AddRangeAsync(userPermissions);
        }

        // Send Approval Email to Admin
        try
        {
            var subject = "Your Avinya AI CRM Account has been Approved!";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #10b981;'>Congratulations!</h2>
                    <p>Hi {adminUser.FullName},</p>
                    <p>We are pleased to inform you that your company, <strong>{tenant.CompanyName}</strong>, has been approved on Avinya AI CRM.</p>
                    <p>You can now log in to your account and start managing your business.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{_emailSettings.FrontendUrl}/login' style='background-color: #10b981; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Log In Now</a>
                    </div>
                    <p>If you have any questions, feel free to reply to this email.</p>
                    <p style='font-size: 12px; color: #6b7280;'>Best regards,<br/>Avinya AI CRM Team</p>
                </div>";

            await _emailService.SendEmailAsync(adminUser.Email, subject, body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Approval Email Failed: {ex.Message}");
        }

        return CommonHelper.SuccessResponseMessage("Admin approved successfully", null);
    }
}

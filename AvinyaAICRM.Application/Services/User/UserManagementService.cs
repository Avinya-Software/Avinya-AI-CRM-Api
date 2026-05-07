using AvinyaAICRM.Application.DTOs.Auth;
using AvinyaAICRM.Application.DTOs.User;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.User;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Infrastructure.Identity;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.EmailService;
using AvinyaAICRM.Application.DTOs.EmailSetting;
using Microsoft.Extensions.Options;

namespace AvinyaAICRM.Application.Services.User
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepo;
        private readonly IUserPermissionRepository _permissionRepo;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;

        public UserManagementService(
            IUserRepository userRepo,
            IUserPermissionRepository permissionRepo,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings)
        {
            _userRepo = userRepo;
            _permissionRepo = permissionRepo;
            _roleManager = roleManager;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
        }

        public async Task<ResponseModel> CreateUserAsync(
            CreateUserRequestModel request,
            string createdByUserId)
        {
            var creator = await _userRepo.GetByIdAsync(createdByUserId);
            if (creator == null)
            return CommonHelper.UnauthorizedResponseMessage(string.Empty,"Not allowed to create users ");

            var creatorRoles = await _userRepo.GetRolesAsync(creator);

            // Hierarchy check (simplified)
            if (creatorRoles.Contains("Staff"))
                return CommonHelper.UnauthorizedResponseMessage(string.Empty, "Not allowed to create users ");

            var existingEmail = await _userRepo.GetByEmailAsync(request.Email);
            if (existingEmail != null)
                return CommonHelper.BadRequestResponseMessage("Email already exists");

            var user = new AppUser
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.Email, // Reverting UserName back to Email
                TenantId = Guid.Parse(request.TenantId),
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedByUserId = createdByUserId
            };

            var password = request.Password;
            // Treat null, empty, or "Default@123" as missing password
            bool isPasswordMissing = string.IsNullOrWhiteSpace(password) || password == "Default@123";
            
            IdentityResult created;
            if (isPasswordMissing)
            {
                created = await _userRepo.CreateUserAsync(user);
            }
            else
            {
                created = await _userRepo.CreateUserAsync(user, password);
            }

            if (!created.Succeeded)
            {
                var errors = string.Join(", ", created.Errors.Select(e => e.Description));
                return CommonHelper.BadRequestResponseMessage(errors);
            }
            await _userRepo.AddToRoleAsync(user, request.Role);

            if (!creatorRoles.Contains("Admin") && !creatorRoles.Contains("SuperAdmin"))
                return CommonHelper.ForbiddenResponseMessage("Not allowed to assign permissions");

            // Assign new permissions
            if (request.PermissionIds != null && request.PermissionIds.Any())
            {
                foreach (var permissionId in request.PermissionIds)
                {
                    await _permissionRepo.AddAsync(new UserPermission
                    {
                        UserId = user.Id, 
                        PermissionId = permissionId,
                        GrantedByUserId = createdByUserId
                    });
                }
                await _permissionRepo.InvalidateCacheAsync(user.Id);
            }
            // Send Welcome Email with Password Reset (Set Password) link ONLY if password was missing/defaulted
            if (isPasswordMissing)
            {
                try
                {
                    var token = await _userRepo.GeneratePasswordResetTokenAsync(user);
                    await SendInvitationEmailAsync(user, token);
                }
                catch (Exception ex)
                {
                    // For debugging: rethrow or return error
                    throw new Exception($"Email sending failed to {user.Email}. Error: {ex.Message}. Check your EmailSettings in appsettings.json.");
                }
            }

            return CommonHelper.CreatedResponseMessage("User",null);
        }

        public async Task<ResponseModel> UpdateUserAsync(UpdateUserRequestModel request , string grantedByUserId)
        {
            var user = await _userRepo.GetByIdAsync(request.UserId);
            if (user == null)
                return CommonHelper.BadRequestResponseMessage("User not found");

            if (user.Email != request.Email)
            {
                var existingEmail = await _userRepo.GetByEmailAsync(request.Email);
                if (existingEmail != null)
                    return CommonHelper.BadRequestResponseMessage("Email already exists");
            }

            user.FullName = request.FullName;
            user.UserName = request.Email; // Syncing UserName with Email
            user.IsActive = request.IsActive;
            user.Email = request.Email;

            await _userRepo.UpdateAsync(user);
            
            // Update Role
            if (!string.IsNullOrEmpty(request.Role))
            {
                var currentRoles = await _userRepo.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userRepo.RemoveFromRolesAsync(user, currentRoles);
                }
                await _userRepo.AddToRoleAsync(user, request.Role);
            }

            var granter = await _userRepo.GetByIdAsync(grantedByUserId);
            if (granter == null)
                return CommonHelper.UnauthorizedResponseMessage(string.Empty, "Not allowed to create users ");

            var granterRoles = await _userRepo.GetRolesAsync(granter);
            if (!granterRoles.Contains("Admin") && !granterRoles.Contains("SuperAdmin"))
                return CommonHelper.ForbiddenResponseMessage("Not allowed to assign permissions");

            // Remove old permissions
            await _permissionRepo.RemoveAllAsync(request.UserId);

            // Assign new permissions
            foreach (var permissionId in request.PermissionIds)
            {
                await _permissionRepo.AddAsync(new UserPermission
                {
                    UserId = request.UserId,
                    PermissionId = permissionId,
                    GrantedByUserId = grantedByUserId
                });
            }

            await _permissionRepo.InvalidateCacheAsync(request.UserId);

            return CommonHelper.UpdatedResponseMessage("User", null);
        }

        public async Task<ResponseModel> AssignPermissionsAsync(
            AssignPermissionsRequestModel request,
            string grantedByUserId)
        {
            var granter = await _userRepo.GetByIdAsync(grantedByUserId);
            if (granter == null)
                return CommonHelper.UnauthorizedResponseMessage(string.Empty, "Not allowed to create users ");

            var granterRoles = await _userRepo.GetRolesAsync(granter);
            if (!granterRoles.Contains("Admin") && !granterRoles.Contains("SuperAdmin"))
                return CommonHelper.ForbiddenResponseMessage("Not allowed to assign permissions");

            // Remove old permissions
            await _permissionRepo.RemoveAllAsync(request.UserId);

            // Assign new permissions
            foreach (var permissionId in request.PermissionIds)
            {
                await _permissionRepo.AddAsync(new UserPermission
                {
                    UserId = request.UserId,
                    PermissionId = permissionId,
                    GrantedByUserId = grantedByUserId
                });
            }

            await _permissionRepo.InvalidateCacheAsync(request.UserId);

            return CommonHelper.SuccessResponseMessage("Permissions assigned successfully", null);
        }

        public async Task<ResponseModel> GetMyPermissionsAsync(string userId)
        {
            var permissions = await _permissionRepo.GetUserPermissionsAsync(userId);

            return CommonHelper.GetResponseMessage(permissions);
        }

        public async Task<ResponseModel> GetMenuAsync(string userId)
        {
            var menu = await _permissionRepo.GetMenuAsync(userId);
            return CommonHelper.GetResponseMessage(menu);
        }

        public async Task<ResponseModel> GetUsersForSuperAdminAsync(UserListFilterRequest request, Guid? currentUserTenant)
        {
            var result = await _userRepo.GetUsersForSuperAdminAsync(request, currentUserTenant);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetMyCompaniesAsync(Guid? currentUserTenant)
        {
            var companies = await _userRepo.GetMyCompaniesAsync(currentUserTenant);
            return CommonHelper.GetResponseMessage(companies);
        }

        public async Task<ResponseModel> GetUsersDropdown(string userId)
        {
            var users = await _userRepo.GetUsersDropdown(userId);
            return CommonHelper.GetResponseMessage(users);
        }

        public async Task<ResponseModel> GetRolesAsync()
        {
            var roles = _roleManager.Roles.Select(r => new { r.Id, r.Name }).ToList();
            return CommonHelper.GetResponseMessage(roles);
        }

        public async Task<ResponseModel> ResendInvitationAsync(string userId, string createdByUserId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                return CommonHelper.BadRequestResponseMessage("User not found");

            var creator = await _userRepo.GetByIdAsync(createdByUserId);
            if (creator == null)
                return CommonHelper.UnauthorizedResponseMessage(string.Empty, "Unauthorized");

            var token = await _userRepo.GeneratePasswordResetTokenAsync(user);
            await SendInvitationEmailAsync(user, token);

            return CommonHelper.SuccessResponseMessage("Invitation email resent successfully", null);
        }

        private async Task SendInvitationEmailAsync(AppUser user, string token)
        {
            var hasPassword = await _userRepo.HasPasswordAsync(user);
            var frontendUrl = _emailSettings?.FrontendUrl ?? "https://aicrm.avinyasoftware.com";
            var baseUrl = frontendUrl.TrimEnd('/');
            
            var userEmail = user.Email ?? "";
            var userFullName = user.FullName ?? "User";

            var setPasswordUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(userEmail)}&hasPassword={hasPassword}";
            
            var subject = hasPassword ? "Reset Your Password - Avinya AI CRM" : "Welcome to Avinya AI CRM - Set Up Your Account";
            var actionText = hasPassword ? "Reset My Password" : "Set My Password";
            var heading = hasPassword ? "Password Reset Request" : "Welcome to Avinya AI CRM!";
            var message = hasPassword 
                ? "We received a request to reset your password. Click the button below to set a new password:" 
                : "Your account has been created by your administrator. To get started, please set your password by clicking the button below:";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #10b981;'>{heading}</h2>
                    <p>Hi {userFullName},</p>
                    <p>{message}</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{setPasswordUrl}' style='background-color: #10b981; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>{actionText}</a>
                    </div>
                    <p>This link will expire in 24 hours for security reasons.</p>
                    <p>After setting your password, you can log in with your email: <strong>{userEmail}</strong></p>
                    <p style='margin-top: 30px; font-size: 12px; color: #6b7280;'>If you did not request this or have any questions, please contact your administrator.</p>
                    <p style='font-size: 12px; color: #6b7280;'>Best regards,<br/>Avinya AI CRM Team</p>
                </div>";

            await _emailService.SendEmailAsync(userEmail, subject, body);
        }

    }
}

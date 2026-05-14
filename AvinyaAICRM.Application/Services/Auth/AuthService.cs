using AvinyaAICRM.Application.DTOs.Auth;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Settings;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tenant;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Auth;
using AvinyaAICRM.Domain.Entities.Tenant;
using AvinyaAICRM.Domain.Enums;
using AvinyaAICRM.Infrastructure.Identity;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.EmailService;
using AvinyaAICRM.Application.DTOs.EmailSetting;
using Microsoft.Extensions.Options;


namespace AvinyaAICRM.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly ITenantRepository _tenantRepo;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;

        public AuthService(
            IUserRepository userRepo,
            ITenantRepository tenantRepo,
            IJwtTokenGenerator jwtTokenGenerator,
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings)
        {
            _userRepo = userRepo;
            _tenantRepo = tenantRepo;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
        }

        public async Task<ResponseModel> RegisterUser(UserRegisterRequestModel request)
        {
            var existingUser = await _userRepo.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return CommonHelper.BadRequestResponseMessage("Email already registered");

            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                PhoneNumber = request.CompanyPhone,
                FullName = request.FullName,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userRepo.CreateUserAsync(user, request.Password);
            if (result.Errors.Any())
            {
                var error = result.Errors.First();
                return CommonHelper.BadRequestResponseMessage(error);
            }
            

            await _userRepo.AddToRoleAsync(user, "Admin");

            var tenant = await _tenantRepo.CreateTenantAsync(new Tenant
            {
                CompanyName = request.CompanyName,
                CompanyEmail = request.Email,
                CompanyPhone =request.CompanyPhone,
                IsActive = true,
                IsApproved = true
            });

            user.TenantId = tenant.TenantId;

            await _userRepo.UpdateAsync(user);

            await _userRepo.AssignAllPermissionsToUserAsync(user.Id);

            // Send Welcome Email
            try
            {
                var subject = "Welcome to Avinya AI CRM - Signup Successful";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                        <h2 style='color: #10b981;'>Welcome to Avinya AI CRM!</h2>
                        <p>Hi {user.FullName},</p>
                        <p>Thank you for signing up. Your account is currently pending approval by our administrators.</p>
                        <p>Once approved, you will receive another email with instructions on how to log in.</p>
                        <p style='font-size: 12px; color: #6b7280;'>Best regards,<br/>Avinya AI CRM Team</p>
                    </div>";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Signup Email Failed: {ex.Message}");
            }

            // Send Notification Email to Admin
            try
            {
                var adminEmail = _emailSettings.Email;
                var adminSubject = "New Company Registration - Action Required";
                var adminBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                        <h2 style='color: #10b981;'>New Registration!</h2>
                        <p>A new company has registered on Avinya AI CRM and is awaiting approval.</p>
                        <p><strong>Company:</strong> {request.CompanyName}</p>
                        <p><strong>Admin Name:</strong> {request.FullName}</p>
                        <p><strong>Admin Email:</strong> {request.Email}</p>
                        <p><strong>Phone:</strong> {request.CompanyPhone}</p>
                        <div style='margin-top: 20px;'>
                            <a href='{_emailSettings.FrontendUrl}/login/superadmin' style='background-color: #10b981; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Review in Dashboard</a>
                        </div>
                    </div>";

                await _emailService.SendEmailAsync(adminEmail, adminSubject, adminBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Admin Notification Email Failed: {ex.Message}");
            }

            return CommonHelper.SuccessResponseMessage("Signup successful. Waiting for approval.", null);
        }

        public async Task<ResponseModel> Login(UserLoginRequestModel model)
        {
            var user = await _userRepo.GetByEmailAsync(model.Email);
            if (user == null)
                return CommonHelper.UnauthorizedResponseMessage(ResponseType.Unauthorized.ToString(), "Invalid credentials");

            if (!user.IsActive)
                return CommonHelper.ForbiddenResponseMessage("not approved");

            var validPassword = await _userRepo.CheckPasswordAsync(user, model.Password);
            if (!validPassword)
                return CommonHelper.UnauthorizedResponseMessage(ResponseType.Unauthorized.ToString(), "Invalid credentials");

            var roles = await _userRepo.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                var tenant = await _tenantRepo.GetByIdAsync(user.TenantId);
                if (tenant == null || !tenant.IsApproved)
                    return CommonHelper.ForbiddenResponseMessage("Tenant not approved");
            }

            var token = await _jwtTokenGenerator.GenerateToken(user);

            var data = new 
            {
                Token = token,
                UserId = user.Id,
                UserName = user.FullName,
                TenantId = user.TenantId
            };

            return CommonHelper.SuccessResponseMessage("",data);
        }

        public async Task<ResponseModel> AdminLogin(UserLoginRequestModel model)
        {
            var user = await _userRepo.GetByEmailAsync(model.Email);
            if (user == null)
                return CommonHelper.UnauthorizedResponseMessage(ResponseType.Unauthorized.ToString(), "Invalid credentials");

            var validPassword = await _userRepo.CheckPasswordAsync(user, model.Password);
            if (!validPassword)
                return CommonHelper.UnauthorizedResponseMessage(ResponseType.Unauthorized.ToString(), "Invalid credentials");

            var roles = await _userRepo.GetRolesAsync(user);
            if (!roles.Contains("SuperAdmin"))
            {
                return CommonHelper.AdminLoginForbiddenResponseMessage("SuperAdmin");
            }

            var token = await _jwtTokenGenerator.GenerateToken(user);

            var data = new
            {
                Token = token,
                UserId = user.Id,
                UserName = user.FullName,
                TenantId = user.TenantId
            };

            return CommonHelper.SuccessResponseMessage("", data);
        }

        public async Task<ResponseModel> ForgotPassword(string email)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null)
                return CommonHelper.BadRequestResponseMessage("User not found");

            var token = await _userRepo.GeneratePasswordResetTokenAsync(user);
            
            // Send Email
            var frontendUrl = _emailSettings?.FrontendUrl ?? "https://aicrm.avinyasoftware.com";
            var baseUrl = frontendUrl.TrimEnd('/');
            
            var userEmail = user.Email ?? email;
            var userFullName = user.FullName ?? "User";

            var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(userEmail)}&hasPassword=true";
            var subject = "Reset Your Password - Avinya AI CRM";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #10b981;'>Password Reset Request</h2>
                    <p>Hi {userFullName},</p>
                    <p>We received a request to reset your password. Click the button below to set a new password:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetUrl}' style='background-color: #10b981; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Reset Password</a>
                    </div>
                    <p>This link will expire in 24 hours for security reasons.</p>
                    <p>If you didn't request this, you can safely ignore this email.</p>
                    <p style='font-size: 12px; color: #6b7280;'>Best regards,<br/>Avinya AI CRM Team</p>
                </div>";

            await _emailService.SendEmailAsync(userEmail, subject, body);

            return CommonHelper.SuccessResponseMessage("Password reset link sent to your email", null);
        }

        public async Task<ResponseModel> ResetPassword(ResetPasswordRequestModel model)
        {
            var user = await _userRepo.GetByEmailAsync(model.Email);
            if (user == null)
                return CommonHelper.BadRequestResponseMessage("User not found");

            // Robustness: handle potential + being turned into space by some URI parsers
            var token = model.Token.Replace(" ", "+");

            var result = await _userRepo.ResetPasswordAsync(user, token, model.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return CommonHelper.BadRequestResponseMessage(errors);
            }

            return CommonHelper.SuccessResponseMessage("Password reset successfully", null);
        }
    }
}

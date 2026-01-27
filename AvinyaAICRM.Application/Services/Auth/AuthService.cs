using AvinyaAICRM.Application.DTOs.Auth;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tenant;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Auth;
using AvinyaAICRM.Domain.Entities.Tenant;
using AvinyaAICRM.Domain.Enums;
using AvinyaAICRM.Infrastructure.Identity;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;


namespace AvinyaAICRM.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly ITenantRepository _tenantRepo;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(
            IUserRepository userRepo,
            ITenantRepository tenantRepo,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepo = userRepo;
            _tenantRepo = tenantRepo;
            _jwtTokenGenerator = jwtTokenGenerator;
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
                FullName = request.FullName,
                IsActive = false
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
                IsActive = false,
                IsApproved = false
            });

            user.TenantId = tenant.TenantId;

            await _userRepo.UpdateAsync(user);

            return CommonHelper.SuccessResponseMessage("Signup successful. Waiting for approval.", null);
        }

        public async Task<ResponseModel> Login(UserLoginRequestModel model)
        {
            var user = await _userRepo.GetByEmailAsync(model.Email);
            if (user == null)
                return CommonHelper.UnauthorizedResponseMessage(ResponseType.Unauthorized.ToString(), "Invalid credentials");

            if (!user.IsActive)
                return CommonHelper.ForbiddenResponseMessage("Account not approved");

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
    }

}

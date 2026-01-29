using AvinyaAICRM.Application.DTOs.Auth;
using AvinyaAICRM.Application.DTOs.User;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.User;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Infrastructure.Identity;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Identity;

namespace AvinyaAICRM.Application.Services.User
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepo;
        private readonly IUserPermissionRepository _permissionRepo;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementService(
            IUserRepository userRepo,
            IUserPermissionRepository permissionRepo,
            RoleManager<IdentityRole> roleManager)
        {
            _userRepo = userRepo;
            _permissionRepo = permissionRepo;
            _roleManager = roleManager;
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

            var user = new AppUser
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.Email,
                TenantId = creator.TenantId,
                IsActive = true
            };

            await _userRepo.CreateUserAsync(user, request.Password);
            await _userRepo.AddToRoleAsync(user, request.Role);
            return CommonHelper.CreatedResponseMessage("User",null);
        }

        public async Task<ResponseModel> UpdateUserAsync(UpdateUserRequestModel request)
        {
            var user = await _userRepo.GetByIdAsync(request.UserId);
            if (user == null)
                return CommonHelper.BadRequestResponseMessage("User not found");

            user.FullName = request.FullName;
            user.IsActive = request.IsActive;

            await _userRepo.UpdateAsync(user);

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

        public async Task<ResponseModel> GetUsersForSuperAdminAsync(UserListFilterRequest request)
        {
            var result = await _userRepo.GetUsersForSuperAdminAsync(request);
            return CommonHelper.GetResponseMessage(result);
        }

    }

}

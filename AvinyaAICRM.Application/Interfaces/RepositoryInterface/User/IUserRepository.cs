using AvinyaAICRM.Application.DTOs.User;
using AvinyaAICRM.Infrastructure.Identity;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.User
{
    public interface IUserRepository
    {
        Task<AppUser?> GetByIdAsync(string userId);
        Task<AppUser?> GetByEmailAsync(string email);
        Task<IdentityResult> CreateUserAsync(AppUser user, string password);
        Task<IdentityResult> CreateUserAsync(AppUser user);
        Task AddToRoleAsync(AppUser user, string role);
        Task RemoveFromRolesAsync(AppUser user, IEnumerable<string> roles);
        Task<bool> CheckPasswordAsync(AppUser user, string password);
        Task UpdateAsync(AppUser user);
        Task<IList<string>> GetRolesAsync(AppUser user);
        Task<AppUser?> GetAdminByTenantIdAsync(Guid tenantId);
        Task<bool> IsInRoleAsync(string userId, string roleName);
        Task<PagedResult<UserListDto>> GetUsersForSuperAdminAsync(UserListFilterRequest request, Guid? currentUserTenantId);
        Task<List<AvinyaAICRM.Domain.Entities.Tenant.Tenant>> GetMyCompaniesAsync(Guid? currentUserTenantId);
        Task<List<UserDropdownDto>> GetUsersDropdown(string userId);
        Task<AppUser?> GetByFullNameAsync(string fullName);
        Task<AppUser> GetUserName(string name);
        Task<string> GeneratePasswordResetTokenAsync(AppUser user);
        Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string newPassword);
        Task<bool> HasPasswordAsync(AppUser user);
    }
}

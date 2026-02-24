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
        Task AddToRoleAsync(AppUser user, string role);
        Task<bool> CheckPasswordAsync(AppUser user, string password);
        Task UpdateAsync(AppUser user);
        Task<IList<string>> GetRolesAsync(AppUser user);
        Task<AppUser?> GetAdminByTenantIdAsync(Guid tenantId);
        Task<bool> IsInRoleAsync(string userId, string roleName);
        Task<PagedResult<UserListDto>> GetUsersForSuperAdminAsync(UserListFilterRequest request);
        Task<List<AvinyaAICRM.Domain.Entities.Tenant.Tenant>> GetMyCompaniesAsync();
        Task<List<UserDropdownDto>> GetUsersDropdown(string userId);
        Task<AppUser> GetUserName(string name);

    }
}

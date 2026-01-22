using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Repositories.User
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<AppUser> _userManager;

        public UserRepository(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<AppUser?> GetByEmailAsync(string email)
            => await _userManager.FindByEmailAsync(email);

        public async Task<bool> CreateUserAsync(AppUser user, string password)
            => (await _userManager.CreateAsync(user, password)).Succeeded;

        public async Task AddToRoleAsync(AppUser user, string role)
            => await _userManager.AddToRoleAsync(user, role);

        public async Task<bool> CheckPasswordAsync(AppUser user, string password)
            => await _userManager.CheckPasswordAsync(user, password);

        public async Task UpdateAsync(AppUser user)
            => await _userManager.UpdateAsync(user);

        public async Task<IList<string>> GetRolesAsync(AppUser user)
            => await _userManager.GetRolesAsync(user);

        public async Task<AppUser?> GetAdminByTenantIdAsync(Guid tenantId)
        {
            return await _userManager.Users
                .Where(u => u.TenantId == tenantId)
                .Where(u => _userManager.IsInRoleAsync(u, "Admin").Result)
                .FirstOrDefaultAsync();
        }

    }

}

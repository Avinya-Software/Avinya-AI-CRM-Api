using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Infrastructure.Identity;
using AvinyaAICRM.Infrastructure.Persistence;
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
        private readonly AppDbContext _context;

        public UserRepository(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<AppUser?> GetByEmailAsync(string email)
        {
            try
            {
                var res = await _userManager.FindByEmailAsync(email);
                return res;
            }
            catch (Exception ex)
            {
                return null;
            }
            
        } 

        //public async Task<bool> CreateUserAsync(AppUser user, string password)
        //    => (await _userManager.CreateAsync(user, password)).Succeeded;

        public async Task<IdentityResult> CreateUserAsync(AppUser user, string password)
        {
            var res = await _userManager.CreateAsync(user, password);
            return res;
        }
        

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
            return await (
                from user in _userManager.Users
                join userRole in _context.UserRoles on user.Id equals userRole.UserId
                join role in _context.Roles on userRole.RoleId equals role.Id
                where user.TenantId == tenantId
                      && role.Name == "Admin"
                select user
            ).FirstOrDefaultAsync();
        }

        public async Task<AppUser?> GetByIdAsync(string userId)
       => await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

        public async Task CreateAsync(AppUser user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

    }

}

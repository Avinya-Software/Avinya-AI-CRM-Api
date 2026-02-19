using AvinyaAICRM.Application.DTOs.User;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Infrastructure.Identity;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        public async Task<bool> IsInRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, roleName);
        }

        public async Task<PagedResult<UserListDto>> GetUsersForSuperAdminAsync(UserListFilterRequest request)
        {
            var query =
                from u in _context.Users
                join ur in _context.UserRoles on u.Id equals ur.UserId
                join r in _context.Roles on ur.RoleId equals r.Id
                join t in _context.Tenants on u.TenantId equals t.TenantId into tenantJoin
                from t in tenantJoin.DefaultIfEmpty()
                select new
                {
                    User = u,
                    RoleName = r.Name,
                    TenantName = t != null ? t.CompanyName : "System"
                };

            // 🔎 Filters
            if (!string.IsNullOrEmpty(request.Role))
                query = query.Where(x => x.RoleName == request.Role);

            if (request.TenantId.HasValue)
                query = query.Where(x => x.User.TenantId == request.TenantId);

            if (request.IsActive.HasValue)
                query = query.Where(x => x.User.IsActive == request.IsActive);

            if (!string.IsNullOrEmpty(request.Search))
                query = query.Where(x =>
                    x.User.FullName.Contains(request.Search) ||
                    x.User.Email.Contains(request.Search));

            var totalRecords = await query.CountAsync();

            var users = await query
                .OrderByDescending(x => x.User.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new UserListDto
                {
                    UserId = x.User.Id,
                    FullName = x.User.FullName,
                    Email = x.User.Email,
                    Role = x.RoleName,
                    TenantId = x.User.TenantId,
                    TenantName = x.TenantName,
                    IsActive = x.User.IsActive,
                    CreatedAt = x.User.CreatedAt,

                    // ✅ Include Permissions
                    PermissionIds = _context.UserPermissions
                        .Where(up => up.UserId == x.User.Id)
                        .Select(up => up.PermissionId)
                        .ToList()
                })
                .ToListAsync();

            return new PagedResult<UserListDto>
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize),
                Data = users
            };
        }


        public async Task<List<AvinyaAICRM.Domain.Entities.Tenant.Tenant>> GetMyCompaniesAsync()
        {
            var tenants = await _context.Tenants.ToListAsync();
            return tenants;
        }

        public async Task<List<UserDropdownDto>> GetUsersDropdown(string userId)
        {
            var tenantId = await _context.Users
               .Where(u => u.Id == userId)
               .Select(u => u.TenantId)
               .FirstOrDefaultAsync();

            if (tenantId == null)
                return new List<UserDropdownDto>();

            return await _context.Users
                .Where(u => u.TenantId == tenantId && u.IsActive)
                .Select(u => new UserDropdownDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email
                })
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
    }

}

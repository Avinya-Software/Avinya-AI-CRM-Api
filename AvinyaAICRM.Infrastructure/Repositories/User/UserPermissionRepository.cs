using AvinyaAICRM.Application.DTOs.MenuItem;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AvinyaAICRM.Infrastructure.Repositories.User
{
    public class UserPermissionRepository : IUserPermissionRepository
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IUserRepository _userRepository;

        public UserPermissionRepository(AppDbContext context, IMemoryCache cache, IUserRepository userRepo)
        {
            _context = context;
            _cache = cache;
            _userRepository = userRepo;
        }
        private string CacheKey(string userId) => $"PERMS_{userId}";

        public async Task RemoveAllAsync(string userId)
        {
            var permissions = await _context.UserPermissions
                .Where(x => x.UserId == userId)
                .ToListAsync();

            _context.UserPermissions.RemoveRange(permissions);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(UserPermission permission)
        {
            try
            {
                _context.UserPermissions.Add(permission);
                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {

            }
          
        }

        public async Task<bool> HasPermissionAsync(string userId, string moduleKey, string actionKey)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            return permissions.Contains($"{moduleKey}:{actionKey}");
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            if (await _userRepository.IsInRoleAsync(userId, "SuperAdmin"))
            {
                return await (
                    from p in _context.Permissions
                    join m in _context.Modules on p.ModuleId equals m.ModuleId
                    join a in _context.Actions on p.ActionId equals a.ActionId
                    select m.ModuleKey + ":" + a.ActionKey
                ).ToListAsync();
            }

            // 🔹 Cached normal users
            if (_cache.TryGetValue(CacheKey(userId), out List<string> cached))
                return cached;

            var permissions = await (
                from up in _context.UserPermissions
                join p in _context.Permissions on up.PermissionId equals p.PermissionId
                join m in _context.Modules on p.ModuleId equals m.ModuleId
                join a in _context.Actions on p.ActionId equals a.ActionId
                where up.UserId == userId
                select m.ModuleKey + ":" + a.ActionKey
            ).ToListAsync();

            _cache.Set(CacheKey(userId), permissions, TimeSpan.FromMinutes(30));
            return permissions;
        }

        public Task InvalidateCacheAsync(string userId)
        {
            _cache.Remove(CacheKey(userId));
            return Task.CompletedTask;
        }

        public async Task<List<MenuItemDto>> GetMenuAsync(string userId)
        {
            if (await _userRepository.IsInRoleAsync(userId, "SuperAdmin"))
            {
                return await (
                    from p in _context.Permissions
                    join m in _context.Modules on p.ModuleId equals m.ModuleId
                    join a in _context.Actions on p.ActionId equals a.ActionId
                    where m.IsActive
                    group a.ActionKey by new { m.ModuleKey, m.ModuleName } into g
                    select new MenuItemDto
                    {
                        ModuleKey = g.Key.ModuleKey,
                        ModuleName = g.Key.ModuleName,
                        Actions = g.ToList()
                    }
                ).ToListAsync();
            }

            // 🔹 Normal users (existing logic)
            return await (
                from up in _context.UserPermissions
                join p in _context.Permissions on up.PermissionId equals p.PermissionId
                join m in _context.Modules on p.ModuleId equals m.ModuleId
                join a in _context.Actions on p.ActionId equals a.ActionId
                where up.UserId == userId
                group a.ActionKey by new { m.ModuleKey, m.ModuleName } into g
                select new MenuItemDto
                {
                    ModuleKey = g.Key.ModuleKey,
                    ModuleName = g.Key.ModuleName,
                    Actions = g.ToList()
                }
            ).ToListAsync();
        }

    }
}

using AvinyaAICRM.Application.DTOs.MenuItem;
using AvinyaAICRM.Domain.Entities.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.User
{
    public interface IUserPermissionRepository
    {
        Task RemoveAllAsync(string userId);
        Task AddAsync(UserPermission permission);
        Task<bool> HasPermissionAsync(string userId, string moduleKey, string actionKey);
        Task<List<string>> GetUserPermissionsAsync(string userId);
        Task InvalidateCacheAsync(string userId);
        Task<List<MenuItemDto>> GetMenuAsync(string userId);
    }

}

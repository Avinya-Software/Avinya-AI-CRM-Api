using AvinyaAICRM.Application.DTOs.Permission;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Permission;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Repositories.Permission
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly AppDbContext _context;

        public PermissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PermissionListDto>> GetAllPermissionsAsync()
        {
            return await (
                from p in _context.Permissions
                join m in _context.Modules on p.ModuleId equals m.ModuleId
                join a in _context.Actions on p.ActionId equals a.ActionId
                where m.IsActive
                group new { p, a } by new { m.ModuleKey, m.ModuleName } into g
                select new PermissionListDto
                {
                    ModuleKey = g.Key.ModuleKey,
                    ModuleName = g.Key.ModuleName,
                    Permissions = g.Select(x => new PermissionActionDto
                    {
                        PermissionId = x.p.PermissionId,
                        ActionKey = x.a.ActionKey,
                        ActionName = x.a.ActionName
                    }).ToList()
                }
            ).ToListAsync();
        }
    }
}

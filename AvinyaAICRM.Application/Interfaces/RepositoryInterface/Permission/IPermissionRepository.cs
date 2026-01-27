using AvinyaAICRM.Application.DTOs.Permission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Permission
{
    public interface IPermissionRepository
    {
        Task<List<PermissionListDto>> GetAllPermissionsAsync();
    }
}

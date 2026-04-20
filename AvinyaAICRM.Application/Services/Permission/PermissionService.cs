using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Permission;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Permission;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.Permission
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionService(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task<ResponseModel> GetPermissionListAsync()
        {
            var permissions = await _permissionRepository.GetAllPermissionsAsync();

            return CommonHelper.GetResponseMessage(permissions);
        }
    }

}

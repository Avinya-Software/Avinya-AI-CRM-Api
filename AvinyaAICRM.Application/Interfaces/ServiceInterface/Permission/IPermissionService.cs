using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Permission
{
    public interface IPermissionService
    {
        Task<ResponseModel> GetPermissionListAsync();
    }

}

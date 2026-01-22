using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.SuperAdmin
{
    public interface ISuperAdminService
    {
        Task<ResponseModel> ApproveAdminAsync(Guid tenantId);
    }
}

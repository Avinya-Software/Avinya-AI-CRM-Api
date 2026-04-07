using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Tenants
{
    public interface ITenantService
    {
        Task<ResponseModel> GetByIdAsync(Guid tenantId);
        Task<ResponseModel> UpdateAsync(AvinyaAICRM.Domain.Entities.Tenant.Tenant tenant);
    }
}

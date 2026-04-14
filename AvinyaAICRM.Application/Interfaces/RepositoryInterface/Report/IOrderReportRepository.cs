using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report
{
    public interface IOrderReportRepository
    {
        Task<OrderReportDto> GetOrderReportAsync(OrderReportFilterDto filter);
        Task<PagedResult<OrderLifecycleReportDto>> GetOrderLifecycleReportAsync(OrderReportFilterDto filter);
    }
}

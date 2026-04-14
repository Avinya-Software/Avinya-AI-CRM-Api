using AvinyaAICRM.Application.DTOs.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report
{
    public interface IFinanceReportRepository
    {
        Task<FinanceReportDto> GetFinanceReportAsync(FinanceReportFilterDto filter);
    }
}

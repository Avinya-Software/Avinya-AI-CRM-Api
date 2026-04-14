using AvinyaAICRM.Application.DTOs.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report
{
    public interface IQuotationReportRepository
    {
        Task<QuotationReportDto> GetQuotationReportAsync(QuotationReportFilterDto filter);
        Task<List<QuotationLifecycleReportDto>> GetQuotationLifecycleReportAsync(QuotationReportFilterDto filter);
    }
}

using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Report
{
    public interface IQuotationReportService
    {
        Task<ResponseModel> GetQuotationReportAsync(QuotationReportFilterDto filter);
        Task<ResponseModel> GetQuotationLifecycleReportAsync(QuotationReportFilterDto filter);
    }
}

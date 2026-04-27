using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Report;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.Report
{
    public class QuotationReportService : IQuotationReportService
    {
        private readonly IQuotationReportRepository _repository;

        public QuotationReportService(IQuotationReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResponseModel> GetQuotationReportAsync(QuotationReportFilterDto filter)
        {
            // Default: current quarter
            if (filter.DateFrom is null && filter.DateTo is null)
            {
                var today = DateTime.Now;
                var quarter = (today.Month - 1) / 3;
                filter.DateFrom = new DateTime(today.Year, quarter * 3 + 1, 1);
                filter.DateTo = today;
            }

            if (filter.DateTo.HasValue)
                filter.DateTo = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);

            var report = await _repository.GetQuotationReportAsync(filter);

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Quotation report fetched successfully.",
                Data = report
            };
        }

        public async Task<ResponseModel> GetQuotationLifecycleReportAsync(QuotationReportFilterDto filter)
        {
            if (filter.DateTo.HasValue)
                filter.DateTo = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);

            var report = await _repository.GetQuotationLifecycleReportAsync(filter);

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Quotation lifecycle report fetched successfully.",
                Data = report
            };
        }
    }
}

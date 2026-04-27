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
    public class FinanceReportService : IFinanceReportService
    {
        private readonly IFinanceReportRepository _repository;

        public FinanceReportService(IFinanceReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResponseModel> GetFinanceReportAsync(FinanceReportFilterDto filter)
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

            var report = await _repository.GetFinanceReportAsync(filter);

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Finance report fetched successfully.",
                Data = report
            };
        }
    }
}

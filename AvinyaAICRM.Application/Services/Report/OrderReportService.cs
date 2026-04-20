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

    public class OrderReportService : IOrderReportService
    {
        private readonly IOrderReportRepository _repository;

        public OrderReportService(IOrderReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResponseModel> GetOrderReportAsync(OrderReportFilterDto filter)
        {
            // Default: current month
            if (filter.DateFrom is null && filter.DateTo is null)
            {
                var today = DateTime.UtcNow;
                filter.DateFrom = new DateTime(today.Year, today.Month, 1);
                filter.DateTo = today;
            }

            if (filter.DateTo.HasValue)
                filter.DateTo = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);

            var report = await _repository.GetOrderReportAsync(filter);

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Order report fetched successfully.",
                Data = report
            };
        }

        public async Task<ResponseModel> GetOrderLifecycleReportAsync(OrderReportFilterDto filter)
        {
            if (filter.DateTo.HasValue)
                filter.DateTo = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);

            var report = await _repository.GetOrderLifecycleReportAsync(filter);

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Order lifecycle report fetched successfully.",
                Data = report
            };
        }
    }
}

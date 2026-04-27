using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Application.Interfaces;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Report;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Services.Report
{
    public class LeadReportService : ILeadReportService
    {
        private readonly ILeadReportRepository _repository;

        public LeadReportService(ILeadReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResponseModel> GetLeadPipelineReportAsync(LeadPipelineFilterDto filter)
        {
            // Default date range: current month if nothing supplied
            if (filter.DateFrom is null && filter.DateTo is null)
            {
                var today = DateTime.Now;
                filter.DateFrom = new DateTime(today.Year, today.Month, 1);
                filter.DateTo = today;
            }

            // Ensure DateTo covers the full end-of-day
            if (filter.DateTo.HasValue)
                filter.DateTo = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);

            var report = await _repository.GetLeadPipelineReportAsync(filter);

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Lead pipeline report fetched successfully.",
                Data = report
            };
        }
        public async Task<ResponseModel> GetLeadLifecycleReportAsync(LeadPipelineFilterDto filter)
        {
            // Ensure DateTo covers the full end-of-day
            if (filter.DateTo.HasValue)
                filter.DateTo = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);

            var report = await _repository.GetLeadLifecycleReportAsync(filter);

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Lead lifecycle report fetched successfully.",
                Data = report
            };
        }
    }
}

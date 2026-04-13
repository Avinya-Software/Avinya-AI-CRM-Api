using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Report;
using AvinyaAICRM.Shared.Model;


namespace AvinyaAICRM.Application.Services.Report
{
    public class ClientReportService : IClientReportService
    {
        private readonly IClientReportRepository _repository;

        public ClientReportService(IClientReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResponseModel> GetClientReportAsync(ClientReportFilterDto filter)
        {
            // Default: current quarter
            if (filter.DateFrom is null && filter.DateTo is null)
            {
                var today = DateTime.UtcNow;
                var quarter = (today.Month - 1) / 3;
                filter.DateFrom = new DateTime(today.Year, quarter * 3 + 1, 1);
                filter.DateTo = today;
            }

            if (filter.DateTo.HasValue)
                filter.DateTo = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);

            var report = await _repository.GetClientReportAsync(filter);

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Client report fetched successfully.",
                Data = report
            };
        }
    }
}

using AvinyaAICRM.Application.DTOs.Report;
using AvinyaAICRM.Shared.Model;


namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Report
{
    public interface IOrderReportService
    {
        Task<ResponseModel> GetOrderReportAsync(OrderReportFilterDto filter);
        Task<ResponseModel> GetOrderLifecycleReportAsync(OrderReportFilterDto filter);
    }
}

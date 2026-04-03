using AvinyaAICRM.Application.DTOs.Order;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders
{
    public interface IOrderPdfService
    {
        byte[] GenerateOrderBillPdf(OrderResponseDto order, Guid billId);
    }
}

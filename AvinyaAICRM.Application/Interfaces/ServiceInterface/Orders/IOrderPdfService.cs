using AvinyaAICRM.Application.DTOs.Order;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders
{
    public interface IOrderPdfService
    {
        byte[] GenerateOrderPdf(OrderResponseDto order);
    }
}
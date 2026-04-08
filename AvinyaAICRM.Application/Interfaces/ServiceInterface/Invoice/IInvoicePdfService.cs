using AvinyaAICRM.Application.DTOs.Order;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Invoice
{
    public interface IInvoicePdfService
    {
        byte[] GenerateInvoicePdf(AvinyaAICRM.Application.DTOs.Invoice.InvoiceDto invoice, OrderResponseDto orderDetails);
    }
}

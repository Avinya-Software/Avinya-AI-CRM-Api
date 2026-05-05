using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.EmailService
{
    public interface IDocumentEmailService
    {
        Task SendQuotationEmailAsync(string toEmail, string clientName, string quotationNo, byte[] pdfData);
        Task SendOrderEmailAsync(string toEmail, string clientName, string orderNo, byte[] pdfData);
        Task SendInvoiceEmailAsync(string toEmail, string clientName, string invoiceNo, byte[] pdfData);
    }
}

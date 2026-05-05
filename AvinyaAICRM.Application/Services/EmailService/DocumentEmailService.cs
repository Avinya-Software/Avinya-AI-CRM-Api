using AvinyaAICRM.Application.Interfaces.ServiceInterface.EmailService;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.EmailService
{
    public class DocumentEmailService : IDocumentEmailService
    {
        private readonly IEmailService _emailService;

        public DocumentEmailService(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendQuotationEmailAsync(string toEmail, string clientName, string quotationNo, byte[] pdfData)
        {
            string subject = $"Quotation #{quotationNo} from Avinya AI CRM";
            string body = $@"
                <h3>Hello {clientName},</h3>
                <p>Please find the attached quotation <b>#{quotationNo}</b> as requested.</p>
                <p>If you have any questions, feel free to contact us.</p>
                <br/>
                <p>Best Regards,<br/>Team Avinya AI CRM</p>";

            await _emailService.SendEmailWithAttachmentAsync(toEmail, subject, body, pdfData, $"Quotation_{quotationNo}.pdf");
        }

        public async Task SendOrderEmailAsync(string toEmail, string clientName, string orderNo, byte[] pdfData)
        {
            string subject = $"Order Confirmation #{orderNo} - Avinya AI CRM";
            string body = $@"
                <h3>Hello {clientName},</h3>
                <p>Thank you for your order! Please find the attached order confirmation <b>#{orderNo}</b>.</p>
                <p>We will keep you updated on the progress.</p>
                <br/>
                <p>Best Regards,<br/>Team Avinya AI CRM</p>";

            await _emailService.SendEmailWithAttachmentAsync(toEmail, subject, body, pdfData, $"Order_{orderNo}.pdf");
        }

        public async Task SendInvoiceEmailAsync(string toEmail, string clientName, string invoiceNo, byte[] pdfData)
        {
            string subject = $"Invoice #{invoiceNo} from Avinya AI CRM";
            string body = $@"
                <h3>Hello {clientName},</h3>
                <p>Please find the attached invoice <b>#{invoiceNo}</b> for your recent purchase.</p>
                <p>Kindly acknowledge the receipt.</p>
                <br/>
                <p>Best Regards,<br/>Team Avinya AI CRM</p>";

            await _emailService.SendEmailWithAttachmentAsync(toEmail, subject, body, pdfData, $"Invoice_{invoiceNo}.pdf");
        }
    }
}

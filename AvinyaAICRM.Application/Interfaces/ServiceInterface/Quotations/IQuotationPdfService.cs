using AvinyaAICRM.Application.DTOs.Quotation;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Quotations
{
    public interface IQuotationPdfService
    {
        byte[] GenerateQuotationPdf(QuotationResponseDto quotation);
    }
}

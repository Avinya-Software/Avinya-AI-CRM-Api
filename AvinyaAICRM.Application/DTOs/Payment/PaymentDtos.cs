using System;

namespace AvinyaAICRM.Application.DTOs.Payment
{
    public class PaymentDto
    {
        public Guid PaymentID { get; set; }
        public Guid InvoiceID { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public string? TransactionRef { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
    }

    public class CreatePaymentDto
    {
        public Guid InvoiceID { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public string? TransactionRef { get; set; }
    }

    public class UpdatePaymentDto
    {
        public Guid PaymentID { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public string? TransactionRef { get; set; }
    }
}

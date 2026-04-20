using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.Payments
{
    public class Payment
    {
        public Guid PaymentID { get; set; }
        public Guid InvoiceID { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public string? TransactionRef { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
    }
}
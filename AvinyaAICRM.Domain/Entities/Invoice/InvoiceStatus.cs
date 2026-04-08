using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.Invoice
{
    public class InvoiceStatus
    {
        [Key]
        public int InvoiceStatusID { get; set; }
        public string InvoiceStatusName { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Report
{
    public class FinanceReportFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        /// <summary>Filter invoices by status</summary>
        public int? InvoiceStatusId { get; set; }

        /// <summary>Filter by specific client</summary>
        public Guid? ClientId { get; set; }

        /// <summary>Filter expenses by category</summary>
        public int? ExpenseCategoryId { get; set; }

        /// <summary>Filter payments by mode: Cash, UPI, Card, Online</summary>
        public string? PaymentMode { get; set; }

        /// <summary>Show only overdue invoices (OutstandingAmount > 0 and past DueDate)</summary>
        public bool OverdueOnly { get; set; } = false;

        // Injected from JWT
        public Guid TenantId { get; set; }
    }
}

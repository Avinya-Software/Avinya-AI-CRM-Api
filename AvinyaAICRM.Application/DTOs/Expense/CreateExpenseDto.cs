using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Expense
{
    public class CreateExpenseDto
    {
        public DateTime ExpenseDate { get; set; }
        public int CategoryId { get; set; } = 1;
        public decimal Amount { get; set; }
        public string? PaymentMode { get; set; }
        public string? Description { get; set; }
        public string? ReceiptPath { get; set; }
    }
}

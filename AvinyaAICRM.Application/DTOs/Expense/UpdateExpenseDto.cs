using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Expense
{
    public class UpdateExpenseDto : CreateExpenseDto
    {
        public Guid ExpenseId { get; set; }
    }
}

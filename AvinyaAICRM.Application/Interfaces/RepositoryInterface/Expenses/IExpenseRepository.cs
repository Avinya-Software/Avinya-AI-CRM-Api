using AvinyaAICRM.Domain.Entities.Expenses;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Expenses
{
    public interface IExpenseRepository
    {
        Task<PagedResult<Expense>> GetFilteredAsync(
    string? search,
    int page,
    int pageSize,
    string tenantId,
    string? status,
    DateTime? from,
    DateTime? to);
        Task<Expense?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(Expense expense);
        Task<bool> UpdateAsync(Expense expense);
        Task<List<ExpenseCategory>> GetCategoriesAsync();
    }
}

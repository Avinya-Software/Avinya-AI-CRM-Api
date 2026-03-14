using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Expenses;
using AvinyaAICRM.Domain.Entities.Expenses;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AvinyaAICRM.Infrastructure.Repositories.Expenses
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly AppDbContext _context;

        public ExpenseRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Expense>> GetFilteredAsync(
             string? search,
             int page,
             int pageSize,
             string tenantId,
             string? status,
             DateTime? from,
             DateTime? to)
        {
            try
            {
                var query = _context.Expenses
               .Include(x => x.ExpenseCategory)
               .Where(x => !x.IsDeleted &&
                           x.TenantId == Guid.Parse(tenantId));

                // 🔍 Search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(x =>
                        (x.Description != null && x.Description.ToLower().Contains(search)) ||
                        (x.PaymentMode != null && x.PaymentMode.ToLower().Contains(search)));
                }

                // 📅 Date Filter
                if (from.HasValue)
                    query = query.Where(x => x.ExpenseDate >= from.Value);

                if (to.HasValue)
                    query = query.Where(x => x.ExpenseDate <= to.Value);

                // 📌 Status Filter
                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(x => x.Status == status);

                var totalCount = await query.CountAsync();

                var data = await query
                    .OrderByDescending(x => x.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<Expense>
                {
                    Data = data,
                    TotalRecords = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    PageNumber = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                return new PagedResult<Expense>();
            }
        }

        public async Task<Expense?> GetByIdAsync(Guid id)
        {
            return await _context.Expenses
                .FirstOrDefaultAsync(x => x.ExpenseId == id && !x.IsDeleted);
        }

        public async Task<bool> CreateAsync(Expense expense)
        {
            await _context.Expenses.AddAsync(expense);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Expense expense)
        {
            _context.Expenses.Update(expense);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}

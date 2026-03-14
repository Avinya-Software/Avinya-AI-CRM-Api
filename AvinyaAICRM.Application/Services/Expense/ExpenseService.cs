using AvinyaAICRM.Application.DTOs.Expense;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Expenses;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Expense;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.Expense
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _repository;

        public ExpenseService(IExpenseRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResponseModel> GetFilteredAsync(
    string? search,
    int page,
    int pageSize,
    string tenantId,
    string? status,
    DateTime? from,
    DateTime? to)
        {
            var result = await _repository.GetFilteredAsync(
                search,
                page,
                pageSize,
                tenantId,
                status,
                from,
                to);

            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetByIdAsync(Guid id)
        {
            var expense = await _repository.GetByIdAsync(id);
            if (expense == null)
                return CommonHelper.BadRequestResponseMessage("Expense not found");

            return CommonHelper.GetResponseMessage(expense);
        }

        public async Task<ResponseModel> CreateAsync(CreateExpenseDto dto, string tenantId, Guid userId)
        {
            var expense = new AvinyaAICRM.Domain.Entities.Expenses.Expense
            {
                ExpenseId = Guid.NewGuid(),
                TenantId = Guid.Parse(tenantId),
                ExpenseDate = dto.ExpenseDate,
                CategoryId = dto.CategoryId,
                Amount = dto.Amount,
                PaymentMode = dto.PaymentMode,
                Description = dto.Description,
                ReceiptPath = dto.ReceiptPath,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            var success = await _repository.CreateAsync(expense);
            if (!success)
                return CommonHelper.BadRequestResponseMessage("Failed to create expense");

            return CommonHelper.GetResponseMessage(expense);
        }

        public async Task<ResponseModel> UpdateAsync(UpdateExpenseDto dto, Guid userId)
        {
            var expense = await _repository.GetByIdAsync(dto.ExpenseId);
            if (expense == null)
                return CommonHelper.BadRequestResponseMessage("Expense not found");

            expense.ExpenseDate = dto.ExpenseDate;
            expense.CategoryId = dto.CategoryId;
            expense.Amount = dto.Amount;
            expense.PaymentMode = dto.PaymentMode;
            expense.Description = dto.Description;
            expense.ReceiptPath = dto.ReceiptPath;
            expense.ModifiedBy = userId;
            expense.ModifiedDate = DateTime.UtcNow;

            var success = await _repository.UpdateAsync(expense);
            if (!success)
                return CommonHelper.BadRequestResponseMessage("Failed to update expense");

            return CommonHelper.GetResponseMessage(expense);
        }

        public async Task<ResponseModel> DeleteAsync(Guid id)
        {
            var expense = await _repository.GetByIdAsync(id);
            if (expense == null)
                return CommonHelper.BadRequestResponseMessage("Expense not found");

            expense.IsDeleted = true;

            var success = await _repository.UpdateAsync(expense);
            if (!success)
                return CommonHelper.BadRequestResponseMessage("Failed to delete expense");

            return CommonHelper.SuccessResponseMessage("Expense deleted successfully", null);
        }
    }
}

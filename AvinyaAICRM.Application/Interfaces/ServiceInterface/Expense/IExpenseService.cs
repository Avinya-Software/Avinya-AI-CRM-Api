using AvinyaAICRM.Application.DTOs.Expense;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Expense
{
    public interface IExpenseService
    {
        Task<ResponseModel> GetFilteredAsync(
    string? search,
    int page,
    int pageSize,
    string tenantId,
    string? status,
    DateTime? from,
    DateTime? to);
        Task<ResponseModel> GetByIdAsync(Guid id);
        Task<ResponseModel> CreateAsync(CreateExpenseDto dto, string tenantId, Guid userId);
        Task<ResponseModel> UpdateAsync(UpdateExpenseDto dto, Guid userId);
        Task<ResponseModel> DeleteAsync(Guid id);
    }
}

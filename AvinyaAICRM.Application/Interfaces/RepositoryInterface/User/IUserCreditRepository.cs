using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Shared.Model;
using System;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.User
{
    public interface IUserCreditRepository
    {
        Task<UserCredit?> GetByUserIdAsync(string userId);
        Task AddUserCreditAsync(UserCredit credit);
        Task UpdateUserCreditAsync(UserCredit credit);
        Task AddTransactionAsync(CreditTransaction transaction);
        Task<PagedResult<AvinyaAICRM.Application.DTOs.User.UserCreditListItemDto>> GetUserCreditsAsync(AvinyaAICRM.Application.DTOs.User.UserCreditFilterRequest request);
        Task<PagedResult<AvinyaAICRM.Application.DTOs.User.CreditTransactionDto>> GetTransactionsByUserIdAsync(string userId, int pageNumber, int pageSize);
    }
}

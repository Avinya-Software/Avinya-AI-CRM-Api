using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.User
{
    public interface ICreditService
    {
        Task<bool> HasEnoughCreditsAsync(string userId, int required);
        Task DeductCreditsAsync(string userId, int amount, string action);
        Task<int> DeductCreditsForTokenUsageAsync(string userId, int totalTokens, string action);
        Task<int> GetRemainingCreditsAsync(string userId);
        Task EnsureUserCreditExistsAsync(string userId, Guid tenantId);
        Task<AvinyaAICRM.Shared.Model.ResponseModel> GetByUserIdAsync(string userId);
        Task<AvinyaAICRM.Shared.Model.ResponseModel> UpdateBalanceAsync(string userId, int newBalance, string action, string? description = null);
        Task<AvinyaAICRM.Shared.Model.ResponseModel> GetUserCreditsAsync(AvinyaAICRM.Application.DTOs.User.UserCreditFilterRequest request);
        Task<AvinyaAICRM.Shared.Model.ResponseModel> GetTransactionsByUserIdAsync(string userId, int pageNumber, int pageSize);
        Task<int> ResetAllBalancesAsync(int amount);
        Task<DateTime?> GetLastResetDateAsync();
    }
}

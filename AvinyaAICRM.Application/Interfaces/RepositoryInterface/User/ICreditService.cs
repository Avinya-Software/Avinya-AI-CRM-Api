using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.User
{
    public interface ICreditService
    {
        Task<bool> HasEnoughCreditsAsync(string userId, int required);
        Task DeductCreditsAsync(string userId, int amount, string action);
        Task<int> GetRemainingCreditsAsync(string userId);
        Task EnsureUserCreditExistsAsync(string userId, Guid tenantId);
    }
}

using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Repositories.User
{
    public class CreditService : ICreditService
    {
        private readonly AppDbContext _context;
        private const int DEFAULT_BALANCE = 15000;

        public CreditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetRemainingCreditsAsync(string userId)
        {
            var credit = await _context.UserCredits
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();
            
            return credit?.Balance ?? 0;
        }

        public async Task<bool> HasEnoughCreditsAsync(string userId, int required)
        {
            var remaining = await GetRemainingCreditsAsync(userId);
            return remaining >= required;
        }

        public async Task DeductCreditsAsync(string userId, int amount, string action)
        {
            var credit = await _context.UserCredits
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();

            if (credit == null) return;

            // Ensure balance doesn't go below 0
            credit.Balance = Math.Max(0, credit.Balance - amount);
            credit.UpdatedAt = DateTime.UtcNow;

            _context.CreditTransactions.Add(new CreditTransaction
            {
                UserCreditId = credit.Id,
                Amount = amount,
                Action = action,
                Description = $"Used for {action}",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task EnsureUserCreditExistsAsync(string userId, Guid tenantId)
        {
            var exists = await _context.UserCredits.AnyAsync(x => x.UserId == userId);
            if (!exists)
            {
                _context.UserCredits.Add(new UserCredit
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Balance = DEFAULT_BALANCE,
                    UpdatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}

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
        private readonly IUserCreditRepository _repository;
        private const int DEFAULT_BALANCE = 15000;

        public CreditService(AppDbContext context, IUserCreditRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        public async Task<AvinyaAICRM.Shared.Model.ResponseModel> GetByUserIdAsync(string userId)
        {
            try
            {
                var credit = await _repository.GetByUserIdAsync(userId);
                return AvinyaAICRM.Shared.Helper.CommonHelper.GetResponseMessage(credit);
            }
            catch (Exception ex)
            {
                return AvinyaAICRM.Shared.Helper.CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<AvinyaAICRM.Shared.Model.ResponseModel> UpdateBalanceAsync(string userId, int newBalance, string action, string? description = null)
        {
            try
            {
                var credit = await _repository.GetByUserIdAsync(userId);
                if (credit == null)
                    return AvinyaAICRM.Shared.Helper.CommonHelper.BadRequestResponseMessage("User credit not found");
                // Treat `newBalance` as an amount to add to existing balance
                var addedAmount = newBalance;
                credit.Balance = credit.Balance + addedAmount;
                credit.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateUserCreditAsync(credit);

                await _repository.AddTransactionAsync(new CreditTransaction
                {
                    UserCreditId = credit.Id,
                    Amount = addedAmount,
                    Action = action,
                    Description = description,
                    Timestamp = DateTime.UtcNow
                });

                return AvinyaAICRM.Shared.Helper.CommonHelper.SuccessResponseMessage("Balance updated", credit);
            }
            catch (Exception ex)
            {
                return AvinyaAICRM.Shared.Helper.CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<AvinyaAICRM.Shared.Model.ResponseModel> GetUserCreditsAsync(AvinyaAICRM.Application.DTOs.User.UserCreditFilterRequest request)
        {
            try
            {
                var res = await _repository.GetUserCreditsAsync(request);
                return AvinyaAICRM.Shared.Helper.CommonHelper.GetResponseMessage(res);
            }
            catch (Exception ex)
            {
                return AvinyaAICRM.Shared.Helper.CommonHelper.ExceptionMessage(ex);
            }
        }

        public async Task<AvinyaAICRM.Shared.Model.ResponseModel> GetTransactionsByUserIdAsync(string userId, int pageNumber, int pageSize)
        {
            try
            {
                var res = await _repository.GetTransactionsByUserIdAsync(userId, pageNumber, pageSize);
                return AvinyaAICRM.Shared.Helper.CommonHelper.GetResponseMessage(res);
            }
            catch (Exception ex)
            {
                return AvinyaAICRM.Shared.Helper.CommonHelper.ExceptionMessage(ex);
            }
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

using AvinyaAICRM.Application.DTOs.User;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Domain.Entities.User;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AvinyaAICRM.Infrastructure.Repositories.User
{
    public class UserCreditRepository : IUserCreditRepository
    {
        private readonly AppDbContext _context;

        public UserCreditRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddTransactionAsync(CreditTransaction transaction)
        {
            await _context.CreditTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task AddUserCreditAsync(UserCredit credit)
        {
            await _context.UserCredits.AddAsync(credit);
            await _context.SaveChangesAsync();
        }

        public async Task<UserCredit?> GetByUserIdAsync(string userId)
        {
            return await _context.UserCredits.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<PagedResult<AvinyaAICRM.Application.DTOs.User.UserCreditListItemDto>> GetUserCreditsAsync(UserCreditFilterRequest request)
        {
            // join with users to allow searching by name/email/phone
            var query = from uc in _context.UserCredits.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on uc.UserId equals u.Id into uj
                        from u in uj.DefaultIfEmpty()
                        select new { Credit = uc, User = u };

            if (!string.IsNullOrEmpty(request.UserId))
                query = query.Where(x => x.Credit.UserId == request.UserId);

            if (request.TenantId.HasValue)
                query = query.Where(x => x.Credit.TenantId == request.TenantId.Value);

            if (!string.IsNullOrEmpty(request.Search))
            {
                var s = request.Search.Trim();
                query = query.Where(x => (x.User != null && (
                    (x.User.FullName != null && EF.Functions.Like(x.User.FullName, $"%{s}%")) ||
                    (x.User.Email != null && EF.Functions.Like(x.User.Email, $"%{s}%")) ||
                    (x.User.PhoneNumber != null && EF.Functions.Like(x.User.PhoneNumber, $"%{s}%"))
                )));
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.Credit.UpdatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new AvinyaAICRM.Application.DTOs.User.UserCreditListItemDto
                {
                    Id = x.Credit.Id,
                    UserId = x.Credit.UserId,
                    TenantId = x.Credit.TenantId,
                    Balance = x.Credit.Balance,
                    UpdatedAt = x.Credit.UpdatedAt,
                    CreatedAt = x.Credit.CreatedAt,
                    FullName = x.User != null ? x.User.FullName : null,
                    Email = x.User != null ? x.User.Email : null,
                    PhoneNumber = x.User != null ? x.User.PhoneNumber : null
                })
                .ToListAsync();

            return new PagedResult<AvinyaAICRM.Application.DTOs.User.UserCreditListItemDto>
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)request.PageSize),
                Data = data
            };
        }

        public async Task<PagedResult<AvinyaAICRM.Application.DTOs.User.CreditTransactionDto>> GetTransactionsByUserIdAsync(string userId, int pageNumber, int pageSize)
        {
            var credit = await _context.UserCredits.FirstOrDefaultAsync(x => x.UserId == userId);
            if (credit == null)
            {
                return new PagedResult<AvinyaAICRM.Application.DTOs.User.CreditTransactionDto>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = 0,
                    TotalPages = 0,
                    Data = new List<AvinyaAICRM.Application.DTOs.User.CreditTransactionDto>()
                };
            }
            var query = from ct in _context.CreditTransactions.AsNoTracking()
                        join uc in _context.UserCredits.AsNoTracking() on ct.UserCreditId equals uc.Id
                        join u in _context.Users.AsNoTracking() on uc.UserId equals u.Id into uj
                        from u in uj.DefaultIfEmpty()
                        where ct.UserCreditId == credit.Id
                        select new { Transaction = ct, User = u };

            var total = await query.CountAsync();

            var data = await query.OrderByDescending(x => x.Transaction.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AvinyaAICRM.Application.DTOs.User.CreditTransactionDto
                {
                    Id = x.Transaction.Id,
                    UserCreditId = x.Transaction.UserCreditId,
                    Action = x.Transaction.Action,
                    Amount = x.Transaction.Amount,
                    Description = x.Transaction.Description,
                    Timestamp = x.Transaction.Timestamp,
                    UserId = x.User != null ? x.User.Id : null,
                    FullName = x.User != null ? x.User.FullName : null,
                    Email = x.User != null ? x.User.Email : null,
                    PhoneNumber = x.User != null ? x.User.PhoneNumber : null
                })
                .ToListAsync();

            return new PagedResult<AvinyaAICRM.Application.DTOs.User.CreditTransactionDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data
            };
        }

        public async Task UpdateUserCreditAsync(UserCredit credit)
        {
            _context.UserCredits.Update(credit);
            await _context.SaveChangesAsync();
        }
    }
}

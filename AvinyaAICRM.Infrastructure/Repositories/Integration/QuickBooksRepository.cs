using AvinyaAICRM.Domain.Entities.QuickBook;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class QuickBooksRepository
{
    private readonly AppDbContext _context;

    public QuickBooksRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SaveConnectionAsync(string realmId, string accessToken, string refreshToken, int expiresIn)
    {
        var existing = await _context.QuickBooksConnections
            .FirstOrDefaultAsync(x => x.RealmId == realmId);

        var expiry = DateTime.Now.AddSeconds(expiresIn);

        if (existing != null)
        {
            existing.AccessToken = accessToken;
            existing.RefreshToken = refreshToken;
            existing.TokenExpiry = expiry;
            existing.UpdatedDate = DateTime.Now;
        }
        else
        {
            _context.QuickBooksConnections.Add(new QuickBooksConnection
            {
                RealmId = realmId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenExpiry = expiry,
                CreatedDate = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<QuickBooksConnection?> GetConnectionAsync()
    {
        return await _context.QuickBooksConnections.FirstOrDefaultAsync();
    }
}
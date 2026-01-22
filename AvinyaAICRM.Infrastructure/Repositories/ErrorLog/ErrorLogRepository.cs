using AvinyaAICRM.Application.Interfaces.RepositoryInterface;
using AvinyaAICRM.Domain.Entities.ErrorLogs;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace AvinyaAICRM.Infrastructure.Repositories.ErrorLog
{
    public class ErrorLogRepository : IErrorLogRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ErrorLogRepository> _logger;

        public ErrorLogRepository(AppDbContext context, ILogger<ErrorLogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(ErrorLogs errorLog)
        {
            try
            {
                await _context.ErrorLogs.AddAsync(errorLog);
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while logging details in ErrorLog Table");
                throw;
            }
        }
    }
}

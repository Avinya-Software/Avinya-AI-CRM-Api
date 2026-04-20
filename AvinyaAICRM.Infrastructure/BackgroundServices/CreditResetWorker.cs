using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.BackgroundServices
{
    public class CreditResetWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CreditResetWorker> _logger;
        private const int RESET_AMOUNT = 15000;

        public CreditResetWorker(IServiceProvider serviceProvider, ILogger<CreditResetWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Credit Reset Worker started (IST Schedule).");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Get current time in Indian Standard Time
                var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

                // Calculate next midnight in IST
                var nextRunIst = nowIst.Date.AddDays(1);
                var delay = nextRunIst - nowIst;

                _logger.LogInformation("Next IST credit reset scheduled for {NextRun} (in {Delay} hours)", nextRunIst, delay.TotalHours);

                try
                {
                    await Task.Delay(delay, stoppingToken);

                    _logger.LogInformation("Midnight reached. Starting daily credit reset...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var creditService = scope.ServiceProvider.GetRequiredService<ICreditService>();
                        await creditService.ResetAllBalancesAsync(RESET_AMOUNT);
                    }

                    _logger.LogInformation("Daily credit reset completed successfully.");
                }
                catch (TaskCanceledException)
                {
                    // Exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during daily credit reset.");
                    // Wait a bit before retrying if something went wrong
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
}

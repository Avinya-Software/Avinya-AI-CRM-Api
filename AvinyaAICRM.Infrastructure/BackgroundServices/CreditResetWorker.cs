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
        private const int RESET_AMOUNT = 30;

        public CreditResetWorker(IServiceProvider serviceProvider, ILogger<CreditResetWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Credit Reset Worker started (Invisible Persistence Mode).");

            var istZone = GetIstTimeZone();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var creditService = scope.ServiceProvider.GetRequiredService<ICreditService>();
                        
                        // 1. Check when the last reset happened (in Local)
                        var lastResetLocal = await creditService.GetLastResetDateAsync();
                        
                        // 2. Use current local time
                        var nowLocal = DateTime.Now;

                        // 3. Logic: If it hasn't been reset today, do it now.
                        if (!lastResetLocal.HasValue || lastResetLocal.Value.Date < nowLocal.Date)
                        {
                            _logger.LogInformation("Last reset was {LastReset}. Resetting all balances to {Amount}...", lastResetLocal?.ToString() ?? "Never", RESET_AMOUNT);
                            
                            int updatedCount = await creditService.ResetAllBalancesAsync(RESET_AMOUNT);
                            
                            _logger.LogInformation("Credit reset completed successfully. Updated {Count} users.", updatedCount);
                        }
                        else
                        {
                            _logger.LogInformation("Credits were already reset today ({LastResetDate}). Skipping.", lastResetLocal.Value.ToShortDateString());
                        }
                    }

                    // 4. Wait for 5 minutes before checking again (Safe for production/Shared Hosting)
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during persistent credit reset cycle.");
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
            }
        }

        private TimeZoneInfo GetIstTimeZone()
        {
            try
            {
                // Windows ID: "India Standard Time"
                // Linux/IANA ID: "Asia/Kolkata"
                var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                var tzId = isWindows ? "India Standard Time" : "Asia/Kolkata";
                
                return TimeZoneInfo.FindSystemTimeZoneById(tzId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not find standard IST timezone ID. Falling back to UTC+5:30 offset.");
                return TimeZoneInfo.CreateCustomTimeZone("IST-Fallback", TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30), "IST", "IST");
            }
        }
    }
}

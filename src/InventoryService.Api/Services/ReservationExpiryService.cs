namespace InventoryService.Api.Services;

public sealed class ReservationExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var reservations = scope.ServiceProvider.GetRequiredService<ReservationService>();

                var expired = await reservations.ExpireOverdueAsync(stoppingToken);
                if (expired > 0)
                    logger.LogInformation("Expired {Count} unpaid reservations and released their stock", expired);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to expire reservations; will retry on the next run");
            }
        }
    }
}

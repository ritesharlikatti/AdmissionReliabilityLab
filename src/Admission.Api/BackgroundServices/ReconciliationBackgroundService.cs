using Admission.Api.Services;

namespace Admission.Api.BackgroundServices;

public class ReconciliationBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReconciliationBackgroundService> _logger;

    public ReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ReconciliationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var intervalSeconds =
            _configuration.GetValue<int?>(
                "Reconciliation:IntervalSeconds")
            ?? 30;

        var interval =
            TimeSpan.FromSeconds(intervalSeconds);

        _logger.LogInformation(
            "Background reconciliation started. Interval: {IntervalSeconds} seconds.",
            intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    interval,
                    stoppingToken);

                using var scope =
                    _scopeFactory.CreateScope();

                var applicationService =
                    scope.ServiceProvider
                        .GetRequiredService<IApplicationService>();

                var changedCount =
                    await applicationService
                        .ReconcileProcessingApplicationsAsync(
                            stoppingToken);

                _logger.LogInformation(
                    "Background reconciliation cycle completed. " +
                    "{ChangedCount} application(s) changed.",
                    changedCount);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected failure during background reconciliation.");
            }
        }

        _logger.LogInformation(
            "Background reconciliation stopped.");
    }
}
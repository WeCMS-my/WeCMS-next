using WeCms.Shared.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dapper;

namespace WeCms.Infrastructure.Data;

public sealed class TwoFactorTicketCleanupService : IHostedService, IDisposable
{
    private readonly IDbConnectionFactory _db;
    private readonly IClock _clock;
    private readonly PeriodicTimer _timer;
    private readonly ILogger<TwoFactorTicketCleanupService> _logger;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public TwoFactorTicketCleanupService(IDbConnectionFactory db, IClock clock, ILogger<TwoFactorTicketCleanupService> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loop = Task.Run(() => LoopAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is not null)
            await _cts.CancelAsync();
        if (_loop is not null)
            await _loop.WaitAsync(cancellationToken);
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        // Wait a bit on startup to let migrations complete
        try { await Task.Delay(TimeSpan.FromSeconds(30), ct); } catch (OperationCanceledException) { return; }

        while (await _timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await using var c = await _db.OpenAsync(ct);
                var deleted = await c.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM sys_two_factor_ticket WHERE expires_at < @N",
                    new { N = _clock.UtcNow.DateTime },
                    cancellationToken: ct));
                if (deleted > 0)
                    _logger.LogInformation("Cleaned {Count} expired two-factor tickets", deleted);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean expired two-factor tickets");
            }
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _timer.Dispose();
    }
}

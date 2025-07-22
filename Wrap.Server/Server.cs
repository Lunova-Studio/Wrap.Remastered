using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Wrap.Remastered.Server;

public sealed class Server : BackgroundService {
    private readonly ILogger<Server>? _logger;
    private readonly ServerCoordinator _serverCoordinator;

    public Server(ServerCoordinator serverCoordinator, ILogger<Server> logger) {
        _logger = logger;
        _serverCoordinator = serverCoordinator;
    }

    public override async Task StartAsync(CancellationToken cancellationToken) {
        await _serverCoordinator.StartAsync();
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        await _serverCoordinator.StopAsync();
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _ = PollingAsync(_serverCoordinator.StatisticsPolling, 30000, stoppingToken);
        _ = PollingAsync(async () => await _serverCoordinator.KeepAliveSendPollingAsync(stoppingToken),
            10000, stoppingToken);
    }

    private static Task PollingAsync(Action action, int time, CancellationToken cancellationToken) => Task.Run(async () => {
        while (!cancellationToken.IsCancellationRequested) {
            action();
            await Task.Delay(time, cancellationToken);
        }
    }, cancellationToken);
}
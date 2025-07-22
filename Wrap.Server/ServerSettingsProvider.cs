using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Wrap.Remastered.Server.Models;

namespace Wrap.Remastered.Server;

public sealed class ServerSettingsProvider {
    public ServerConfiguration Configuration { get; } = new();

    public ServerSettingsProvider(IConfiguration configuration, ILogger<ServerSettingsProvider> logger) {
        Configuration.Port = configuration.GetValue<int>("Server:Port");
        Configuration.BossThreads = configuration.GetValue<int>("Server:BossThreads");
        Configuration.WorkerThreads = configuration.GetValue<int>("Server:WorkerThreads");
        Configuration.MaxConnections = configuration.GetValue<int>("Server:MaxConnections");
        Configuration.IsIPv4Only = configuration.GetValue<bool>("Server:IPv4Only");

        logger.LogInformation("Server settings initialized");
        logger.LogInformation("Actual listening port: {Port}", Configuration.Port);
        logger.LogInformation("Actual boss threads: {BossThreads}", Configuration.BossThreads);
        logger.LogInformation("Actual worker threads: {WorkerThreads}", Configuration.WorkerThreads);
        logger.LogInformation("Actual max connections: {MaxConnections}", Configuration.MaxConnections);
        logger.LogInformation("Is IPv4 only: {IPv4Only}", Configuration.IsIPv4Only);
    }
}
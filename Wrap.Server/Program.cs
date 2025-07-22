using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wrap.Shared.Extensions;
using Wrap.Shared.Managers;

namespace Wrap.Remastered.Server;

internal static class Program {
    internal static async Task Main(string[] args) {
        var builder = Host.CreateDefaultBuilder(args)
        .UseWrapLogger((hostingContext, loggerConfiguration) => {
            loggerConfiguration.Configuration(hostingContext.Configuration);
        });

        builder.ConfigureServices((hostContext, services) => {
            services.AddSingleton<CommandManager>();
            services.AddSingleton<ServerCoordinator>();
            services.AddSingleton<ServerSettingsProvider>();

            services.AddHostedService<CommandRegisterService>();
            services.AddHostedService<Server>();
        });

        var app = builder.Build();
        await app.RunAsync();
    }
}
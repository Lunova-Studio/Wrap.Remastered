using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Warp.Client;
using Wrap.Shared.Extensions;
using Wrap.Shared.Managers;

namespace Wrap.ClientConsole;

internal static class Program {
    internal static async Task Main(string[] args) {
        var builder = Host.CreateDefaultBuilder(args)
            .UseWrapLogger((hostingContext, loggerConfiguration) => {
                loggerConfiguration.Configuration(hostingContext.Configuration);
            });

        builder.ConfigureServices(service => {
            service.UseWrapClient();
            service.AddSingleton<CommandManager>();

            service.AddHostedService<CommandRegisterService>();
            service.AddHostedService<ClientService>();
        });

        var app = builder.Build();
        await app.RunAsync();
    }
}
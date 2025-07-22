using Microsoft.Extensions.DependencyInjection;
using Warp.Client.Interfaces;

namespace Warp.Client;

public static class HostingExtensions {
    public static IServiceCollection UseWrapClient(this IServiceCollection services) {
        services.AddSingleton<IClient, Client>();
        return services;
    }
}
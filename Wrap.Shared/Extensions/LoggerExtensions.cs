using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wrap.Shared.Logging;

namespace Wrap.Shared.Extensions;

public static class LoggerExtensions {
    public static IHostBuilder UseWrapLogger(this IHostBuilder builder,
        Action<HostBuilderContext, LoggerConfiguration> configureOptions) => builder.ConfigureLogging((hostingContext, logging) => {
            var configuration = new LoggerConfiguration();
            configureOptions(hostingContext, configuration);

            logging.ClearProviders();
            logging.AddProvider(new LoggerProvider(configuration));
        });
}
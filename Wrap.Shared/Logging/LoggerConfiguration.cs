using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Wrap.Shared.Logging;

public record LoggerConfiguration {
    private const string BASE_KEY = "Wrap:Console:";
    private const string DEFAULT_OUTPUTTEMPLATE = "[{Level}] {Message}{NewLine}{Exception}";

    public LogLevel MinimumLevel { get; set; }
    public string? OutputTemplate { get; set; }

    public void Configuration(IConfiguration configuration) {
        OutputTemplate = configuration.GetValue($"{BASE_KEY}outputTemplate", DEFAULT_OUTPUTTEMPLATE);
        MinimumLevel = configuration.GetValue($"{BASE_KEY}MinimumLevel:Default", LogLevel.Information);
    }
}
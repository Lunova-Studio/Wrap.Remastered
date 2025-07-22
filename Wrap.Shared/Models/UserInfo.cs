namespace Wrap.Shared.Models;

public record UserInfo {
    public string Name { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
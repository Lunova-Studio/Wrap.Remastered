using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp.Client.Models;

public record UserProfile {
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// P2P心跳间隔（秒）
    /// </summary>
    public int PeerHeartbeatInterval { get; set; } = 10;

    /// <summary>
    /// 是否启用代理转发
    /// </summary>
    public bool EnableProxyForwarding { get; set; } = true;

    /// <summary>
    /// 本地代理监听端口
    /// </summary>
    public int LocalProxyPort { get; set; } = 25565;

    /// <summary>
    /// 转发目标地址
    /// </summary>
    public string ProxyTargetAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// 转发目标端口
    /// </summary>
    public int ProxyTargetPort { get; set; } = 25565;

    private static string ProfilePath => Path.Combine(AppContext.BaseDirectory, "userprofile.json");

    public static UserProfile Load() {
        if (File.Exists(ProfilePath)) {
            var json = File.ReadAllText(ProfilePath);
            return JsonSerializer.Deserialize(json, UserProfileJsonContext.Default.UserProfile) ?? new UserProfile();
        }

        return new UserProfile();
    }

    public void Save() {
        var json = JsonSerializer.Serialize(this, UserProfileJsonContext.Default.UserProfile);
        File.WriteAllText(ProfilePath, json);
    }
}

[JsonSerializable(typeof(UserProfile))]
public sealed partial class UserProfileJsonContext : JsonSerializerContext;
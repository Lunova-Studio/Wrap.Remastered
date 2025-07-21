using System;

namespace Wrap.Remastered.Client.Configuration;

/// <summary>
/// 客户端配置类
/// </summary>
public class ClientConfiguration
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public string ServerAddress { get; set; } = "wrap.api.lunova.studio";

    /// <summary>
    /// 服务器端口
    /// </summary>
    public int ServerPort { get; set; } = 10270;

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// 重连间隔（秒）
    /// </summary>
    public int ReconnectInterval { get; set; } = 5;

    /// <summary>
    /// 最大重连次数
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 10;

    /// <summary>
    /// 是否启用自动重连
    /// </summary>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    /// 是否启用心跳
    /// </summary>
    public bool EnableHeartbeat { get; set; } = true;

    /// <summary>
    /// 心跳间隔（秒）
    /// </summary>
    public int HeartbeatInterval { get; set; } = 30;

    /// <summary>
    /// P2P心跳间隔（秒）
    /// </summary>
    public int PeerHeartbeatInterval { get; set; } = 10;
    
    /// <summary>
    /// 代理超时时间（秒）
    /// </summary>
    public int ProxyTimeout { get; set; } = 30;
    
    /// <summary>
    /// 最大代理连接数
    /// </summary>
    public int MaxProxyConnections { get; set; } = 100;
    
    /// <summary>
    /// 代理缓冲区大小
    /// </summary>
    public int ProxyBufferSize { get; set; } = 8192;

    /// <summary>
    /// 是否启用日志
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// 日志级别
    /// </summary>
    public string LogLevel { get; set; } = "Info";

    /// <summary>
    /// 缓冲区大小
    /// </summary>
    public int BufferSize { get; set; } = 8192;

    /// <summary>
    /// 验证配置
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrEmpty(ServerAddress))
            throw new ArgumentException("服务器地址不能为空", nameof(ServerAddress));

        if (ServerPort <= 0 || ServerPort > 65535)
            throw new ArgumentOutOfRangeException(nameof(ServerPort), "服务器端口必须在1-65535之间");

        if (ConnectionTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(ConnectionTimeout), "连接超时时间必须大于0");

        if (ReconnectInterval <= 0)
            throw new ArgumentOutOfRangeException(nameof(ReconnectInterval), "重连间隔必须大于0");

        if (MaxReconnectAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxReconnectAttempts), "最大重连次数不能为负数");

        if (HeartbeatInterval <= 0)
            throw new ArgumentOutOfRangeException(nameof(HeartbeatInterval), "心跳间隔必须大于0");

        if (BufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(BufferSize), "缓冲区大小必须大于0");
    }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    /// <returns>默认配置</returns>
    public static ClientConfiguration CreateDefault()
    {
        return new ClientConfiguration();
    }

    /// <summary>
    /// 创建开发环境配置
    /// </summary>
    /// <returns>开发环境配置</returns>
    public static ClientConfiguration CreateDevelopment()
    {
        return new ClientConfiguration
        {
            ServerAddress = "127.0.0.1",
            ServerPort = 10270,
            ConnectionTimeout = 10,
            ReconnectInterval = 3,
            MaxReconnectAttempts = 5,
            EnableAutoReconnect = true,
            EnableHeartbeat = true,
            HeartbeatInterval = 15,
            EnableLogging = true,
            LogLevel = "Debug",
            BufferSize = 4096
        };
    }

    /// <summary>
    /// 创建生产环境配置
    /// </summary>
    /// <returns>生产环境配置</returns>
    public static ClientConfiguration CreateProduction()
    {
        return new ClientConfiguration
        {
            ServerAddress = "127.0.0.1",
            ServerPort = 10270,
            ConnectionTimeout = 60,
            ReconnectInterval = 10,
            MaxReconnectAttempts = 20,
            EnableAutoReconnect = true,
            EnableHeartbeat = true,
            HeartbeatInterval = 60,
            EnableLogging = true,
            LogLevel = "Info",
            BufferSize = 16384
        };
    }
} 
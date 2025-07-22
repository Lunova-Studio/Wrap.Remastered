namespace Wrap.Remastered.Server.Models;

/// <summary>
/// 服务器配置类
/// </summary>
public sealed class ServerConfiguration {
    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; set; } = 10270;

    /// <summary>
    /// Boss线程数
    /// </summary>
    public int BossThreads { get; set; } = 1;

    /// <summary>
    /// Worker线程数
    /// </summary>
    public int WorkerThreads { get; set; } = 4;

    /// <summary>
    /// 最大连接数
    /// </summary>
    public int MaxConnections { get; set; } = 1000;

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 300;

    /// <summary>
    /// 是否启用统计信息
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// 统计信息输出间隔（秒）
    /// </summary>
    public int StatisticsInterval { get; set; } = 60;

    /// <summary>
    /// 是否只接受 IPv4 连接
    /// </summary>
    public bool IsIPv4Only { get; set; } = true;

    /// <summary>
    /// 验证配置
    /// </summary>
    public void Validate() {
        if (Port <= 0 || Port > 65535)
            throw new ArgumentOutOfRangeException(nameof(Port), "端口号必须在1-65535之间");

        if (BossThreads <= 0)
            throw new ArgumentOutOfRangeException(nameof(BossThreads), "Boss线程数必须大于0");

        if (WorkerThreads <= 0)
            throw new ArgumentOutOfRangeException(nameof(WorkerThreads), "Worker线程数必须大于0");

        if (MaxConnections <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxConnections), "最大连接数必须大于0");

        if (ConnectionTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(ConnectionTimeout), "连接超时时间必须大于0");

        if (StatisticsInterval <= 0)
            throw new ArgumentOutOfRangeException(nameof(StatisticsInterval), "统计信息输出间隔必须大于0");
    }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    /// <returns>默认配置</returns>
    public static ServerConfiguration CreateDefault() {
        return new ServerConfiguration();
    }

    /// <summary>
    /// 创建开发环境配置
    /// </summary>
    /// <returns>开发环境配置</returns>
    public static ServerConfiguration CreateDevelopment() {
        return new ServerConfiguration {
            Port = 10270,
            BossThreads = 1,
            WorkerThreads = 2,
            MaxConnections = 100,
            ConnectionTimeout = 300,
            StatisticsInterval = 30,
            EnableStatistics = true
        };
    }

    /// <summary>
    /// 创建生产环境配置
    /// </summary>
    /// <returns>生产环境配置</returns>
    public static ServerConfiguration CreateProduction() {
        return new ServerConfiguration {
            Port = 10270,
            BossThreads = 2,
            WorkerThreads = 8,
            MaxConnections = 10000,
            ConnectionTimeout = 600,
            StatisticsInterval = 300,
            EnableStatistics = true
        };
    }
}
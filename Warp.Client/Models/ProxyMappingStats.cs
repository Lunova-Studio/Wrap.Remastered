using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp.Client.Models;

/// <summary>
/// 代理映射统计信息
/// </summary>
public record ProxyMappingStats {
    /// <summary>
    /// 总连接数
    /// </summary>
    public int TotalConnections { get; set; }

    /// <summary>
    /// 活跃连接数
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// 总用户数
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// 总传输字节数
    /// </summary>
    public long TotalBytesTransferred { get; set; }

    /// <summary>
    /// 平均每个连接的传输字节数
    /// </summary>
    public double AverageBytesPerConnection => TotalConnections > 0 ? (double)TotalBytesTransferred / TotalConnections : 0;

    /// <summary>
    /// 平均每个用户的连接数
    /// </summary>
    public double AverageConnectionsPerUser => TotalUsers > 0 ? (double)TotalConnections / TotalUsers : 0;

    /// <summary>
    /// 传输字节数的人类可读格式
    /// </summary>
    public string TotalBytesTransferredFormatted {
        get {
            if (TotalBytesTransferred < 1024)
                return $"{TotalBytesTransferred} B";

            if (TotalBytesTransferred < 1024 * 1024)
                return $"{TotalBytesTransferred / 1024.0:F1} KB";

            if (TotalBytesTransferred < 1024 * 1024 * 1024)
                return $"{TotalBytesTransferred / (1024.0 * 1024):F1} MB";

            return $"{TotalBytesTransferred / (1024.0 * 1024 * 1024):F1} GB";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrap.Remastered.Network.Pool;

/// <summary>
/// 连接池统计信息
/// </summary>
public class NetworkPoolStatistics
{
    /// <summary>
    /// 最大连接数
    /// </summary>
    public int MaxConnections { get; set; }

    /// <summary>
    /// 当前连接数
    /// </summary>
    public int CurrentConnections { get; set; }

    /// <summary>
    /// 可用连接数
    /// </summary>
    public int AvailableConnections { get; set; }

    /// <summary>
    /// 连接池是否已满
    /// </summary>
    public bool IsFull { get; set; }

    /// <summary>
    /// 连接池是否为空
    /// </summary>
    public bool IsEmpty { get; set; }

    /// <summary>
    /// 活跃连接数
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// 断开连接数
    /// </summary>
    public int DisconnectedConnections { get; set; }
}

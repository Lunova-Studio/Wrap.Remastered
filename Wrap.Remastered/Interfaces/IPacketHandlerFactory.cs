using Wrap.Remastered.Server.Handlers.PacketHandlers;

namespace Wrap.Remastered.Interfaces;

/// <summary>
/// 数据包处理器工厂接口
/// </summary>
public interface IPacketHandlerFactory
{
    /// <summary>
    /// 获取数据包处理器
    /// </summary>
    /// <param name="packetType">数据包类型</param>
    /// <returns>数据包处理器</returns>
    IPacketHandler? GetHandler(int packetType);
}

using DotNetty.Transport.Channels;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ServerBound;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public class PluginMessagePacketHandler : BasePacketHandler
{
    public static event Action<IChannel, PluginMessagePacket>? PluginMessageReceived;

    public PluginMessagePacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var pluginMsg = PluginMessagePacket.Serializer.Deserialize(packet.Data) as PluginMessagePacket;
        if (pluginMsg == null)
        {
            Server.GetLoggingService().LogWarning("插件消息", "PluginMessagePacket 反序列化失败");
            return;
        }
        LogInfo(channel, packet, $"收到插件消息: Key={pluginMsg.NamespacedKey}, Data.Length={pluginMsg.Data.Length}");
        PluginMessageReceived?.Invoke(channel, pluginMsg);
    }
} 
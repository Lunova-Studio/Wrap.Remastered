using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class PluginMessagePacketHandler : BasePacketHandler {
    public static event Action<IChannel, PluginMessagePacket>? PluginMessageReceived;

    public PluginMessagePacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (PluginMessagePacket.Serializer.Deserialize(packet.Data) is not PluginMessagePacket pluginMsg) {
            Server.Logger.LogWarning("PluginMessagePacket 反序列化失败");
            return;
        }

        LogInfo(channel, packet, $"收到插件消息: Key={pluginMsg.NamespacedKey}, Data.Length={pluginMsg.Data.Length}");
        PluginMessageReceived?.Invoke(channel, pluginMsg);
    }
}
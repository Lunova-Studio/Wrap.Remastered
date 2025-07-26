using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Shared.Network.Serializers.Client;

public sealed class PingInfoPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PingInfoPacket packet = new();
        using MemoryStream stream = new(data);
        packet.Ping = TimeSpan.FromTicks(stream.ReadInt64());
        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PingInfoPacket pingInfoPacket)
            throw new ArgumentException("Invalid packet type for serialization.", nameof(obj));

        using MemoryStream stream = new();
        stream.WriteInt64(pingInfoPacket.Ping.Ticks);
        return stream.ToArray();
    }
}

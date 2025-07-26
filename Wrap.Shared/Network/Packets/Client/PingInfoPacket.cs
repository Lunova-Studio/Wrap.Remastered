using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public class PingInfoPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PingInfoPacketSerializer();

    public TimeSpan Ping { get; set; }

    public PingInfoPacket(TimeSpan ping)
    {
        Ping = ping;
    }

    public PingInfoPacket() { }

    public ISerializer<IPacket> GetSerializer() => Serializer;

    public void OnSerialize(ref byte[] data) { }

    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PingInfoPacket;
}

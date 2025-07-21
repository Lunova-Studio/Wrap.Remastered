using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class DisconnectPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new DisconnectPacketSerializer();

    public string Reason { get; set; } = string.Empty;
    public DateTime DisconnectTime { get; set; } = DateTime.Now;

    public DisconnectPacket(string? reason)
    {
        Reason = reason == null ? string.Empty : reason;
        DisconnectTime = DateTime.Now;
    }

    public DisconnectPacket() { }

    public ISerializer<IPacket> GetSerializer() => Serializer;

    public void OnSerialize(ref byte[] data) { }

    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.DisconnectPacket;
}

public class DisconnectPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        DisconnectPacket packet = new();
        using MemoryStream stream = new(data);
        packet.Reason = stream.ReadString();
        packet.DisconnectTime = DateTime.FromBinary(stream.ReadInt64());
        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not DisconnectPacket disconnectPacket) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteString(disconnectPacket.Reason);
        stream.WriteInt64(disconnectPacket.DisconnectTime.ToBinary());
        return stream.ToArray();
    }
}

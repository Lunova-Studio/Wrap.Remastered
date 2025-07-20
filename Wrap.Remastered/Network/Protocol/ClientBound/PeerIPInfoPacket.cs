using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

/// <summary>
/// P2P IP信息交换包
/// </summary>
public class PeerIPInfoPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerIPInfoPacketSerializer();
    
    /// <summary>
    /// 目标用户ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 目标用户IP地址（字节数组）
    /// </summary>
    public byte[] TargetIPAddress { get; set; } = new byte[4];
    
    /// <summary>
    /// 目标用户端口
    /// </summary>
    public int TargetPort { get; set; }

    public PeerIPInfoPacket() { }

    public PeerIPInfoPacket(string targetUserId, byte[] targetIPAddress, int targetPort)
    {
        TargetUserId = targetUserId;
        TargetIPAddress = targetIPAddress;
        TargetPort = targetPort;
    }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 使用默认序列化
    }

    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PeerIPInfoPacket;
}

public class PeerIPInfoPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerIPInfoPacket packet = new();

        using MemoryStream stream = new(data);

        packet.TargetUserId = stream.ReadString();
        packet.TargetIPAddress = stream.ReadBytes(4);
        packet.TargetPort = stream.ReadInt();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerIPInfoPacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.TargetUserId);
        stream.WriteBytes(packet.TargetIPAddress);
        stream.WriteInt(packet.TargetPort);

        return stream.ToArray();
    }
} 
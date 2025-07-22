using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public class LoginFailedPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new LoginFailedPacketSerializer();

    public int ErrorCode { get; set; } = 0;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime FailTime { get; set; } = DateTime.Now;

    public LoginFailedPacket(int errorCode, string errorMessage) {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        FailTime = DateTime.Now;
    }

    public LoginFailedPacket() { }

    public ISerializer<IPacket> GetSerializer() {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data) {
        // 可以在这里添加额外的序列化逻辑
    }

    public ClientBoundPacketType GetPacketType() {
        return ClientBoundPacketType.LoginFailedPacket;
    }
}
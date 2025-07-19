using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Network.Protocol.ServerBound;

namespace Wrap.Remastered.Network.Protocol;

public interface IClientBoundPacket : IPacket
{
    public static Dictionary<ClientBoundPacketType, ISerializer<IPacket>> Serializers = new()
    {
        { ClientBoundPacketType.LoginSucceedPacket, new LoginSucceedPacketSerializer() },
        { ClientBoundPacketType.LoginFailedPacket, new LoginFailedPacketSerializer() },
        { ClientBoundPacketType.DisconnectPacket, new DisconnectPacketSerializer() }
    };

    ClientBoundPacketType GetPacketType();
}

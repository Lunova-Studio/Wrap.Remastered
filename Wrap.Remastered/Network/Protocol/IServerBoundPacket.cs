using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ServerBound;

namespace Wrap.Remastered.Network.Protocol;

public interface IServerBoundPacket : IPacket
{
    public static Dictionary<ServerBoundPacketType, ISerializer<IPacket>> Serializers = new()
    {
        { ServerBoundPacketType.LoginPacket, new LoginPacketSerializer() }
    };

    ServerBoundPacketType GetPacketType();
}

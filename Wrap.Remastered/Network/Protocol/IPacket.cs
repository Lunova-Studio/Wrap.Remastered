using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol;

public interface IPacket : ISerializable<IPacket>
{

}

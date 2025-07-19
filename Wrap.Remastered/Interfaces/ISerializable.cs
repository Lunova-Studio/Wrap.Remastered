using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrap.Remastered.Interfaces;

public interface ISerializable<TSelf> where TSelf : ISerializable<TSelf>
{
    ISerializer<TSelf> GetSerializer();
    void OnSerialize(ref byte[] data);
}

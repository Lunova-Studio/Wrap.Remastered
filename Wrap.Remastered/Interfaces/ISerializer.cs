using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrap.Remastered.Interfaces;


public interface ISerializer { }
public interface ISerializer<T> : ISerializer
{
    byte[] Serialize(T obj);
    T Deserialize(byte[] data);
}

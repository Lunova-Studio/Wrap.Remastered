namespace Wrap.Shared.Interfaces;

public interface ISerializer;

public interface ISerializer<T> : ISerializer {
    byte[] Serialize(T obj);
    T Deserialize(byte[] data);
}

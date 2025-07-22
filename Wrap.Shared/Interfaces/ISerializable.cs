namespace Wrap.Shared.Interfaces;

public interface ISerializable<TSelf> where TSelf : ISerializable<TSelf> {
    void OnSerialize(ref byte[] data);
    ISerializer<TSelf> GetSerializer();
}
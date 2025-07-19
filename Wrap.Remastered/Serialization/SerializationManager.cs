using System;
using System.Collections.Generic;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Serialization;

/// <summary>
/// 序列化管理器，避免使用反射以支持 AOT
/// </summary>
public class SerializationManager
{
    private static readonly Dictionary<Type, ISerializer<object>> _serializers = new();
    private static readonly object _lock = new();

    /// <summary>
    /// 注册序列化器
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="serializer">序列化器</param>
    public static void RegisterSerializer<T>(ISerializer<T> serializer) where T : class
    {
        lock (_lock)
        {
            _serializers[typeof(T)] = new SerializerAdapter<T>(serializer);
        }
    }

    /// <summary>
    /// 获取序列化器
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>序列化器</returns>
    public static ISerializer<T>? GetSerializer<T>() where T : class
    {
        lock (_lock)
        {
            if (_serializers.TryGetValue(typeof(T), out var serializer))
            {
                return ((SerializerAdapter<T>)serializer).InnerSerializer;
            }
            return null;
        }
    }

    /// <summary>
    /// 检查是否有序列化器
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>是否有序列化器</returns>
    public static bool HasSerializer<T>() where T : class
    {
        lock (_lock)
        {
            return _serializers.ContainsKey(typeof(T));
        }
    }

    /// <summary>
    /// 序列化对象
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="obj">对象</param>
    /// <returns>序列化后的字节数组</returns>
    public static byte[] Serialize<T>(T obj) where T : class
    {
        var serializer = GetSerializer<T>();
        if (serializer == null)
        {
            throw new InvalidOperationException($"未找到类型 {typeof(T).Name} 的序列化器");
        }

        return serializer.Serialize(obj);
    }

    /// <summary>
    /// 反序列化对象
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="data">字节数组</param>
    /// <returns>反序列化后的对象</returns>
    public static T Deserialize<T>(byte[] data) where T : class
    {
        var serializer = GetSerializer<T>();
        if (serializer == null)
        {
            throw new InvalidOperationException($"未找到类型 {typeof(T).Name} 的序列化器");
        }

        return serializer.Deserialize(data);
    }

    /// <summary>
    /// 清除所有序列化器
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _serializers.Clear();
        }
    }

    /// <summary>
    /// 序列化器适配器
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    private class SerializerAdapter<T> : ISerializer<object> where T : class
    {
        public ISerializer<T> InnerSerializer { get; }

        public SerializerAdapter(ISerializer<T> serializer)
        {
            InnerSerializer = serializer;
        }

        public object Deserialize(byte[] data)
        {
            return InnerSerializer.Deserialize(data);
        }

        public byte[] Serialize(object obj)
        {
            if (obj is T typedObj)
            {
                return InnerSerializer.Serialize(typedObj);
            }
            throw new ArgumentException($"对象类型不匹配，期望 {typeof(T).Name}，实际 {obj.GetType().Name}");
        }
    }
} 
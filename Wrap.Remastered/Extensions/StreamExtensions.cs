using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrap.Remastered.Extensions;

public static class StreamExtensions
{
    public static int ReadVarInt(this Stream stream)
    {
        uint result = 0;
        int length = 0;
        while (true)
        {
            byte current = stream.ReadUInt8();
            result |= (current & 0x7Fu) << length++ * 7;
            if (length > 5)
                throw new InvalidDataException("VarInt may not be longer than 28 bits.");
            if ((current & 0x80) != 128)
                break;
        }
        return (int)result;
    }
    public static void WriteVarInt(this Stream stream, int _value)
    {
        uint value = (uint)_value;
        while (true)
        {
            if ((value & 0xFFFFFF80u) == 0)
            {
                stream.WriteUInt8((byte)value);
                break;
            }
            stream.WriteUInt8((byte)(value & 0x7F | 0x80));
            value >>= 7;
        }
    }
    public static string ReadString(this Stream stream)
    {
        long length = stream.ReadInt32();
        if (length == 0) return string.Empty;
        byte[] buffer = new byte[length];
        stream.Read(buffer);
        return Encoding.UTF8.GetString(buffer);
    }
    public static void WriteString(this Stream stream, string value)
    {
        stream.WriteInt32(Encoding.UTF8.GetByteCount(value));
        if (value.Length > 0)
            stream.Write(Encoding.UTF8.GetBytes(value));
    }
    public static long ReadInt64(this Stream stream)
    {
        byte[] buffer = new byte[8];
        stream.Read(buffer);
        return BitConverter.ToInt64(buffer);
    }
    public static int ReadInt32(this Stream stream)
    {
        byte[] buffer = new byte[4];
        stream.Read(buffer);
        return BitConverter.ToInt32(buffer);
    }
    public static short ReadInt16(this Stream stream)
    {
        byte[] buffer = new byte[2];
        stream.Read(buffer);
        return BitConverter.ToInt16(buffer);
    }
    public static sbyte ReadInt8(this Stream stream)
    {
        return (sbyte)stream.ReadByte();
    }
    public static ulong ReadUInt64(this Stream stream)
    {
        byte[] buffer = new byte[8];
        stream.Read(buffer);
        return BitConverter.ToUInt64(buffer);
    }
    public static uint ReadUInt32(this Stream stream)
    {
        byte[] buffer = new byte[4];
        stream.Read(buffer);
        return BitConverter.ToUInt32(buffer);
    }
    public static ushort ReadUInt16(this Stream stream)
    {
        byte[] buffer = new byte[2];
        stream.Read(buffer);
        return BitConverter.ToUInt16(buffer);
    }
    public static byte ReadUInt8(this Stream stream)
    {
        return (byte)stream.ReadByte();
    }
    public static void WriteInt64(this Stream stream, long value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        stream.Write(buffer);
    }
    public static void WriteInt32(this Stream stream, int value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        stream.Write(buffer);
    }
    public static void WriteInt16(this Stream stream, short value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        stream.Write(buffer);
    }
    public static void WriteInt8(this Stream stream, sbyte value)
    {
        stream.WriteByte((byte)value);
    }
    public static void WriteUInt64(this Stream stream, ulong value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        stream.Write(buffer);
    }
    public static void WriteUInt32(this Stream stream, uint value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        stream.Write(buffer);
    }
    public static void WriteUInt16(this Stream stream, ushort value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        stream.Write(buffer);
    }
    public static void WriteUInt8(this Stream stream, byte value)
    {
        stream.WriteByte(value);
    }
}

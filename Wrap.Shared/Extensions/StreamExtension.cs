using System.Buffers;
using System.Text;

namespace Wrap.Shared.Extensions;

public static class StreamExtensions {
    private const int BITMASK = 0x7F;
    private const int MAX_VARINT_LENGTH = 5;
    private const int CONTINUATION_BIT = 0x80;

    #region Read

    public static int ReadVarInt(this Stream stream) {
        int shift = 0;
        uint result = 0;

        for (int i = 0; i < MAX_VARINT_LENGTH; i++) {
            var current = stream.ReadUInt8();
            result |= (uint)(current & BITMASK) << shift;
            shift += 7;

            if ((current & CONTINUATION_BIT) is 0)
                return (int)result;
        }

        throw new InvalidDataException("VarInt is too long, maximum 5 bytes allowed.");
    }

    public static string ReadString(this Stream stream) {
        long length = stream.ReadInt32();

        if (length < 0)
            throw new InvalidDataException("String length cannot be negative.");

        if (length is 0)
            return string.Empty;

        var buffer = ArrayPool<byte>.Shared.Rent((int)length);

        try {
            int bytesRead = stream.Read(buffer, 0, (int)length);
            if (bytesRead < length)
                throw new EndOfStreamException("Unexpected end of stream.");

            return Encoding.UTF8.GetString(buffer, 0, (int)length);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static long ReadInt64(this Stream stream) {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(8);

        try {
            stream.ReadIntoBuffer(buffer, 8);
            return BitConverter.ToInt64(buffer);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static int ReadInt32(this Stream stream) {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(4);

        try {
            stream.ReadIntoBuffer(buffer, 4);
            return BitConverter.ToInt32(buffer);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static short ReadInt16(this Stream stream) {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(2);

        try {
            stream.ReadIntoBuffer(buffer, 2);
            return BitConverter.ToInt16(buffer);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static sbyte ReadInt8(this Stream stream) {
        int byteRead = stream.ReadByte();

        if (byteRead == -1)
            throw new EndOfStreamException("Stream ended unexpectedly.");

        return (sbyte)byteRead;
    }

    public static ulong ReadUInt64(this Stream stream) {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(8);

        try {
            stream.ReadIntoBuffer(buffer, 8);
            return BitConverter.ToUInt64(buffer);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static uint ReadUInt32(this Stream stream) {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(4);

        try {
            stream.ReadIntoBuffer(buffer, 4);
            return BitConverter.ToUInt32(buffer);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static ushort ReadUInt16(this Stream stream) {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(2);

        try {
            stream.ReadIntoBuffer(buffer, 2);
            return BitConverter.ToUInt16(buffer);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static byte ReadUInt8(this Stream stream) {
        int byteRead = stream.ReadByte();

        if (byteRead == -1)
            throw new EndOfStreamException("Stream ended unexpectedly.");

        return (byte)byteRead;
    }

    public static byte[] ReadBytes(this Stream stream, int count) {
        int rest = count;
        byte[] buffer = new byte[count];

        while (rest > 0) {
            int readed = stream.Read(buffer, count - rest, rest);
            rest -= readed;
        }

        return buffer;
    }

    public static bool ReadBool(this Stream stream) {
        byte next = (byte)stream.ReadByte();
        return (next is 1);
    }

    private static void ReadIntoBuffer(this Stream stream, byte[] buffer, int expectedLength) {
        int bytesRead = stream.Read(buffer, 0, expectedLength);

        if (bytesRead < expectedLength)
            throw new EndOfStreamException("Stream ended unexpectedly.");
    }

    #endregion

    #region Write

    public static void WriteVarInt(this Stream stream, int value) {
        uint unsignedValue = (uint)value;

        while (true) {
            if ((unsignedValue & 0xFFFFFF80u) is 0) {
                stream.WriteByte((byte)unsignedValue);
                break;
            }

            stream.WriteByte((byte)((unsignedValue & 0x7F) | 0x80));
            unsignedValue >>= 7;
        }
    }

    public static void WriteString(this Stream stream, string value) {
        byte[] encodedString = Encoding.UTF8.GetBytes(value);
        stream.WriteInt32(encodedString.Length);

        if (encodedString.Length > 0)
            stream.Write(encodedString, 0, encodedString.Length);
    }

    public static void WriteInt64(this Stream stream, long value) {
        WriteBytes(stream, BitConverter.GetBytes(value));
    }

    public static void WriteInt32(this Stream stream, int value) {
        WriteBytes(stream, BitConverter.GetBytes(value));
    }

    public static void WriteInt16(this Stream stream, short value) {
        WriteBytes(stream, BitConverter.GetBytes(value));
    }

    public static void WriteInt8(this Stream stream, sbyte value) {
        stream.WriteByte((byte)value);
    }

    public static void WriteUInt64(this Stream stream, ulong value) {
        WriteBytes(stream, BitConverter.GetBytes(value));
    }

    public static void WriteUInt32(this Stream stream, uint value) {
        WriteBytes(stream, BitConverter.GetBytes(value));
    }

    public static void WriteUInt16(this Stream stream, ushort value) {
        WriteBytes(stream, BitConverter.GetBytes(value));
    }

    public static void WriteUInt8(this Stream stream, byte value) {
        stream.WriteByte(value);
    }

    public static void WriteBytes(this Stream stream, byte[] buffer) {
        stream.Write(buffer, 0, buffer.Length);
    }

    public static void WriteBool(this Stream stream, bool value) {
        stream.WriteByte((byte)(value ? 1 : 0));
    }

    #endregion 
}
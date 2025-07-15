using System;
using System.IO;
using System.Text;

namespace GenPack.IO;

/// <summary>
/// Binary reader that respects endianness and string encoding settings
/// </summary>
public class EndianAwareBinaryReader : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly UnitEndian _endian;
    private readonly Encoding _encoding;
    private readonly bool _needsByteSwap;
    private bool _disposed;

    public EndianAwareBinaryReader(Stream input, UnitEndian endian, StringEncoding stringEncoding)
    {
        _endian = endian;
        _encoding = GetEncoding(stringEncoding);
        _reader = new BinaryReader(input, _encoding);
        _needsByteSwap = (_endian == UnitEndian.Big && BitConverter.IsLittleEndian) || 
                         (_endian == UnitEndian.Little && !BitConverter.IsLittleEndian);
    }

    private static Encoding GetEncoding(StringEncoding stringEncoding) => stringEncoding switch
    {
        StringEncoding.UTF8 => Encoding.UTF8,
        StringEncoding.UTF16 => Encoding.Unicode,
        StringEncoding.UTF32 => Encoding.UTF32,
        StringEncoding.ASCII => Encoding.ASCII,
        _ => Encoding.UTF8
    };

    public byte ReadByte() => _reader.ReadByte();
    public sbyte ReadSByte() => _reader.ReadSByte();

    public short ReadInt16()
    {
        if (_needsByteSwap)
        {
            var bytes = _reader.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }
        else
        {
            return _reader.ReadInt16();
        }
    }

    public ushort ReadUInt16()
    {
        if (_needsByteSwap)
        {
            var bytes = _reader.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }
        else
        {
            return _reader.ReadUInt16();
        }
    }

    public int ReadInt32()
    {
        if (_needsByteSwap)
        {
            var bytes = _reader.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
        else
        {
            return _reader.ReadInt32();
        }
    }

    public uint ReadUInt32()
    {
        if (_needsByteSwap)
        {
            var bytes = _reader.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
        else
        {
            return _reader.ReadUInt32();
        }
    }

    public long ReadInt64()
    {
        if (_needsByteSwap)
        {
            var bytes = _reader.ReadBytes(8);
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }
        else
        {
            return _reader.ReadInt64();
        }
    }

    public ulong ReadUInt64()
    {
        if (_needsByteSwap)
        {
            var bytes = _reader.ReadBytes(8);
            Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
        else
        {
            return _reader.ReadUInt64();
        }
    }

    public float ReadSingle()
    {
        if (_needsByteSwap)
        {
            var bytes = _reader.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }
        else
        {
            return _reader.ReadSingle();
        }
    }

    public double ReadDouble()
    {
        if (_needsByteSwap)
        {
            var bytes = _reader.ReadBytes(8);
            Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }
        else
        {
            return _reader.ReadDouble();
        }
    }

    public string ReadString()
    {
        var length = Read7BitEncodedInt();
        var bytes = _reader.ReadBytes(length);
        return _encoding.GetString(bytes);
    }

    public byte[] ReadBytes(int count) => _reader.ReadBytes(count);

    public int Read7BitEncodedInt()
    {
        // Read out an int 7 bits at a time. The high bit
        // of the byte when on means to continue reading more bytes.
        int count = 0;
        int shift = 0;
        byte b;
        do
        {
            // Check for a corrupted stream. Read a max of 5 bytes.
            // In a future version, add a DataFormatException.
            if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                throw new FormatException("Bad 7-bit encoded integer");

            // ReadByte handles end of stream cases for us.
            b = ReadByte();
            count |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
        return count;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _reader?.Dispose();
            _disposed = true;
        }
    }
}
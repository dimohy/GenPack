using System;
using System.IO;
using System.Text;

namespace GenPack.IO;

/// <summary>
/// Binary writer that respects endianness and string encoding settings
/// </summary>
public class EndianAwareBinaryWriter : IDisposable
{
    private readonly BinaryWriter _writer;
    private readonly UnitEndian _endian;
    private readonly Encoding _encoding;
    private readonly bool _needsByteSwap;
    private bool _disposed;

    public EndianAwareBinaryWriter(Stream output, UnitEndian endian, StringEncoding stringEncoding)
    {
        _endian = endian;
        _encoding = GetEncoding(stringEncoding);
        _writer = new BinaryWriter(output, _encoding);
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

    public void Write(byte value) => _writer.Write(value);
    public void Write(sbyte value) => _writer.Write(value);

    public void Write(short value)
    {
        if (_needsByteSwap)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _writer.Write(bytes);
        }
        else
        {
            _writer.Write(value);
        }
    }

    public void Write(ushort value)
    {
        if (_needsByteSwap)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _writer.Write(bytes);
        }
        else
        {
            _writer.Write(value);
        }
    }

    public void Write(int value)
    {
        if (_needsByteSwap)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _writer.Write(bytes);
        }
        else
        {
            _writer.Write(value);
        }
    }

    public void Write(uint value)
    {
        if (_needsByteSwap)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _writer.Write(bytes);
        }
        else
        {
            _writer.Write(value);
        }
    }

    public void Write(long value)
    {
        if (_needsByteSwap)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _writer.Write(bytes);
        }
        else
        {
            _writer.Write(value);
        }
    }

    public void Write(ulong value)
    {
        if (_needsByteSwap)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _writer.Write(bytes);
        }
        else
        {
            _writer.Write(value);
        }
    }

    public void Write(float value)
    {
        if (_needsByteSwap)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _writer.Write(bytes);
        }
        else
        {
            _writer.Write(value);
        }
    }

    public void Write(double value)
    {
        if (_needsByteSwap)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _writer.Write(bytes);
        }
        else
        {
            _writer.Write(value);
        }
    }

    public void Write(string value)
    {
        var bytes = _encoding.GetBytes(value);
        Write7BitEncodedInt(bytes.Length);
        _writer.Write(bytes);
    }

    public void Write(byte[] buffer) => _writer.Write(buffer);

    public void Write7BitEncodedInt(int value)
    {
        // Write out an int 7 bits at a time. The high bit of the byte,
        // when on, tells reader to continue reading more bytes.
        uint v = (uint)value;   // support negative numbers
        while (v >= 0x80)
        {
            Write((byte)(v | 0x80));
            v >>= 7;
        }
        Write((byte)v);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _writer?.Dispose();
            _disposed = true;
        }
    }
}
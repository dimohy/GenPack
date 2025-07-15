using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GenPack.Checksum;

namespace GenPack.IO;

/// <summary>
/// Binary writer that respects endianness and string encoding settings with checksum support
/// </summary>
public class EndianAwareBinaryWriter : IDisposable
{
    private readonly BinaryWriter _writer;
    private readonly UnitEndian _endian;
    private readonly Encoding _encoding;
    private readonly bool _needsByteSwap;
    private bool _disposed;
    
    // Checksum support
    private readonly Stack<(MemoryStream buffer, IChecksumCalculator calculator)> _checksumStack = new();
    private MemoryStream? _currentChecksumBuffer;
    private IChecksumCalculator? _currentChecksumCalculator;

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

    #region Checksum Methods

    /// <summary>
    /// Begins a checksum calculation region. All subsequent writes will be included in checksum calculation.
    /// </summary>
    public void BeginChecksumRegion()
    {
        // Save current checksum state if nested
        if (_currentChecksumBuffer != null && _currentChecksumCalculator != null)
        {
            _checksumStack.Push((_currentChecksumBuffer, _currentChecksumCalculator));
        }
        
        _currentChecksumBuffer = new MemoryStream();
        _currentChecksumCalculator = null; // Will be set when WriteChecksum is called
    }

    /// <summary>
    /// Ends the current checksum calculation region.
    /// </summary>
    public void EndChecksumRegion()
    {
        // Don't dispose the buffer yet - it will be used for checksum calculation
        // The buffer will be cleaned up when WriteChecksum is called or when a new region begins
        
        // For nested regions, restore previous state
        if (_checksumStack.Count > 0)
        {
            // Store current buffer temporarily
            var currentBuffer = _currentChecksumBuffer;
            var currentCalculator = _currentChecksumCalculator;
            
            // Restore previous state
            var (buffer, calculator) = _checksumStack.Pop();
            _currentChecksumBuffer = buffer;
            _currentChecksumCalculator = calculator;
            
            // The current buffer will be used for checksum calculation before being disposed
        }
        // For the top-level region, keep the buffer for checksum calculation
    }

    /// <summary>
    /// Calculates and writes the checksum for the current region.
    /// </summary>
    /// <param name="checksumType">The type of checksum to calculate</param>
    /// <returns>The calculated checksum value</returns>
    public object WriteChecksum(ChecksumType checksumType)
    {
        if (_currentChecksumBuffer == null)
            throw new InvalidOperationException("No active checksum region. Call BeginChecksumRegion() first.");

        var calculator = ChecksumCalculatorFactory.Create(checksumType);
        calculator.Reset();
        
        var data = _currentChecksumBuffer.ToArray();
        calculator.Update(data);
        
        var checksumBytes = calculator.GetChecksum();
        WriteRaw(checksumBytes);
        
        // Clean up the current checksum buffer after use
        _currentChecksumBuffer.Dispose();
        _currentChecksumBuffer = null;
        _currentChecksumCalculator = null;
        
        return ConvertChecksumToTypedValue(checksumBytes, checksumType);
    }

    private object ConvertChecksumToTypedValue(byte[] checksumBytes, ChecksumType checksumType)
    {
        return checksumType switch
        {
            ChecksumType.Sum8 => checksumBytes[0],
            ChecksumType.XorSum => checksumBytes[0],
            ChecksumType.Lrc8 => checksumBytes[0],
            ChecksumType.Sum16 => BitConverter.ToUInt16(checksumBytes, 0),
            ChecksumType.Fletcher16 => BitConverter.ToUInt16(checksumBytes, 0),
            ChecksumType.Crc16 => BitConverter.ToUInt16(checksumBytes, 0),
            ChecksumType.Crc16Ccitt => BitConverter.ToUInt16(checksumBytes, 0),
            ChecksumType.Crc32 => BitConverter.ToUInt32(checksumBytes, 0),
            ChecksumType.Crc32C => BitConverter.ToUInt32(checksumBytes, 0),
            _ => checksumBytes
        };
    }

    /// <summary>
    /// Writes raw bytes without endian conversion or checksum inclusion
    /// </summary>
    private void WriteRaw(byte[] data)
    {
        _writer.Write(data);
    }

    /// <summary>
    /// Writes data to both the main stream and checksum buffer if active
    /// </summary>
    private void WriteWithChecksum(byte[] data)
    {
        _writer.Write(data);
        _currentChecksumBuffer?.Write(data, 0, data.Length);
    }

    #endregion

    public void Write(byte value) 
    {
        var data = new[] { value };
        WriteWithChecksum(data);
    }
    
    public void Write(sbyte value) 
    {
        var data = new[] { unchecked((byte)value) };
        WriteWithChecksum(data);
    }

    public void Write(short value)
    {
        byte[] bytes;
        if (_needsByteSwap)
        {
            bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
        }
        else
        {
            bytes = BitConverter.GetBytes(value);
        }
        WriteWithChecksum(bytes);
    }

    public void Write(ushort value)
    {
        byte[] bytes;
        if (_needsByteSwap)
        {
            bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
        }
        else
        {
            bytes = BitConverter.GetBytes(value);
        }
        WriteWithChecksum(bytes);
    }

    public void Write(int value)
    {
        byte[] bytes;
        if (_needsByteSwap)
        {
            bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
        }
        else
        {
            bytes = BitConverter.GetBytes(value);
        }
        WriteWithChecksum(bytes);
    }

    public void Write(uint value)
    {
        byte[] bytes;
        if (_needsByteSwap)
        {
            bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
        }
        else
        {
            bytes = BitConverter.GetBytes(value);
        }
        WriteWithChecksum(bytes);
    }

    public void Write(long value)
    {
        byte[] bytes;
        if (_needsByteSwap)
        {
            bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
        }
        else
        {
            bytes = BitConverter.GetBytes(value);
        }
        WriteWithChecksum(bytes);
    }

    public void Write(ulong value)
    {
        byte[] bytes;
        if (_needsByteSwap)
        {
            bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
        }
        else
        {
            bytes = BitConverter.GetBytes(value);
        }
        WriteWithChecksum(bytes);
    }

    public void Write(float value)
    {
        byte[] bytes;
        if (_needsByteSwap)
        {
            bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
        }
        else
        {
            bytes = BitConverter.GetBytes(value);
        }
        WriteWithChecksum(bytes);
    }

    public void Write(double value)
    {
        byte[] bytes;
        if (_needsByteSwap)
        {
            bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
        }
        else
        {
            bytes = BitConverter.GetBytes(value);
        }
        WriteWithChecksum(bytes);
    }

    public void Write(string value)
    {
        var bytes = _encoding.GetBytes(value);
        Write7BitEncodedInt(bytes.Length);
        WriteWithChecksum(bytes);
    }

    public void Write(byte[] buffer) => WriteWithChecksum(buffer);

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
            // Clean up checksum resources
            while (_checksumStack.Count > 0)
            {
                var (buffer, _) = _checksumStack.Pop();
                buffer?.Dispose();
            }
            _currentChecksumBuffer?.Dispose();
            
            _writer?.Dispose();
            _disposed = true;
        }
    }
}
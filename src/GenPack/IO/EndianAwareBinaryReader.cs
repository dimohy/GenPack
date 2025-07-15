using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GenPack.Checksum;

namespace GenPack.IO;

/// <summary>
/// Binary reader that respects endianness and string encoding settings with checksum validation
/// </summary>
public class EndianAwareBinaryReader : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly UnitEndian _endian;
    private readonly Encoding _encoding;
    private readonly bool _needsByteSwap;
    private bool _disposed;
    
    // Checksum support
    private readonly Stack<(MemoryStream buffer, IChecksumCalculator calculator)> _checksumStack = new();
    private MemoryStream? _currentChecksumBuffer;
    private IChecksumCalculator? _currentChecksumCalculator;

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

    #region Checksum Methods

    /// <summary>
    /// Begins a checksum validation region. All subsequent reads will be included in checksum calculation.
    /// </summary>
    public void BeginChecksumRegion()
    {
        // Save current checksum state if nested
        if (_currentChecksumBuffer != null && _currentChecksumCalculator != null)
        {
            _checksumStack.Push((_currentChecksumBuffer, _currentChecksumCalculator));
        }
        
        _currentChecksumBuffer = new MemoryStream();
        _currentChecksumCalculator = null; // Will be set when checksum is read
    }

    /// <summary>
    /// Ends the current checksum validation region.
    /// </summary>
    public void EndChecksumRegion()
    {
        // Don't dispose the buffer yet - it will be used for checksum validation
        // The buffer will be cleaned up when ReadAndValidateChecksum is called or when a new region begins
        
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
            
            // The current buffer will be used for checksum validation before being disposed
        }
        // For the top-level region, keep the buffer for checksum validation
    }

    /// <summary>
    /// Reads and validates the checksum for the current region.
    /// </summary>
    /// <param name="checksumType">The type of checksum to validate</param>
    /// <returns>The checksum value and validation result</returns>
    public object ReadAndValidateChecksum(ChecksumType checksumType)
    {
        if (_currentChecksumBuffer == null)
            throw new InvalidOperationException("No active checksum region. Call BeginChecksumRegion() first.");

        var calculator = ChecksumCalculatorFactory.Create(checksumType);
        calculator.Reset();
        
        var data = _currentChecksumBuffer.ToArray();
        calculator.Update(data);
        
        var checksumSize = ChecksumCalculatorFactory.GetChecksumSize(checksumType);
        var expectedChecksum = ReadRaw(checksumSize);
        
        if (!calculator.Validate(expectedChecksum))
        {
            throw new InvalidDataException($"Checksum validation failed for {checksumType}");
        }
        
        // Clean up the current checksum buffer after use
        _currentChecksumBuffer.Dispose();
        _currentChecksumBuffer = null;
        _currentChecksumCalculator = null;
        
        return ConvertChecksumToTypedValue(expectedChecksum, checksumType);
    }

    /// <summary>
    /// Validates the checksum without returning the value (for legacy support).
    /// </summary>
    /// <param name="checksumType">The type of checksum to validate</param>
    public void ValidateChecksum(ChecksumType checksumType)
    {
        ReadAndValidateChecksum(checksumType);
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
    /// Reads raw bytes without endian conversion or checksum inclusion
    /// </summary>
    private byte[] ReadRaw(int count)
    {
        return _reader.ReadBytes(count);
    }

    /// <summary>
    /// Reads data and includes it in checksum calculation if active
    /// </summary>
    private byte[] ReadWithChecksum(int count)
    {
        var data = _reader.ReadBytes(count);
        _currentChecksumBuffer?.Write(data, 0, data.Length);
        return data;
    }

    #endregion

    #region Size Reading Methods

    /// <summary>
    /// Reads size information using the specified size mode
    /// </summary>
    /// <param name="sizeMode">The encoding mode for the size</param>
    /// <returns>The size value as an integer</returns>
    public int ReadSize(SizeMode sizeMode)
    {
        return sizeMode switch
        {
            SizeMode.Variable7Bit => Read7BitEncodedInt(),
            SizeMode.Fixed8Bit => ReadByte(),
            SizeMode.Fixed16Bit => ReadUInt16(),
            SizeMode.Fixed32Bit => ReadInt32(),
            _ => throw new ArgumentOutOfRangeException(nameof(sizeMode), sizeMode, "Invalid size mode")
        };
    }

    #endregion

    public byte ReadByte() 
    {
        var data = ReadWithChecksum(1);
        return data[0];
    }
    
    public sbyte ReadSByte() 
    {
        var data = ReadWithChecksum(1);
        return unchecked((sbyte)data[0]);
    }

    public short ReadInt16()
    {
        var bytes = ReadWithChecksum(2);
        if (_needsByteSwap)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToInt16(bytes, 0);
    }

    public ushort ReadUInt16()
    {
        var bytes = ReadWithChecksum(2);
        if (_needsByteSwap)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToUInt16(bytes, 0);
    }

    public int ReadInt32()
    {
        var bytes = ReadWithChecksum(4);
        if (_needsByteSwap)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToInt32(bytes, 0);
    }

    public uint ReadUInt32()
    {
        var bytes = ReadWithChecksum(4);
        if (_needsByteSwap)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToUInt32(bytes, 0);
    }

    public long ReadInt64()
    {
        var bytes = ReadWithChecksum(8);
        if (_needsByteSwap)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToInt64(bytes, 0);
    }

    public ulong ReadUInt64()
    {
        var bytes = ReadWithChecksum(8);
        if (_needsByteSwap)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToUInt64(bytes, 0);
    }

    public float ReadSingle()
    {
        var bytes = ReadWithChecksum(4);
        if (_needsByteSwap)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToSingle(bytes, 0);
    }

    public double ReadDouble()
    {
        var bytes = ReadWithChecksum(8);
        if (_needsByteSwap)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToDouble(bytes, 0);
    }

    public string ReadString()
    {
        var length = Read7BitEncodedInt();
        var bytes = ReadWithChecksum(length);
        return _encoding.GetString(bytes);
    }

    public byte[] ReadBytes(int count) => ReadWithChecksum(count);

    public int Read7BitEncodedInt()
    {
        // Read out an int 7 bits at a time. The high bit of the byte,
        // when on, tells reader to continue reading more bytes.
        int count = 0;
        int shift = 0;
        byte b;
        do
        {
            // Check for a corrupted stream. Read a max of 5 bytes.
            // In a future version, this check could be removed for performance.
            if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
            {
                throw new FormatException("Bad 7-bit encoded integer format");
            }

            // ReadByte handles end of stream cases for us.
            b = ReadByte();
            count |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
        return count;
    }

    /// <summary>
    /// Reads the checksum value without validation (for property assignment).
    /// </summary>
    /// <returns>The checksum bytes as read from stream</returns>
    public byte[] ReadChecksum()
    {
        // For now, just read a single byte as default checksum size
        // This should be improved to read based on the active checksum type
        return ReadRaw(1);
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
            
            _reader?.Dispose();
            _disposed = true;
        }
    }
}
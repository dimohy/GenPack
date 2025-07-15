using System;

namespace GenPack.Checksum.Calculators;

/// <summary>
/// CRC-16 checksum calculator using polynomial 0x8005 (CRC-16-IBM).
/// Provides strong error detection for network protocols.
/// </summary>
public sealed class Crc16Calculator : IChecksumCalculator
{
    private const ushort Polynomial = 0x8005;
    private static readonly ushort[] _table;
    private ushort _crc;

    static Crc16Calculator()
    {
        _table = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            ushort crc = (ushort)i;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (ushort)((crc >> 1) ^ Polynomial);
                else
                    crc >>= 1;
            }
            _table[i] = crc;
        }
    }

    /// <inheritdoc />
    public ChecksumType Type => ChecksumType.Crc16;

    /// <inheritdoc />
    public int ChecksumSize => 2;

    /// <inheritdoc />
    public void Reset()
    {
        _crc = 0;
    }

    /// <inheritdoc />
    public void Update(byte data)
    {
        _crc = (ushort)((_crc >> 8) ^ _table[(_crc ^ data) & 0xFF]);
    }

    /// <inheritdoc />
    public void Update(byte[] data)
    {
        if (data == null) return;
        Update(data, 0, data.Length);
    }

    /// <inheritdoc />
    public void Update(byte[] data, int offset, int length)
    {
        if (data == null) return;
        
        for (int i = 0; i < length; i++)
        {
            _crc = (ushort)((_crc >> 8) ^ _table[(_crc ^ data[offset + i]) & 0xFF]);
        }
    }

    /// <inheritdoc />
    public byte[] GetChecksum()
    {
        return BitConverter.GetBytes(_crc);
    }

    /// <inheritdoc />
    public bool Validate(byte[] expectedChecksum)
    {
        if (expectedChecksum == null || expectedChecksum.Length != 2)
            return false;
        
        var expected = BitConverter.ToUInt16(expectedChecksum, 0);
        return _crc == expected;
    }
}
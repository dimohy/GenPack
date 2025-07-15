using System;

namespace GenPack.Checksum.Calculators;

/// <summary>
/// CRC-32C checksum calculator using Castagnoli polynomial 0x1EDC6F41.
/// Optimized for modern CPUs and used in protocols like iSCSI and SCTP.
/// </summary>
public sealed class Crc32CCalculator : IChecksumCalculator
{
    private const uint Polynomial = 0x82F63B78; // Reversed Castagnoli polynomial
    private static readonly uint[] _table;
    private uint _crc;

    static Crc32CCalculator()
    {
        _table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ Polynomial;
                else
                    crc >>= 1;
            }
            _table[i] = crc;
        }
    }

    /// <inheritdoc />
    public ChecksumType Type => ChecksumType.Crc32C;

    /// <inheritdoc />
    public int ChecksumSize => 4;

    /// <inheritdoc />
    public void Reset()
    {
        _crc = 0xFFFFFFFF;
    }

    /// <inheritdoc />
    public void Update(byte data)
    {
        _crc = (_crc >> 8) ^ _table[(_crc ^ data) & 0xFF];
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
            _crc = (_crc >> 8) ^ _table[(_crc ^ data[offset + i]) & 0xFF];
        }
    }

    /// <inheritdoc />
    public byte[] GetChecksum()
    {
        var finalCrc = _crc ^ 0xFFFFFFFF;
        return BitConverter.GetBytes(finalCrc);
    }

    /// <inheritdoc />
    public bool Validate(byte[] expectedChecksum)
    {
        if (expectedChecksum == null || expectedChecksum.Length != 4)
            return false;
        
        var expected = BitConverter.ToUInt32(expectedChecksum, 0);
        var calculated = _crc ^ 0xFFFFFFFF;
        return calculated == expected;
    }
}
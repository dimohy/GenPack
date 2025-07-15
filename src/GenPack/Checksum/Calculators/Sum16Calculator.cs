using System;

namespace GenPack.Checksum.Calculators;

/// <summary>
/// Simple 16-bit sum checksum calculator.
/// Calculates the sum of all bytes modulo 65536.
/// </summary>
public sealed class Sum16Calculator : IChecksumCalculator
{
    private ushort _sum;

    /// <inheritdoc />
    public ChecksumType Type => ChecksumType.Sum16;

    /// <inheritdoc />
    public int ChecksumSize => 2;

    /// <inheritdoc />
    public void Reset()
    {
        _sum = 0;
    }

    /// <inheritdoc />
    public void Update(byte data)
    {
        _sum = unchecked((ushort)(_sum + data));
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
            _sum = unchecked((ushort)(_sum + data[offset + i]));
        }
    }

    /// <inheritdoc />
    public byte[] GetChecksum()
    {
        return BitConverter.GetBytes(_sum);
    }

    /// <inheritdoc />
    public bool Validate(byte[] expectedChecksum)
    {
        if (expectedChecksum == null || expectedChecksum.Length != 2)
            return false;
        
        var expected = BitConverter.ToUInt16(expectedChecksum, 0);
        return _sum == expected;
    }
}
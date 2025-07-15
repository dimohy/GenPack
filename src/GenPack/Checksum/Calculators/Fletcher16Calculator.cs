using System;

namespace GenPack.Checksum.Calculators;

/// <summary>
/// Fletcher 16-bit checksum calculator.
/// Provides better error detection than simple sums by using two running sums.
/// </summary>
public sealed class Fletcher16Calculator : IChecksumCalculator
{
    private ushort _sum1;
    private ushort _sum2;

    /// <inheritdoc />
    public ChecksumType Type => ChecksumType.Fletcher16;

    /// <inheritdoc />
    public int ChecksumSize => 2;

    /// <inheritdoc />
    public void Reset()
    {
        _sum1 = 0;
        _sum2 = 0;
    }

    /// <inheritdoc />
    public void Update(byte data)
    {
        _sum1 = (ushort)((_sum1 + data) % 255);
        _sum2 = (ushort)((_sum2 + _sum1) % 255);
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
            _sum1 = (ushort)((_sum1 + data[offset + i]) % 255);
            _sum2 = (ushort)((_sum2 + _sum1) % 255);
        }
    }

    /// <inheritdoc />
    public byte[] GetChecksum()
    {
        var result = (ushort)((_sum2 << 8) | _sum1);
        return BitConverter.GetBytes(result);
    }

    /// <inheritdoc />
    public bool Validate(byte[] expectedChecksum)
    {
        if (expectedChecksum == null || expectedChecksum.Length != 2)
            return false;
        
        var expected = BitConverter.ToUInt16(expectedChecksum, 0);
        var calculated = (ushort)((_sum2 << 8) | _sum1);
        return calculated == expected;
    }
}
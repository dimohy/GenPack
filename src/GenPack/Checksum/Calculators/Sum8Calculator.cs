namespace GenPack.Checksum.Calculators;

/// <summary>
/// Simple 8-bit sum checksum calculator.
/// Calculates the sum of all bytes modulo 256.
/// </summary>
public sealed class Sum8Calculator : IChecksumCalculator
{
    private byte _sum;

    /// <inheritdoc />
    public ChecksumType Type => ChecksumType.Sum8;

    /// <inheritdoc />
    public int ChecksumSize => 1;

    /// <inheritdoc />
    public void Reset()
    {
        _sum = 0;
    }

    /// <inheritdoc />
    public void Update(byte data)
    {
        _sum = unchecked((byte)(_sum + data));
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
            _sum = unchecked((byte)(_sum + data[offset + i]));
        }
    }

    /// <inheritdoc />
    public byte[] GetChecksum()
    {
        return new[] { _sum };
    }

    /// <inheritdoc />
    public bool Validate(byte[] expectedChecksum)
    {
        if (expectedChecksum == null || expectedChecksum.Length != 1)
            return false;
        
        return _sum == expectedChecksum[0];
    }
}
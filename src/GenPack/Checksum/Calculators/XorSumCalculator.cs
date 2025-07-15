namespace GenPack.Checksum.Calculators;

/// <summary>
/// XOR-based checksum calculator.
/// Calculates the XOR of all bytes in the data.
/// </summary>
public sealed class XorSumCalculator : IChecksumCalculator
{
    private byte _xor;

    /// <inheritdoc />
    public ChecksumType Type => ChecksumType.XorSum;

    /// <inheritdoc />
    public int ChecksumSize => 1;

    /// <inheritdoc />
    public void Reset()
    {
        _xor = 0;
    }

    /// <inheritdoc />
    public void Update(byte data)
    {
        _xor ^= data;
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
            _xor ^= data[offset + i];
        }
    }

    /// <inheritdoc />
    public byte[] GetChecksum()
    {
        return new[] { _xor };
    }

    /// <inheritdoc />
    public bool Validate(byte[] expectedChecksum)
    {
        if (expectedChecksum == null || expectedChecksum.Length != 1)
            return false;
        
        return _xor == expectedChecksum[0];
    }
}
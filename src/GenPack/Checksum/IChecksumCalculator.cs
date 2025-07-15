namespace GenPack.Checksum;

/// <summary>
/// Interface for checksum calculation algorithms.
/// Designed for network packet validation without requiring stream seeking.
/// </summary>
public interface IChecksumCalculator
{
    /// <summary>
    /// Gets the checksum type implemented by this calculator
    /// </summary>
    ChecksumType Type { get; }

    /// <summary>
    /// Gets the size of the checksum result in bytes
    /// </summary>
    int ChecksumSize { get; }

    /// <summary>
    /// Resets the checksum calculation to initial state
    /// </summary>
    void Reset();

    /// <summary>
    /// Updates the checksum with a single byte
    /// </summary>
    /// <param name="data">The byte to include in checksum calculation</param>
    void Update(byte data);

    /// <summary>
    /// Updates the checksum with a byte array
    /// </summary>
    /// <param name="data">The byte array to include in checksum calculation</param>
    void Update(byte[] data);

    /// <summary>
    /// Updates the checksum with a portion of a byte array
    /// </summary>
    /// <param name="data">The byte array containing data</param>
    /// <param name="offset">The starting offset in the array</param>
    /// <param name="length">The number of bytes to process</param>
    void Update(byte[] data, int offset, int length);

    /// <summary>
    /// Gets the final checksum value as a byte array
    /// </summary>
    /// <returns>The computed checksum</returns>
    byte[] GetChecksum();

    /// <summary>
    /// Validates if the provided checksum matches the calculated value
    /// </summary>
    /// <param name="expectedChecksum">The expected checksum value</param>
    /// <returns>True if checksums match, false otherwise</returns>
    bool Validate(byte[] expectedChecksum);
}
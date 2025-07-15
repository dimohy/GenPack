using System;
using GenPack.Checksum.Calculators;

namespace GenPack.Checksum;

/// <summary>
/// Factory for creating checksum calculator instances.
/// Provides centralized creation of checksum calculators for all supported algorithms.
/// </summary>
public static class ChecksumCalculatorFactory
{
    /// <summary>
    /// Creates a checksum calculator instance for the specified checksum type
    /// </summary>
    /// <param name="checksumType">The type of checksum algorithm to create</param>
    /// <returns>A new instance of the appropriate checksum calculator</returns>
    /// <exception cref="ArgumentException">Thrown when the checksum type is not supported</exception>
    public static IChecksumCalculator Create(ChecksumType checksumType)
    {
        return checksumType switch
        {
            ChecksumType.Sum8 => new Sum8Calculator(),
            ChecksumType.Sum16 => new Sum16Calculator(),
            ChecksumType.XorSum => new XorSumCalculator(),
            ChecksumType.Lrc8 => new XorSumCalculator(), // LRC8 is identical to XorSum
            ChecksumType.Fletcher16 => new Fletcher16Calculator(),
            ChecksumType.Crc16 => new Crc16Calculator(),
            ChecksumType.Crc16Ccitt => new Crc16CcittCalculator(),
            ChecksumType.Crc32 => new Crc32Calculator(),
            ChecksumType.Crc32C => new Crc32CCalculator(),
            _ => throw new ArgumentException($"Unsupported checksum type: {checksumType}", nameof(checksumType))
        };
    }

    /// <summary>
    /// Gets the size in bytes of the checksum for the specified type
    /// </summary>
    /// <param name="checksumType">The checksum type</param>
    /// <returns>The size of the checksum in bytes</returns>
    public static int GetChecksumSize(ChecksumType checksumType)
    {
        return checksumType switch
        {
            ChecksumType.Sum8 => 1,
            ChecksumType.XorSum => 1,
            ChecksumType.Lrc8 => 1,
            ChecksumType.Sum16 => 2,
            ChecksumType.Fletcher16 => 2,
            ChecksumType.Crc16 => 2,
            ChecksumType.Crc16Ccitt => 2,
            ChecksumType.Crc32 => 4,
            ChecksumType.Crc32C => 4,
            _ => throw new ArgumentException($"Unsupported checksum type: {checksumType}", nameof(checksumType))
        };
    }
}
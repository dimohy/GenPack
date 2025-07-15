namespace GenPack;

/// <summary>
/// Defines the types of checksum algorithms supported by GenPack.
/// These algorithms are optimized for network packet validation without requiring stream seeking.
/// </summary>
public enum ChecksumType
{
    /// <summary>
    /// Simple 8-bit sum checksum (sum of all bytes modulo 256)
    /// </summary>
    Sum8,

    /// <summary>
    /// Simple 16-bit sum checksum (sum of all bytes modulo 65536)
    /// </summary>
    Sum16,

    /// <summary>
    /// XOR-based checksum (XOR of all bytes)
    /// </summary>
    XorSum,

    /// <summary>
    /// Longitudinal Redundancy Check (LRC) - XOR of all bytes
    /// </summary>
    Lrc8,

    /// <summary>
    /// Fletcher 16-bit checksum algorithm
    /// </summary>
    Fletcher16,

    /// <summary>
    /// CRC-16 using polynomial 0x8005 (CRC-16-IBM)
    /// </summary>
    Crc16,

    /// <summary>
    /// CRC-16 using polynomial 0x1021 (CRC-16-CCITT)
    /// </summary>
    Crc16Ccitt,

    /// <summary>
    /// CRC-32 using polynomial 0x04C11DB7 (CRC-32-IEEE 802.3)
    /// </summary>
    Crc32,

    /// <summary>
    /// CRC-32C using Castagnoli polynomial 0x1EDC6F41 (optimized for modern CPUs)
    /// </summary>
    Crc32C
}
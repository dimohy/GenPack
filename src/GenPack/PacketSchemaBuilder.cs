#pragma warning disable IDE1006 // 명명 스타일

namespace GenPack;

public class PacketSchemaBuilder
{
    private UnitEndian _defaultEndian;
    private StringEncoding _defaultStringEncoding;

    private PacketSchemaBuilder()
    {
    }

    public static PacketSchemaBuilder Create(UnitEndian defaultEndian = UnitEndian.Little, StringEncoding defaultStringEncoding = StringEncoding.UTF8)
    {
        var result = new PacketSchemaBuilder();
        result._defaultEndian = defaultEndian;
        result._defaultStringEncoding = defaultStringEncoding;

        return result;
    }

    public PacketSchema Build()
    {
        return new PacketSchema
        {
            DefaultEndian = _defaultEndian,
            DefaultStringEncoding = _defaultStringEncoding
        };
    }

    public PacketSchemaBuilder @byte(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @sbyte(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @short(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @ushort(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @int(string name, string description = "")
    {
        return this;
    }
    public PacketSchemaBuilder @uint(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @long(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @ulong(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @float(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @double(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @object<T>(string name, string description = "")
        where T : IGenPackable
    {
        return this;
    }

    /// <summary>
    /// Defines a list property with variable 7-bit encoding for size (default behavior)
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    /// <param name="name">The name of the property</param>
    /// <param name="description">Optional description for the property</param>
    /// <returns>The current PacketSchemaBuilder instance for method chaining</returns>
    public PacketSchemaBuilder @list<T>(string name, string description = "")
    {
        return this;
    }

    /// <summary>
    /// Defines a list property with the specified size encoding mode
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    /// <param name="name">The name of the property</param>
    /// <param name="sizeMode">The encoding mode for the list size</param>
    /// <param name="description">Optional description for the property</param>
    /// <returns>The current PacketSchemaBuilder instance for method chaining</returns>
    public PacketSchemaBuilder @list<T>(string name, SizeMode sizeMode, string description = "")
    {
        return this;
    }

    /// <summary>
    /// Defines a dictionary property with variable 7-bit encoding for size (default behavior)
    /// </summary>
    /// <typeparam name="T">The type of values in the dictionary</typeparam>
    /// <param name="name">The name of the property</param>
    /// <param name="description">Optional description for the property</param>
    /// <returns>The current PacketSchemaBuilder instance for method chaining</returns>
    public PacketSchemaBuilder @dict<T>(string name, string description = "")
    {
        return this;
    }

    /// <summary>
    /// Defines a dictionary property with the specified size encoding mode
    /// </summary>
    /// <typeparam name="T">The type of values in the dictionary</typeparam>
    /// <param name="name">The name of the property</param>
    /// <param name="sizeMode">The encoding mode for the dictionary size</param>
    /// <param name="description">Optional description for the property</param>
    /// <returns>The current PacketSchemaBuilder instance for method chaining</returns>
    public PacketSchemaBuilder @dict<T>(string name, SizeMode sizeMode, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @array<T>(string name, int length, string description = "")
    {
        return this;
    }

    /// <summary>
    /// Marks the beginning of a checksum calculation region.
    /// All data written between BeginChecksumRegion and EndChecksumRegion will be included in checksum calculation.
    /// </summary>
    /// <returns>The current PacketSchemaBuilder instance for method chaining</returns>
    public PacketSchemaBuilder BeginChecksumRegion()
    {
        return this;
    }

    /// <summary>
    /// Marks the end of a checksum calculation region.
    /// Must be paired with a corresponding BeginChecksumRegion call.
    /// </summary>
    /// <returns>The current PacketSchemaBuilder instance for method chaining</returns>
    public PacketSchemaBuilder EndChecksumRegion()
    {
        return this;
    }

    /// <summary>
    /// Defines a checksum field that will contain the calculated checksum value.
    /// The checksum is computed over the data between the most recent BeginChecksumRegion and EndChecksumRegion calls.
    /// </summary>
    /// <param name="name">The name of the checksum property</param>
    /// <param name="checksumType">The type of checksum algorithm to use</param>
    /// <param name="description">Optional description for the checksum field</param>
    /// <returns>The current PacketSchemaBuilder instance for method chaining</returns>
    public PacketSchemaBuilder @checksum(string name, ChecksumType checksumType, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @string(string name, string description = "", int length = 0)
    {
        return this;
    }

    #region Legacy Methods (for backward compatibility)
    
    /// <summary>
    /// Legacy method. Use BeginChecksumRegion() instead.
    /// </summary>
    [System.Obsolete("Use BeginChecksumRegion() instead. This method will be removed in a future version.")]
    public PacketSchemaBuilder BeginPointChecksum()
    {
        return BeginChecksumRegion();
    }

    /// <summary>
    /// Legacy method. Use EndChecksumRegion() instead.
    /// </summary>
    [System.Obsolete("Use EndChecksumRegion() instead. This method will be removed in a future version.")]
    public PacketSchemaBuilder EndPointChecksum()
    {
        return EndChecksumRegion();
    }

    /// <summary>
    /// Legacy method. Use @checksum(string, ChecksumType, string) instead.
    /// </summary>
    [System.Obsolete("Use @checksum(string, ChecksumType, string) instead. This method will be removed in a future version.")]
    public PacketSchemaBuilder @checkum(ChecksumType checksumType)
    {
        return this;
    }

    #endregion
}

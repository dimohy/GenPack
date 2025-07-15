namespace GenPack;

/// <summary>
/// Specifies how the size/length information is encoded in list and dictionary collections.
/// </summary>
public enum SizeMode
{
    /// <summary>
    /// Variable 7-bit encoding (default) - most space efficient for smaller sizes
    /// </summary>
    Variable7Bit = 0,
    
    /// <summary>
    /// Fixed 8-bit encoding - maximum 255 items
    /// </summary>
    Fixed8Bit = 1,
    
    /// <summary>
    /// Fixed 16-bit encoding - maximum 65,535 items
    /// </summary>
    Fixed16Bit = 2,
    
    /// <summary>
    /// Fixed 32-bit encoding - maximum 2,147,483,647 items
    /// </summary>
    Fixed32Bit = 3
}
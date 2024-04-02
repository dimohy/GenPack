using System.ComponentModel;


namespace GenPack { 
    public class PacketSchema
    {
        public UnitEndian DefaultEndian { get; init; }
        public StringEncoding DefaultStringEncoding { get; init; }

        public static bool IsDefaultType(string schemaType) => schemaType switch
        {
            nameof(PacketSchemaBuilder.@byte) => true,
            nameof(PacketSchemaBuilder.@sbyte) => true,
            nameof(PacketSchemaBuilder.@short) => true,
            nameof(PacketSchemaBuilder.@ushort) => true,
            nameof(PacketSchemaBuilder.@int) => true,
            nameof(PacketSchemaBuilder.@uint) => true,
            nameof(PacketSchemaBuilder.@long) => true,
            nameof(PacketSchemaBuilder.@ulong) => true,
            nameof(PacketSchemaBuilder.@single) => true,
            nameof(PacketSchemaBuilder.@double) => true,
            nameof(PacketSchemaBuilder.@string) => true,
            _ => false
        };

        public static string GetClsType(string schemaType) => schemaType switch
        {
            nameof(PacketSchemaBuilder.@byte) => "Byte",
            nameof(PacketSchemaBuilder.@sbyte) => "SByte",
            nameof(PacketSchemaBuilder.@short) => "Int16",
            nameof(PacketSchemaBuilder.@ushort) => "UInt16",
            nameof(PacketSchemaBuilder.@int) => "Int32",
            nameof(PacketSchemaBuilder.@uint) => "UInt32",
            nameof(PacketSchemaBuilder.@long) => "Int64",
            nameof(PacketSchemaBuilder.@ulong) => "UInt64",
            nameof(PacketSchemaBuilder.@single) => "Single",
            nameof(PacketSchemaBuilder.@double) => "Double",
            nameof(PacketSchemaBuilder.@string) => "String",
            _ => schemaType
        };
    }
}

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}

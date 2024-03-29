#pragma warning disable IDE1006 // 명명 스타일

namespace GenPack;

public class PacketSchemaBuilder
{
    private PacketSchemaBuilder()
    {
    }

    public static PacketSchemaBuilder Create(UnitEndian defaultEndian = UnitEndian.Little, StringEncoding defaultStringEncoding = StringEncoding.UTF8)
    {
        return new PacketSchemaBuilder();
    }

    public PacketSchema Build()
    {
        return new PacketSchema();
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

    public PacketSchemaBuilder @single(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder @double(string name, string description = "")
    {
        return this;
    }

    public PacketSchemaBuilder BeginPointChecksum()
    {
        return this;
    }

    public PacketSchemaBuilder EndPointChecksum()
    {
        return this;
    }

    public PacketSchemaBuilder @checkum(ChecksumType checksumType)
    {
        return this;
    }

    public PacketSchemaBuilder @string(string name, string description = "", int length = 0)
    {
        return this;
    }
}

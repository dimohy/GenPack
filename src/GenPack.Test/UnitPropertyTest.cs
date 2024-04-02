using System.Linq;

namespace GenPack.Test;

public class UnitPropertyTest
{
    [Fact]
    public void TestBytePropertyPacket()
    {
        var p = new TestBytePacket
        {
            Value1 = 10
        };

        var data = p.ToPacket();
        Assert.True(data.SequenceEqual((byte[])[10]));
    }

    [Fact]
    public void TestInt16PropertyPacket()
    {
        var p = new TestInt16Packet
        {
            Value1 = 0x0A0B
        };

        var data = p.ToPacket();
        Assert.True(data.SequenceEqual((byte[])[0x0B, 0x0A]));
    }
}

[GenPackable]
partial class TestBytePacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@byte("Value1", "°ª1")
        .Build();
}

[GenPackable]
partial class TestInt16Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@short("Value1", "°ª1")
        .Build();
}
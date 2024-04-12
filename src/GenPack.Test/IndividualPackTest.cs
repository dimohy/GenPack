using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenPack.Test;

[TestClass]
public class IndividualPackTest
{
    [TestMethod]
    public void TestBytePacket()
    {
        var p = new TestBytePacket
        {
            Value1 = 10
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[10]));
    }

    [TestMethod]
    public void TestSBytePacket()
    {
        var p = new TestSBytePacket
        {
            Value1 = -2 // 0xFE
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0xFE]));
    }

    [TestMethod]
    public void TestInt16Packet()
    {
        var p = new TestInt16Packet
        {
            Value1 = 0x0A0B
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x0B, 0x0A]));
    }

    [TestMethod]
    public void TestUInt16Packet()
    {
        var p = new TestUInt16Packet
        {
            Value1 = 0xFFFE // 0xFFFE
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0xFE, 0xFF]));
    }

    [TestMethod]
    public void TestInt32Packet()
    {
        var p = new TestInt32Packet
        {
            Value1 = 0x0A0B_0C0D
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x0D, 0x0C, 0x0B, 0x0A]));
    }

    [TestMethod]
    public void TestUInt32Packet()
    {
        var p = new TestUInt32Packet
        {
            Value1 = 0xFFFF_FFFE
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0xFE, 0xFF, 0xFF, 0xFF]));
    }

    [TestMethod]
    public void TestInt64Packet()
    {
        var p = new TestInt64Packet
        {
            Value1 = 0x0102_0304_0506_0708
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01]));
    }

    [TestMethod]
    public void TestUInt64Packet()
    {
        var p = new TestUInt64Packet
        {
            Value1 = 0xFFFF_FFFF_FFFF_FFFE
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]));
    }

    [TestMethod]
    public void TestSinglePacket()
    {
        var p = new TestSinglePacket
        {
            Value1 = 3.14f // 0x4048F5C3
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0xC3, 0xF5, 0x48, 0x40]));
    }


    [TestMethod]
    public void TestDoublePacket()
    {
        var p = new TestDoublePacket
        {
            Value1 = 3.141592d // 0x400921FAFC8B0007A
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x7A, 0x00, 0x8B, 0xFC, 0xFA, 0x21, 0x09, 0x40]));
    }

    [TestMethod]
    public void TestStringPacket()
    {
        var p = new TestStringPacket
        {
            Value1 = "test"
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t']));

        var p2 = new TestStringPacket
        {
            Value1 = new string('0', 128)
        };
        data = p2.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x80, 0x01, .. Enumerable.Repeat((byte)'0', 128)]));
    }

    [TestMethod]
    public void TestObjectPacket()
    {
        var p = new TestObjectPacket
        {
            Value1 = new()
            {
                Value1 = "test"
            }
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t']));
    }

    [TestMethod]
    public void TestInt16ListPacket()
    {
        var p = new TestInt16ListPacket
        {
            Value1 = { 1, 2, 3, 4, 5 }
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x05, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05, 0x00]));
    }

    [TestMethod]
    public void TestStringListPacket()
    {
        var p = new TestStringListPacket
        {
            Value1 = { "a", "b", "c", "d", "e" }
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x05, 0x01, (byte)'a', 0x01, (byte)'b', 0x01, (byte)'c', 0x01, (byte)'d', 0x01, (byte)'e']));
    }

    [TestMethod]
    public void TestObjectListPacket()
    {
        var p = new TestObjectListPacket
        {
            Value1 = {
                new() { Value1 = new() { Value1 = "a" } },
                new() { Value1 = new() { Value1 = "b" } },
                new() { Value1 = new() { Value1 = "c" } }
            }
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x03, 0x01, (byte)'a', 0x01, (byte)'b', 0x01, (byte)'c']));
    }

    [TestMethod]
    public void TestByteArrayPacket()
    {
        var p = new TestByteArrayPacket();
        byte[] bytes = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50];
        Array.Copy(bytes, p.Value1, bytes.Length);

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual(bytes));
    }

    [TestMethod]
    public void TestInt16ArrayPacket()
    {
        var p = new TestInt16ArrayPacket();
        short[] shorts = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50];
        Array.Copy(shorts, p.Value1, shorts.Length);

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[1, 0x00, 2, 0x00, 3, 0x00, 4, 0x00, 5, 0x00, 6, 0x00, 7, 0x00, 8, 0x00, 9, 0x00, 10, 0x00, 11, 0x00, 12, 0x00, 13, 0x00, 14, 0x00, 15, 0x00, 16, 0x00, 17, 0x00, 18, 0x00, 19, 0x00, 20, 0x00, 21, 0x00, 22, 0x00, 23, 0x00, 24, 0x00, 25, 0x00, 26, 0x00, 27, 0x00, 28, 0x00, 29, 0x00, 30, 0x00, 31, 0x00, 32, 0x00, 33, 0x00, 34, 0x00, 35, 0x00, 36, 0x00, 37, 0x00, 38, 0x00, 39, 0x00, 40, 0x00, 41, 0x00, 42, 0x00, 43, 0x00, 44, 0x00, 45, 0x00, 46, 0x00, 47, 0x00, 48, 0x00, 49, 0x00, 50, 0x00,]));
    }

    [TestMethod]
    public void TestStringArrayPacket()
    {
        var p = new TestStringArrayPacket();
        string[] strings = ["a", "b", "c", "d", "e"];
        Array.Copy(strings, p.Value1, strings.Length);

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x01, (byte)'a', 0x01, (byte)'b', 0x01, (byte)'c', 0x01, (byte)'d', 0x01, (byte)'e']));
    }

    [TestMethod]
    public void TestInt16DickPacket()
    {
        var p = new TestInt16DickPacket
        {
            Value1 = { ["a"] = 1, ["b"] = 2, ["c"] = 3 }
        };

        var data = p.ToPacket();
        Assert.IsTrue(data.SequenceEqual((byte[])[0x03, 0x01, (byte)'a', 0x01, 0x00, 0x01, (byte)'b', 0x02, 0x00, 0x01, (byte)'c', 0x03, 0x00]));
    }
}

[GenPackable]
partial class TestBytePacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@byte("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestSBytePacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@sbyte("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestInt16Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@short("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestUInt16Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@ushort("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestInt32Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@int("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestUInt32Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@uint("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestInt64Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@long("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestUInt64Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@ulong("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestSinglePacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@float("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestDoublePacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@double("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestStringPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@string("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestObjectPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@object<TestStringPacket>("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestInt16ListPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@list<short>("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestStringListPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@list<string>("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestObjectListPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@list<TestObjectPacket>("Value1", "값1")
        .Build();
}

[GenPackable]
partial class TestByteArrayPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@array<byte>("Value1", 50, "값1")
        .Build();
}

[GenPackable]
partial class TestInt16ArrayPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@array<short>("Value1", 50, "값1")
        .Build();
}

[GenPackable]
partial class  TestStringArrayPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@array<string>("Value1", 5, "값1")
        .Build();
}

[GenPackable]
partial class TestInt16DickPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@dict<short>("Value1", "값1")
        .Build();
}

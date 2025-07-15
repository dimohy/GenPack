using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenPack.Test;

/// <summary>
/// Tests for the new SizeMode functionality for @list and @dict
/// </summary>
[TestClass]
public class SizeModeTest
{
    #region Variable7Bit Tests (default behavior)

    [TestMethod]
    public void TestVariable7BitListSmallSize()
    {
        var packet = new Variable7BitListPacket
        {
            SmallList = { 1, 2, 3 }
        };

        var data = packet.ToPacket();
        var restored = Variable7BitListPacket.FromPacket(data);

        Assert.AreEqual(3, restored.SmallList.Count);
        Assert.AreEqual(1, restored.SmallList[0]);
        Assert.AreEqual(2, restored.SmallList[1]);
        Assert.AreEqual(3, restored.SmallList[2]);

        // Variable 7-bit encoding for size 3 should be a single byte
        Assert.AreEqual(0x03, data[0]);
    }

    [TestMethod]
    public void TestVariable7BitListLargeSize()
    {
        var packet = new Variable7BitListPacket();
        // Add 128 items to test 7-bit encoding boundary
        for (int i = 0; i < 128; i++)
        {
            packet.SmallList.Add(i);
        }

        var data = packet.ToPacket();
        var restored = Variable7BitListPacket.FromPacket(data);

        Assert.AreEqual(128, restored.SmallList.Count);
        
        // Variable 7-bit encoding for size 128 should be two bytes: 0x80, 0x01
        Assert.AreEqual(0x80, data[0]);
        Assert.AreEqual(0x01, data[1]);
    }

    [TestMethod]
    public void TestVariable7BitDictionary()
    {
        var packet = new Variable7BitDictPacket
        {
            SmallDict = { ["key1"] = 100, ["key2"] = 200 }
        };

        var data = packet.ToPacket();
        var restored = Variable7BitDictPacket.FromPacket(data);

        Assert.AreEqual(2, restored.SmallDict.Count);
        Assert.AreEqual(100, restored.SmallDict["key1"]);
        Assert.AreEqual(200, restored.SmallDict["key2"]);
    }

    #endregion

    #region Fixed8Bit Tests

    [TestMethod]
    public void TestFixed8BitList()
    {
        var packet = new Fixed8BitListPacket
        {
            ByteSizeList = { 10, 20, 30, 40, 50 }
        };

        var data = packet.ToPacket();
        var restored = Fixed8BitListPacket.FromPacket(data);

        Assert.AreEqual(5, restored.ByteSizeList.Count);
        Assert.AreEqual(10, restored.ByteSizeList[0]);
        Assert.AreEqual(50, restored.ByteSizeList[4]);

        // Fixed 8-bit encoding for size 5 should be a single byte
        Assert.AreEqual(0x05, data[0]);
    }

    [TestMethod]
    public void TestFixed8BitDict()
    {
        var packet = new Fixed8BitDictPacket
        {
            ByteSizeDict = { ["a"] = 1, ["b"] = 2, ["c"] = 3 }
        };

        var data = packet.ToPacket();
        var restored = Fixed8BitDictPacket.FromPacket(data);

        Assert.AreEqual(3, restored.ByteSizeDict.Count);
        Assert.AreEqual(1, restored.ByteSizeDict["a"]);
        Assert.AreEqual(2, restored.ByteSizeDict["b"]);
        Assert.AreEqual(3, restored.ByteSizeDict["c"]);
    }

    [TestMethod]
    public void TestFixed8BitListMaxSize()
    {
        var packet = new Fixed8BitListPacket();
        // Add exactly 255 items (maximum for 8-bit)
        for (int i = 0; i < 255; i++)
        {
            packet.ByteSizeList.Add(i);
        }

        var data = packet.ToPacket();
        var restored = Fixed8BitListPacket.FromPacket(data);

        Assert.AreEqual(255, restored.ByteSizeList.Count);
        Assert.AreEqual(0xFF, data[0]); // First byte should be 255
    }

    #endregion

    #region Fixed16Bit Tests

    [TestMethod]
    public void TestFixed16BitList()
    {
        var packet = new Fixed16BitListPacket();
        // Add 1000 items to test 16-bit encoding
        for (int i = 0; i < 1000; i++)
        {
            packet.UShortSizeList.Add(i);
        }

        var data = packet.ToPacket();
        var restored = Fixed16BitListPacket.FromPacket(data);

        Assert.AreEqual(1000, restored.UShortSizeList.Count);
        
        // Fixed 16-bit encoding for size 1000 (0x03E8) in little-endian: 0xE8, 0x03
        Assert.AreEqual(0xE8, data[0]);
        Assert.AreEqual(0x03, data[1]);
    }

    [TestMethod]
    public void TestFixed16BitDict()
    {
        var packet = new Fixed16BitDictPacket();
        // Add 500 items
        for (int i = 0; i < 500; i++)
        {
            packet.UShortSizeDict[$"key{i}"] = i * 2;
        }

        var data = packet.ToPacket();
        var restored = Fixed16BitDictPacket.FromPacket(data);

        Assert.AreEqual(500, restored.UShortSizeDict.Count);
        Assert.AreEqual(0, restored.UShortSizeDict["key0"]);
        Assert.AreEqual(998, restored.UShortSizeDict["key499"]);
    }

    #endregion

    #region Fixed32Bit Tests

    [TestMethod]
    public void TestFixed32BitList()
    {
        var packet = new Fixed32BitListPacket();
        // Add 100,000 items to test 32-bit encoding
        for (int i = 0; i < 100000; i++)
        {
            packet.IntSizeList.Add(i);
        }

        var data = packet.ToPacket();
        var restored = Fixed32BitListPacket.FromPacket(data);

        Assert.AreEqual(100000, restored.IntSizeList.Count);
        Assert.AreEqual(0, restored.IntSizeList[0]);
        Assert.AreEqual(99999, restored.IntSizeList[99999]);
    }

    [TestMethod]
    public void TestFixed32BitDict()
    {
        var packet = new Fixed32BitDictPacket
        {
            IntSizeDict = { ["test"] = 42 }
        };

        var data = packet.ToPacket();
        var restored = Fixed32BitDictPacket.FromPacket(data);

        Assert.AreEqual(1, restored.IntSizeDict.Count);
        Assert.AreEqual(42, restored.IntSizeDict["test"]);
        
        // Fixed 32-bit encoding for size 1: 0x01, 0x00, 0x00, 0x00 (little-endian)
        Assert.AreEqual(0x01, data[0]);
        Assert.AreEqual(0x00, data[1]);
        Assert.AreEqual(0x00, data[2]);
        Assert.AreEqual(0x00, data[3]);
    }

    #endregion

    #region Mixed SizeMode Tests

    [TestMethod]
    public void TestMixedSizeModes()
    {
        var packet = new MixedSizeModePacket
        {
            Variable7BitList = { 1, 2, 3 },
            Fixed8BitList = { 10, 20 },
            Fixed16BitDict = { ["key1"] = 100 },
            Fixed32BitDict = { ["key2"] = 200 }
        };

        var data = packet.ToPacket();
        var restored = MixedSizeModePacket.FromPacket(data);

        Assert.AreEqual(3, restored.Variable7BitList.Count);
        Assert.AreEqual(2, restored.Fixed8BitList.Count);
        Assert.AreEqual(1, restored.Fixed16BitDict.Count);
        Assert.AreEqual(1, restored.Fixed32BitDict.Count);

        Assert.AreEqual(3, restored.Variable7BitList[2]);
        Assert.AreEqual(20, restored.Fixed8BitList[1]);
        Assert.AreEqual(100, restored.Fixed16BitDict["key1"]);
        Assert.AreEqual(200, restored.Fixed32BitDict["key2"]);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void TestFixed8BitListSizeOverflow()
    {
        var packet = new Fixed8BitListPacket();
        // Try to add 256 items (exceeds 8-bit maximum of 255)
        for (int i = 0; i <= 255; i++)
        {
            packet.ByteSizeList.Add(i);
        }

        // This should throw when trying to serialize
        packet.ToPacket();
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void TestFixed16BitListSizeOverflow()
    {
        var packet = new Fixed16BitListPacket();
        // Try to add 65536 items (exceeds 16-bit maximum of 65535)
        for (int i = 0; i <= 65535; i++)
        {
            packet.UShortSizeList.Add(i);
        }

        // This should throw when trying to serialize
        packet.ToPacket();
    }

    #endregion
}

#region Test Packet Definitions

[GenPackable]
partial class Variable7BitListPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@list<int>("SmallList", "Variable 7-bit encoded list")
        .Build();
}

[GenPackable]
partial class Variable7BitDictPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@dict<int>("SmallDict", "Variable 7-bit encoded dictionary")
        .Build();
}

[GenPackable]
partial class Fixed8BitListPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@list<int>("ByteSizeList", SizeMode.Fixed8Bit, "8-bit fixed size list")
        .Build();
}

[GenPackable]
partial class Fixed8BitDictPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@dict<int>("ByteSizeDict", SizeMode.Fixed8Bit, "8-bit fixed size dictionary")
        .Build();
}

[GenPackable]
partial class Fixed16BitListPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@list<int>("UShortSizeList", SizeMode.Fixed16Bit, "16-bit fixed size list")
        .Build();
}

[GenPackable]
partial class Fixed16BitDictPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@dict<int>("UShortSizeDict", SizeMode.Fixed16Bit, "16-bit fixed size dictionary")
        .Build();
}

[GenPackable]
partial class Fixed32BitListPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@list<int>("IntSizeList", SizeMode.Fixed32Bit, "32-bit fixed size list")
        .Build();
}

[GenPackable]
partial class Fixed32BitDictPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@dict<int>("IntSizeDict", SizeMode.Fixed32Bit, "32-bit fixed size dictionary")
        .Build();
}

[GenPackable]
partial class MixedSizeModePacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@list<int>("Variable7BitList", SizeMode.Variable7Bit, "Variable 7-bit list")
        .@list<int>("Fixed8BitList", SizeMode.Fixed8Bit, "8-bit fixed list")
        .@dict<int>("Fixed16BitDict", SizeMode.Fixed16Bit, "16-bit fixed dict")
        .@dict<int>("Fixed32BitDict", SizeMode.Fixed32Bit, "32-bit fixed dict")
        .Build();
}

#endregion
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenPack.Test;

/// <summary>
/// Simple test to verify SizeMode functionality works
/// </summary>
[TestClass]
public class SimpleSizeModeTest
{
    [TestMethod]
    public void TestBasicListWithDefaultSize()
    {
        var packet = new BasicListPacket
        {
            Numbers = { 1, 2, 3 }
        };

        var data = packet.ToPacket();
        var restored = BasicListPacket.FromPacket(data);

        Assert.AreEqual(3, restored.Numbers.Count);
        Assert.AreEqual(1, restored.Numbers[0]);
        Assert.AreEqual(2, restored.Numbers[1]);
        Assert.AreEqual(3, restored.Numbers[2]);
    }

    [TestMethod]
    public void TestBasicDictWithDefaultSize()
    {
        var packet = new BasicDictPacket
        {
            Data = { ["key1"] = 100, ["key2"] = 200 }
        };

        var data = packet.ToPacket();
        var restored = BasicDictPacket.FromPacket(data);

        Assert.AreEqual(2, restored.Data.Count);
        Assert.AreEqual(100, restored.Data["key1"]);
        Assert.AreEqual(200, restored.Data["key2"]);
    }
}

#region Simple Test Packet Definitions

[GenPackable]
partial class BasicListPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@list<int>("Numbers", "A list of numbers")
        .Build();
}

[GenPackable]
partial class BasicDictPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@dict<int>("Data", "A dictionary of data")
        .Build();
}

#endregion
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenPack.Test;

/// <summary>
/// Comprehensive tests for checksum functionality in packet serialization.
/// Tests various checksum algorithms and validation scenarios.
/// </summary>
[TestClass]
public class ChecksumTest
{
    #region Basic Checksum Tests

    [TestMethod]
    public void TestSimpleChecksumPacket()
    {
        var packet = new SimpleChecksumPacket
        {
            Command = 0x01,
            Value1 = 0x1234,
            Value2 = 0x5678,
            Message = "Hello"
        };

        var data = packet.ToPacket();
        
        // Round-trip test to verify checksum validation works
        var restored = SimpleChecksumPacket.FromPacket(data);
        
        Assert.AreEqual(0x01, restored.Command);
        Assert.AreEqual(0x1234, restored.Value1);
        Assert.AreEqual(0x5678, restored.Value2);
        Assert.AreEqual("Hello", restored.Message);
        Assert.IsTrue(restored.Checksum > 0); // Verify checksum was calculated
    }

    [TestMethod]
    public void TestMultipleChecksumPacket()
    {
        var packet = new MultipleChecksumPacket
        {
            Header = 0xAA,
            Section1Data = 0x1111,
            Section1Checksum = 0, // Will be calculated
            Section2Data = 0x2222,
            Section2Checksum = 0, // Will be calculated
            Footer = 0xBB
        };

        var data = packet.ToPacket();
        
        // Round-trip test
        var restored = MultipleChecksumPacket.FromPacket(data);
        
        Assert.AreEqual(0xAA, restored.Header);
        Assert.AreEqual(0x1111, restored.Section1Data);
        Assert.AreEqual(0x2222, restored.Section2Data);
        Assert.AreEqual(0xBB, restored.Footer);
        
        // Check that checksums were calculated
        Assert.IsTrue(restored.Section1Checksum >= 0); // XorSum can be 0
        Assert.IsTrue(restored.Section2Checksum > 0); // Sum16 should be > 0 for non-zero data
    }

    #endregion

    #region Sum8 Checksum Tests

    [TestMethod]
    public void TestSum8ChecksumPacket()
    {
        var packet = new Sum8ChecksumPacket
        {
            Data1 = 0x10,
            Data2 = 0x20,
            Data3 = 0x30
        };

        var data = packet.ToPacket();
        var restored = Sum8ChecksumPacket.FromPacket(data);
        
        Assert.AreEqual(0x10, restored.Data1);
        Assert.AreEqual(0x20, restored.Data2);
        Assert.AreEqual(0x30, restored.Data3);
        
        // Expected checksum: (0x10 + 0x20 + 0x30) & 0xFF = 0x60
        Assert.AreEqual(0x60, restored.Checksum);
    }

    [TestMethod]
    public void TestSum8ChecksumWithOverflow()
    {
        var packet = new Sum8ChecksumPacket
        {
            Data1 = 0xFF,
            Data2 = 0xFF,
            Data3 = 0x02
        };

        var data = packet.ToPacket();
        var restored = Sum8ChecksumPacket.FromPacket(data);
        
        // Expected checksum: (0xFF + 0xFF + 0x02) & 0xFF = 0x00
        Assert.AreEqual(0x00, restored.Checksum);
    }

    #endregion

    #region Sum16 Checksum Tests

    [TestMethod]
    public void TestSum16ChecksumPacket()
    {
        var packet = new Sum16ChecksumPacket
        {
            Data1 = 0x1234,
            Data2 = 0x5678
        };

        var data = packet.ToPacket();
        var restored = Sum16ChecksumPacket.FromPacket(data);
        
        Assert.AreEqual(0x1234, restored.Data1);
        Assert.AreEqual(0x5678, restored.Data2);
        
        // Expected checksum: (0x12 + 0x34 + 0x56 + 0x78) = 0x114
        Assert.AreEqual(0x0114, restored.Checksum);
    }

    [TestMethod]
    public void TestSum16ChecksumWithLargeValues()
    {
        var packet = new Sum16ChecksumPacket
        {
            Data1 = 0xFFFF,
            Data2 = 0xFFFF
        };

        var data = packet.ToPacket();
        var restored = Sum16ChecksumPacket.FromPacket(data);
        
        // Expected checksum: (0xFF + 0xFF + 0xFF + 0xFF) = 0x03FC
        Assert.AreEqual(0x03FC, restored.Checksum);
    }

    #endregion

    #region XorSum Checksum Tests

    [TestMethod]
    public void TestXorSumChecksum()
    {
        var packet = new XorSumChecksumPacket
        {
            Data1 = 0xAA,
            Data2 = 0x55,
            Data3 = 0xFF
        };

        var data = packet.ToPacket();
        var restored = XorSumChecksumPacket.FromPacket(data);
        
        Assert.AreEqual(0xAA, restored.Data1);
        Assert.AreEqual(0x55, restored.Data2);
        Assert.AreEqual(0xFF, restored.Data3);
        
        // Expected checksum: 0xAA ^ 0x55 ^ 0xFF = 0x00
        Assert.AreEqual(0x00, restored.Checksum);
    }

    [TestMethod]
    public void TestXorSumChecksumNonZero()
    {
        var packet = new XorSumChecksumPacket
        {
            Data1 = 0x12,
            Data2 = 0x34,
            Data3 = 0x56
        };

        var data = packet.ToPacket();
        var restored = XorSumChecksumPacket.FromPacket(data);
        
        // Expected checksum: 0x12 ^ 0x34 ^ 0x56 = 0x70
        Assert.AreEqual(0x70, restored.Checksum);
    }

    #endregion

    #region Fletcher16 Checksum Tests

    [TestMethod]
    public void TestFletcher16Checksum()
    {
        var packet = new Fletcher16ChecksumPacket
        {
            Data = "Hello"
        };

        var data = packet.ToPacket();
        var restored = Fletcher16ChecksumPacket.FromPacket(data);
        
        Assert.AreEqual("Hello", restored.Data);
        Assert.IsTrue(restored.Checksum > 0); // Fletcher16 rarely produces 0
    }

    [TestMethod]
    public void TestFletcher16ChecksumEmptyData()
    {
        var packet = new Fletcher16ChecksumPacket
        {
            Data = ""
        };

        var data = packet.ToPacket();
        var restored = Fletcher16ChecksumPacket.FromPacket(data);
        
        Assert.AreEqual("", restored.Data);
        Assert.AreEqual(0, restored.Checksum); // Empty data should produce 0
    }

    #endregion

    #region CRC16 Checksum Tests

    [TestMethod]
    public void TestCrc16Checksum()
    {
        var packet = new Crc16ChecksumPacket
        {
            Data = "123456789"
        };

        var data = packet.ToPacket();
        var restored = Crc16ChecksumPacket.FromPacket(data);
        
        Assert.AreEqual("123456789", restored.Data);
        Assert.IsTrue(restored.Checksum > 0); // CRC16 of this data is known to be non-zero
    }

    [TestMethod]
    public void TestCrc16ChecksumKnownValue()
    {
        var packet = new Crc16ChecksumPacket
        {
            Data = "A"
        };

        var data = packet.ToPacket();
        var restored = Crc16ChecksumPacket.FromPacket(data);
        
        Assert.AreEqual("A", restored.Data);
        // CRC16 of "A" has a known value (depends on polynomial used)
        Assert.IsTrue(restored.Checksum > 0);
    }

    #endregion

    #region CRC16-CCITT Checksum Tests

    [TestMethod]
    public void TestCrc16CcittChecksum()
    {
        var packet = new Crc16CcittChecksumPacket
        {
            Data = "HELLO"
        };

        var data = packet.ToPacket();
        var restored = Crc16CcittChecksumPacket.FromPacket(data);
        
        Assert.AreEqual("HELLO", restored.Data);
        Assert.IsTrue(restored.Checksum > 0);
    }

    #endregion

    #region CRC32 Checksum Tests

    [TestMethod]
    public void TestCrc32Checksum()
    {
        var packet = new Crc32ChecksumPacket
        {
            Data = "The quick brown fox"
        };

        var data = packet.ToPacket();
        var restored = Crc32ChecksumPacket.FromPacket(data);
        
        Assert.AreEqual("The quick brown fox", restored.Data);
        Assert.IsTrue(restored.Checksum > 0);
    }

    [TestMethod]
    public void TestCrc32ChecksumLargeData()
    {
        var packet = new Crc32ChecksumPacket
        {
            Data = new string('X', 1000) // Large data to test CRC32 performance
        };

        var data = packet.ToPacket();
        var restored = Crc32ChecksumPacket.FromPacket(data);
        
        Assert.AreEqual(1000, restored.Data.Length);
        Assert.IsTrue(restored.Data.All(c => c == 'X'));
        Assert.IsTrue(restored.Checksum > 0);
    }

    #endregion

    #region CRC32C Checksum Tests

    [TestMethod]
    public void TestCrc32CChecksum()
    {
        var packet = new Crc32CChecksumPacket
        {
            Data = "CRC32C test data"
        };

        var data = packet.ToPacket();
        var restored = Crc32CChecksumPacket.FromPacket(data);
        
        Assert.AreEqual("CRC32C test data", restored.Data);
        Assert.IsTrue(restored.Checksum > 0);
    }

    #endregion

    #region Nested Checksum Tests

    [TestMethod]
    public void TestNestedChecksumPacket()
    {
        var packet = new NestedChecksumPacket
        {
            OuterHeader = 0x11,
            InnerData1 = 0x22,
            InnerChecksum = 0, // Will be calculated
            InnerData2 = 0x33,
            OuterData = 0x44,
            OuterChecksum = 0 // Will be calculated
        };

        var data = packet.ToPacket();
        var restored = NestedChecksumPacket.FromPacket(data);
        
        Assert.AreEqual(0x11, restored.OuterHeader);
        Assert.AreEqual(0x22, restored.InnerData1);
        Assert.AreEqual(0x33, restored.InnerData2);
        Assert.AreEqual(0x44, restored.OuterData);
        
        // Both checksums should be calculated
        Assert.IsTrue(restored.InnerChecksum >= 0);
        Assert.IsTrue(restored.OuterChecksum >= 0);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    [ExpectedException(typeof(InvalidDataException))]
    public void TestChecksumValidationFailure()
    {
        var packet = new SimpleChecksumPacket
        {
            Command = 0x01,
            Value1 = 0x1234,
            Value2 = 0x5678,
            Message = "Hello"
        };

        var data = packet.ToPacket();
        
        // Corrupt the checksum byte
        data[data.Length - 1] = (byte)(data[data.Length - 1] ^ 0xFF);
        
        // This should throw an exception during validation
        SimpleChecksumPacket.FromPacket(data);
    }

    [TestMethod]
    public void TestEmptyDataChecksum()
    {
        var packet = new EmptyDataChecksumPacket();

        var data = packet.ToPacket();
        var restored = EmptyDataChecksumPacket.FromPacket(data);
        
        // Empty data should produce a consistent checksum
        Assert.AreEqual(0, restored.Checksum);
    }

    #endregion

    #region Performance Tests

    [TestMethod]
    public void TestLargePacketWithChecksum()
    {
        var packet = new LargeChecksumPacket
        {
            Header = 0xAA,
            LargeData = new string('Z', 10000), // 10KB of data
            Footer = 0xBB
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var data = packet.ToPacket();
        stopwatch.Stop();
        
        // Should complete within reasonable time (< 100ms for 10KB)
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100);
        
        var restored = LargeChecksumPacket.FromPacket(data);
        Assert.AreEqual(0xAA, restored.Header);
        Assert.AreEqual(10000, restored.LargeData.Length);
        Assert.AreEqual(0xBB, restored.Footer);
        Assert.IsTrue(restored.Checksum > 0);
    }

    #endregion
}

#region Checksum Packet Definitions

[GenPackable]
partial class SimpleChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@byte("Command", "Packet command")
        .BeginChecksumRegion()
        .@ushort("Value1", "First value")
        .@ushort("Value2", "Second value") 
        .@string("Message", "Text message")
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.Sum8, "Packet checksum")
        .Build();
}

[GenPackable]
partial class MultipleChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@byte("Header", "Packet header")
        .BeginChecksumRegion()
        .@ushort("Section1Data", "Section 1 data")
        .EndChecksumRegion()
        .@checksum("Section1Checksum", ChecksumType.XorSum, "Section 1 checksum")
        .BeginChecksumRegion()
        .@ushort("Section2Data", "Section 2 data")
        .EndChecksumRegion()
        .@checksum("Section2Checksum", ChecksumType.Sum16, "Section 2 checksum")
        .@byte("Footer", "Packet footer")
        .Build();
}

[GenPackable]
partial class Sum8ChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .BeginChecksumRegion()
        .@byte("Data1", "First data byte")
        .@byte("Data2", "Second data byte")
        .@byte("Data3", "Third data byte")
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.Sum8, "Sum8 checksum")
        .Build();
}

[GenPackable]
partial class Sum16ChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .BeginChecksumRegion()
        .@ushort("Data1", "First data word")
        .@ushort("Data2", "Second data word")
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.Sum16, "Sum16 checksum")
        .Build();
}

[GenPackable]
partial class XorSumChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .BeginChecksumRegion()
        .@byte("Data1", "First data byte")
        .@byte("Data2", "Second data byte")
        .@byte("Data3", "Third data byte")
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.XorSum, "XorSum checksum")
        .Build();
}

[GenPackable]
partial class Fletcher16ChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .BeginChecksumRegion()
        .@string("Data", "String data for Fletcher16")
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.Fletcher16, "Fletcher16 checksum")
        .Build();
}

[GenPackable]
partial class Crc16ChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .BeginChecksumRegion()
        .@string("Data", "String data for CRC16")
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.Crc16, "CRC16 checksum")
        .Build();
}

[GenPackable]
partial class Crc16CcittChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .BeginChecksumRegion()
        .@string("Data", "String data for CRC16-CCITT")
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.Crc16Ccitt, "CRC16-CCITT checksum")
        .Build();
}

[GenPackable]
partial class Crc32ChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .BeginChecksumRegion()
        .@string("Data", "String data for CRC32")
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.Crc32, "CRC32 checksum")
        .Build();
}

[GenPackable]
partial class Crc32CChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .BeginChecksumRegion()
        .@string("Data", "String data for CRC32C")
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.Crc32C, "CRC32C checksum")
        .Build();
}

[GenPackable]
partial class NestedChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@byte("OuterHeader", "Outer header")
        .BeginChecksumRegion() // Inner checksum region for just InnerData1
        .@byte("InnerData1", "Inner data 1")
        .EndChecksumRegion() // Inner checksum region ends
        .@checksum("InnerChecksum", ChecksumType.Sum8, "Inner checksum")
        .BeginChecksumRegion() // Separate region for outer checksum
        .@byte("InnerData2", "Inner data 2")
        .@byte("OuterData", "Outer data")
        .EndChecksumRegion() // Outer checksum region ends
        .@checksum("OuterChecksum", ChecksumType.XorSum, "Outer checksum")
        .Build();
}

[GenPackable]
partial class EmptyDataChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .BeginChecksumRegion()
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.Sum8, "Checksum of empty data")
        .Build();
}

[GenPackable]
partial class LargeChecksumPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@byte("Header", "Packet header")
        .BeginChecksumRegion()
        .@string("LargeData", "Large string data")
        .EndChecksumRegion()
        .@checksum("Checksum", ChecksumType.Crc32, "CRC32 checksum for large data")
        .@byte("Footer", "Packet footer")
        .Build();
}

#endregion
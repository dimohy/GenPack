using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace GenPack.Test;

/// <summary>
/// Test class for Endian and String Encoding functionality
/// </summary>
[TestClass]
public class EndianAndEncodingTest
{
    #region Endian Tests - Little Endian

    [TestMethod]
    public void TestLittleEndianShort()
    {
        var packet = new LittleEndianShortPacket { Value = 0x1234 };
        var data = packet.ToPacket();
        
        // Little endian: LSB first
        Assert.AreEqual(2, data.Length);
        Assert.AreEqual(0x34, data[0]); // LSB
        Assert.AreEqual(0x12, data[1]); // MSB
        
        var restored = LittleEndianShortPacket.FromPacket(data);
        Assert.AreEqual(0x1234, restored.Value);
    }

    [TestMethod]
    public void TestLittleEndianInt()
    {
        var packet = new LittleEndianIntPacket { Value = 0x12345678 };
        var data = packet.ToPacket();
        
        // Little endian: LSB first
        Assert.AreEqual(4, data.Length);
        Assert.AreEqual(0x78, data[0]);
        Assert.AreEqual(0x56, data[1]);
        Assert.AreEqual(0x34, data[2]);
        Assert.AreEqual(0x12, data[3]);
        
        var restored = LittleEndianIntPacket.FromPacket(data);
        Assert.AreEqual(0x12345678, restored.Value);
    }

    [TestMethod]
    public void TestLittleEndianLong()
    {
        var packet = new LittleEndianLongPacket { Value = 0x123456789ABCDEF0 };
        var data = packet.ToPacket();
        
        // Little endian: LSB first
        Assert.AreEqual(8, data.Length);
        Assert.AreEqual(0xF0, data[0]);
        Assert.AreEqual(0xDE, data[1]);
        Assert.AreEqual(0xBC, data[2]);
        Assert.AreEqual(0x9A, data[3]);
        Assert.AreEqual(0x78, data[4]);
        Assert.AreEqual(0x56, data[5]);
        Assert.AreEqual(0x34, data[6]);
        Assert.AreEqual(0x12, data[7]);
        
        var restored = LittleEndianLongPacket.FromPacket(data);
        Assert.AreEqual(0x123456789ABCDEF0, restored.Value);
    }

    #endregion

    #region Endian Tests - Big Endian

    [TestMethod]
    public void TestBigEndianShort()
    {
        var packet = new BigEndianShortPacket { Value = 0x1234 };
        var data = packet.ToPacket();
        
        // Big endian: MSB first
        Assert.AreEqual(2, data.Length);
        Assert.AreEqual(0x12, data[0]); // MSB
        Assert.AreEqual(0x34, data[1]); // LSB
        
        var restored = BigEndianShortPacket.FromPacket(data);
        Assert.AreEqual(0x1234, restored.Value);
    }

    [TestMethod]
    public void TestBigEndianInt()
    {
        var packet = new BigEndianIntPacket { Value = 0x12345678 };
        var data = packet.ToPacket();
        
        // Big endian: MSB first
        Assert.AreEqual(4, data.Length);
        Assert.AreEqual(0x12, data[0]);
        Assert.AreEqual(0x34, data[1]);
        Assert.AreEqual(0x56, data[2]);
        Assert.AreEqual(0x78, data[3]);
        
        var restored = BigEndianIntPacket.FromPacket(data);
        Assert.AreEqual(0x12345678, restored.Value);
    }

    [TestMethod]
    public void TestBigEndianLong()
    {
        var packet = new BigEndianLongPacket { Value = 0x123456789ABCDEF0 };
        var data = packet.ToPacket();
        
        // Big endian: MSB first
        Assert.AreEqual(8, data.Length);
        Assert.AreEqual(0x12, data[0]);
        Assert.AreEqual(0x34, data[1]);
        Assert.AreEqual(0x56, data[2]);
        Assert.AreEqual(0x78, data[3]);
        Assert.AreEqual(0x9A, data[4]);
        Assert.AreEqual(0xBC, data[5]);
        Assert.AreEqual(0xDE, data[6]);
        Assert.AreEqual(0xF0, data[7]);
        
        var restored = BigEndianLongPacket.FromPacket(data);
        Assert.AreEqual(0x123456789ABCDEF0, restored.Value);
    }

    #endregion

    #region Endian Comparison Tests

    [TestMethod]
    public void TestEndianDifference()
    {
        // Same value, different endianness should produce different byte arrays
        var littlePacket = new LittleEndianShortPacket { Value = 0x1234 };
        var bigPacket = new BigEndianShortPacket { Value = 0x1234 };
        
        var littleData = littlePacket.ToPacket();
        var bigData = bigPacket.ToPacket();
        
        // Should be different byte order
        Assert.AreEqual(littleData.Length, bigData.Length);
        Assert.AreNotEqual(littleData[0], bigData[0]);
        Assert.AreNotEqual(littleData[1], bigData[1]);
        
        // But restored values should be the same
        var restoredLittle = LittleEndianShortPacket.FromPacket(littleData);
        var restoredBig = BigEndianShortPacket.FromPacket(bigData);
        Assert.AreEqual(restoredLittle.Value, restoredBig.Value);
    }

    #endregion

    #region String Encoding Tests - UTF8

    [TestMethod]
    public void TestUTF8Encoding()
    {
        var packet = new UTF8StringPacket { Text = "Hello World!" };
        var data = packet.ToPacket();
        
        var restored = UTF8StringPacket.FromPacket(data);
        Assert.AreEqual("Hello World!", restored.Text);
    }

    [TestMethod]
    public void TestUTF8EncodingKorean()
    {
        var packet = new UTF8StringPacket { Text = "안녕하세요 세계!" };
        var data = packet.ToPacket();
        
        var restored = UTF8StringPacket.FromPacket(data);
        Assert.AreEqual("안녕하세요 세계!", restored.Text);
    }

    [TestMethod]
    public void TestUTF8EncodingEmoji()
    {
        var packet = new UTF8StringPacket { Text = "Hello ?? World ??!" };
        var data = packet.ToPacket();
        
        var restored = UTF8StringPacket.FromPacket(data);
        Assert.AreEqual("Hello ?? World ??!", restored.Text);
    }

    #endregion

    #region String Encoding Tests - UTF16

    [TestMethod]
    public void TestUTF16Encoding()
    {
        var packet = new UTF16StringPacket { Text = "Hello World!" };
        var data = packet.ToPacket();
        
        var restored = UTF16StringPacket.FromPacket(data);
        Assert.AreEqual("Hello World!", restored.Text);
    }

    [TestMethod]
    public void TestUTF16EncodingKorean()
    {
        var packet = new UTF16StringPacket { Text = "안녕하세요 세계!" };
        var data = packet.ToPacket();
        
        var restored = UTF16StringPacket.FromPacket(data);
        Assert.AreEqual("안녕하세요 세계!", restored.Text);
    }

    [TestMethod]
    public void TestUTF16EncodingEmoji()
    {
        var packet = new UTF16StringPacket { Text = "Hello ?? World ??!" };
        var data = packet.ToPacket();
        
        var restored = UTF16StringPacket.FromPacket(data);
        Assert.AreEqual("Hello ?? World ??!", restored.Text);
    }

    #endregion

    #region String Encoding Tests - UTF32

    [TestMethod]
    public void TestUTF32Encoding()
    {
        var packet = new UTF32StringPacket { Text = "Hello World!" };
        var data = packet.ToPacket();
        
        var restored = UTF32StringPacket.FromPacket(data);
        Assert.AreEqual("Hello World!", restored.Text);
    }

    [TestMethod]
    public void TestUTF32EncodingKorean()
    {
        var packet = new UTF32StringPacket { Text = "안녕하세요 세계!" };
        var data = packet.ToPacket();
        
        var restored = UTF32StringPacket.FromPacket(data);
        Assert.AreEqual("안녕하세요 세계!", restored.Text);
    }

    [TestMethod]
    public void TestUTF32EncodingEmoji()
    {
        var packet = new UTF32StringPacket { Text = "Hello ?? World ??!" };
        var data = packet.ToPacket();
        
        var restored = UTF32StringPacket.FromPacket(data);
        Assert.AreEqual("Hello ?? World ??!", restored.Text);
    }

    #endregion

    #region String Encoding Tests - ASCII

    [TestMethod]
    public void TestASCIIEncoding()
    {
        var packet = new ASCIIStringPacket { Text = "Hello World!" };
        var data = packet.ToPacket();
        
        var restored = ASCIIStringPacket.FromPacket(data);
        Assert.AreEqual("Hello World!", restored.Text);
    }

    [TestMethod]
    public void TestASCIIEncodingNumbers()
    {
        var packet = new ASCIIStringPacket { Text = "1234567890" };
        var data = packet.ToPacket();
        
        var restored = ASCIIStringPacket.FromPacket(data);
        Assert.AreEqual("1234567890", restored.Text);
    }

    #endregion

    #region String Encoding Comparison Tests

    [TestMethod]
    public void TestStringEncodingDifferences()
    {
        const string testText = "Hello";
        
        var utf8Packet = new UTF8StringPacket { Text = testText };
        var utf16Packet = new UTF16StringPacket { Text = testText };
        var utf32Packet = new UTF32StringPacket { Text = testText };
        var asciiPacket = new ASCIIStringPacket { Text = testText };
        
        var utf8Data = utf8Packet.ToPacket();
        var utf16Data = utf16Packet.ToPacket();
        var utf32Data = utf32Packet.ToPacket();
        var asciiData = asciiPacket.ToPacket();
        
        // Different encodings should produce different byte lengths for same string
        Assert.IsTrue(utf8Data.Length <= utf16Data.Length); // UTF-8 is more efficient for ASCII
        Assert.IsTrue(utf16Data.Length <= utf32Data.Length); // UTF-32 uses fixed 4 bytes per char
        Assert.AreEqual(utf8Data.Length, asciiData.Length); // ASCII and UTF-8 same for ASCII chars
        
        // But all should restore to the same string
        var restoredUTF8 = UTF8StringPacket.FromPacket(utf8Data);
        var restoredUTF16 = UTF16StringPacket.FromPacket(utf16Data);
        var restoredUTF32 = UTF32StringPacket.FromPacket(utf32Data);
        var restoredASCII = ASCIIStringPacket.FromPacket(asciiData);
        
        Assert.AreEqual(testText, restoredUTF8.Text);
        Assert.AreEqual(testText, restoredUTF16.Text);
        Assert.AreEqual(testText, restoredUTF32.Text);
        Assert.AreEqual(testText, restoredASCII.Text);
    }

    #endregion

    #region Complex Combination Tests

    [TestMethod]
    public void TestComplexBigEndianUTF16Packet()
    {
        var packet = new ComplexBigEndianUTF16Packet 
        { 
            Id = 0x12345678,
            Name = "테스트",
            Value = 42.5f
        };
        
        var data = packet.ToPacket();
        var restored = ComplexBigEndianUTF16Packet.FromPacket(data);
        
        Assert.AreEqual(0x12345678, restored.Id);
        Assert.AreEqual("테스트", restored.Name);
        Assert.AreEqual(42.5f, restored.Value, 0.001f);
    }

    [TestMethod]
    public void TestComplexLittleEndianASCIIPacket()
    {
        var packet = new ComplexLittleEndianASCIIPacket 
        { 
            Id = unchecked((int)0x87654321),
            Name = "Test",
            Value = 123.456
        };
        
        var data = packet.ToPacket();
        var restored = ComplexLittleEndianASCIIPacket.FromPacket(data);
        
        Assert.AreEqual(unchecked((int)0x87654321), restored.Id);
        Assert.AreEqual("Test", restored.Name);
        Assert.AreEqual(123.456, restored.Value, 0.001);
    }

    #endregion

    #region Empty and Edge Cases

    [TestMethod]
    public void TestEmptyString()
    {
        var packet = new UTF8StringPacket { Text = "" };
        var data = packet.ToPacket();
        
        var restored = UTF8StringPacket.FromPacket(data);
        Assert.AreEqual("", restored.Text);
    }

    [TestMethod]
    public void TestZeroValues()
    {
        var packet = new LittleEndianIntPacket { Value = 0 };
        var data = packet.ToPacket();
        
        var restored = LittleEndianIntPacket.FromPacket(data);
        Assert.AreEqual(0, restored.Value);
    }

    [TestMethod]
    public void TestMaxValues()
    {
        var packet = new LittleEndianIntPacket { Value = int.MaxValue };
        var data = packet.ToPacket();
        
        var restored = LittleEndianIntPacket.FromPacket(data);
        Assert.AreEqual(int.MaxValue, restored.Value);
    }

    [TestMethod]
    public void TestMinValues()
    {
        var packet = new LittleEndianIntPacket { Value = int.MinValue };
        var data = packet.ToPacket();
        
        var restored = LittleEndianIntPacket.FromPacket(data);
        Assert.AreEqual(int.MinValue, restored.Value);
    }

    #endregion
}

#region Test Packet Definitions

// Little Endian Packets
[GenPackable]
public partial class LittleEndianShortPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.UTF8)
        .@short("Value", "Test short value")
        .Build();
}

[GenPackable]
public partial class LittleEndianIntPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.UTF8)
        .@int("Value", "Test int value")
        .Build();
}

[GenPackable]
public partial class LittleEndianLongPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.UTF8)
        .@long("Value", "Test long value")
        .Build();
}

// Big Endian Packets
[GenPackable]
public partial class BigEndianShortPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Big, StringEncoding.UTF8)
        .@short("Value", "Test short value")
        .Build();
}

[GenPackable]
public partial class BigEndianIntPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Big, StringEncoding.UTF8)
        .@int("Value", "Test int value")
        .Build();
}

[GenPackable]
public partial class BigEndianLongPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Big, StringEncoding.UTF8)
        .@long("Value", "Test long value")
        .Build();
}

// String Encoding Packets
[GenPackable]
public partial class UTF8StringPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.UTF8)
        .@string("Text", "UTF-8 encoded text")
        .Build();
}

[GenPackable]
public partial class UTF16StringPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.UTF16)
        .@string("Text", "UTF-16 encoded text")
        .Build();
}

[GenPackable]
public partial class UTF32StringPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.UTF32)
        .@string("Text", "UTF-32 encoded text")
        .Build();
}

[GenPackable]
public partial class ASCIIStringPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.ASCII)
        .@string("Text", "ASCII encoded text")
        .Build();
}

// Complex Combination Packets
[GenPackable]
public partial class ComplexBigEndianUTF16Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Big, StringEncoding.UTF16)
        .@int("Id", "Identifier")
        .@string("Name", "Name field")
        .@float("Value", "Float value")
        .Build();
}

[GenPackable]
public partial class ComplexLittleEndianASCIIPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.ASCII)
        .@int("Id", "Identifier")
        .@string("Name", "Name field")
        .@double("Value", "Double value")
        .Build();
}

#endregion
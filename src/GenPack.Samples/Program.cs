// See https://aka.ms/new-console-template for more information
using GenPack;

// Test 1: Default Little Endian + UTF8 (기본값)
var p1 = new PeoplePacket()
{
    Age = 10,
    Name = "John"
};
var data1 = p1.ToPacket();
var newP1 = PeoplePacket.FromPacket(data1);
Console.WriteLine($"Default: {newP1}");

// Test 2: Big Endian + ASCII
var p2 = new BigEndianPacket()
{
    Age = 10,
    Name = "John"
};
var data2 = p2.ToPacket();
var newP2 = BigEndianPacket.FromPacket(data2);
Console.WriteLine($"Big Endian + ASCII: {newP2}");

// Test 3: UTF16 encoding
var p3 = new UTF16Packet()
{
    Age = 10,
    Name = "한글"
};
var data3 = p3.ToPacket();
var newP3 = UTF16Packet.FromPacket(data3);
Console.WriteLine($"UTF16: {newP3}");

// Show byte differences
Console.WriteLine($"\nDefault packet bytes: [{string.Join(", ", data1.Select(b => $"0x{b:X2}"))}]");
Console.WriteLine($"Big Endian packet bytes: [{string.Join(", ", data2.Select(b => $"0x{b:X2}"))}]");
Console.WriteLine($"UTF16 packet bytes: [{string.Join(", ", data3.Select(b => $"0x{b:X2}"))}]");

[GenPackable]
public partial record PeoplePacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@short("Age", "Age description")
        .@string("Name", "Name description")
        .Build();
}

[GenPackable]
public partial record BigEndianPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Big, StringEncoding.ASCII)
        .@short("Age", "Age description")
        .@string("Name", "Name description")
        .Build();
}

[GenPackable]
public partial record UTF16Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.UTF16)
        .@short("Age", "Age description")
        .@string("Name", "Name description")
        .Build();
}

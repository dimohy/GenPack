using GenPack;
using GenPack.Test.Console;

Test1Packet p = new()
{
    Value1 = 0x10
};

using var ms = new MemoryStream();
p.ToPacket(ms);
var data = ms.ToArray();
;

var newP2 = Test1Packet.FromPacket(new MemoryStream(data));
;


[GenPackable]
public partial class Test1Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@byte("Value1", "값1")
        .Build();
}


[GenPackable]
public partial class TestPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.ASCII)
        .@array<byte>("ByteArray", 50, "byte 배열")
        .@array<int>("IntegerArray", 50, "int 배열")
        .@array<Test2Packet>("Test2PacketArray", 5, "개체 배열")
        .@object<Test2Packet>("Test2Packet", "개체 포함")
        .@list<Test2Packet>("Test2PacketList", "개체 리스트")
        .@dict<Test2Packet>("Test2PacketDict", "개체 딕셔너리")
        .@byte(name: "Command", description: "명령어")
        .BeginPointChecksum()
        .@byte("Value1")
        .@short("Value2")
        .@string("Value3", "a", 5)
        .EndPointChecksum()
        .@checkum(ChecksumType.Sum8)
        .Build();
}
using GenPack;
using GenPack.Test.Console;

var pb = PacketSchemaBuilder.Create();

//var packetSchema = pb.Build();


//var tc = new TestPacket();
////tc.Print();

//tc.Value3 = "123";
//Console.WriteLine(tc.Value3);


////var tc2 = new Test2Packet();
////tc2.Print();

//using var ms = new MemoryStream();
//var sw = new BinaryWriter(ms);
////sw.Write("123");

var p = new TestPacket();

using var ms = new MemoryStream();
p.ToPacket(ms);


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
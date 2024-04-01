using GenPack;
using GenPack.Test.Console;

var pb = PacketSchemaBuilder.Create();

var packetSchema = pb.Build();


var tc = new TestPacket();
//tc.Print();

tc.Value3 = "123";
Console.WriteLine(tc.Value3);


//var tc2 = new Test2Packet();
//tc2.Print();

[GenPackable]
public partial class TestPacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.ASCII)
        //.@object<GenPack.Test.Console.Test2Packet>("Test2Packet", "개체 포함")
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
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
        .@byte(name: "Command", description: "명령어")
        .BeginPointChecksum()
        .@byte("Value1")
        .@short("Value2")
        .@string("Value3", "a", 5)
        .EndPointChecksum()
        .@checkum(ChecksumType.Sum8)
        .Build();
}
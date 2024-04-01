namespace GenPack.Test.Console;

[GenPackable]
public partial class Test2Packet
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create(UnitEndian.Little, StringEncoding.ASCII)
        .@byte("Command", "명령어")
        .BeginPointChecksum()
        .@byte("Byte")
        .@short("Short")
        .@string("String", "test", 5)
        .EndPointChecksum()
        .@checkum(ChecksumType.Sum8)
        .Build();
}

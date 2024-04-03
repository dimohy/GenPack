// See https://aka.ms/new-console-template for more information
using GenPack;

var p = new PeoplePacket()
{
    Age = 10,
    Name = "John"
};
var data = p.ToPacket();
var newP = PeoplePacket.FromPacket(data);

Console.WriteLine(newP);


[GenPackable]
public partial record PeoplePacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@short("Age", "Age description")
        .@string("Name", "Name description")
        .Build();
}

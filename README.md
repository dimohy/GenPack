# GenPack
GenPack is a library that uses the .NET source generator to automatically generate packets as classes once you define a schema for the packets.
It's easy to use and the results are useful.

GenPack also works well with Native AOT. You can take advantage of the benefits of Native AOT.

## Simple to use
```csharp
[GenPackable]
public partial record PeoplePacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@short("Age", "Age description")
        .@string("Name", "Name description")
        .Build();
}
```

The following code is automatically generated by the schema information.

```csharp
    public partial record PeoplePacket : GenPack.IGenPackable
    {
        /// <summary>
        /// Age description
        /// </summary>
        public short Age { get; set; }
        /// <summary>
        /// Name description
        /// </summary>
        public string Name { get; set; } = string.Empty;
        public byte[] ToPacket()
        {
            using var ms = new System.IO.MemoryStream();
            ToPacket(ms);
            return ms.ToArray();
        }
        public void ToPacket(System.IO.Stream stream)
        {
            System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);
            writer.Write(Age);
            writer.Write(Name);
        }
        public static PeoplePacket FromPacket(byte[] data)
        {
            using var ms = new System.IO.MemoryStream(data);
            return FromPacket(ms);
        }
        public static PeoplePacket FromPacket(System.IO.Stream stream)
        {
            PeoplePacket result = new PeoplePacket();
            System.IO.BinaryReader reader = new System.IO.BinaryReader(stream);
            int size = 0;
            byte[] buffer = null;
            result.Age = reader.ReadInt16();
            result.Name = reader.ReadString();
            return result;
        }
    }
```

It's simple to use. You can binary serialize with `ToPacket()` and deserialize with `FromPacket()`.

```csharp
var p = new PeoplePacket()
{
    Age = 10,
    Name = "John"
};
var data = p.ToPacket();
var newP = PeoplePacket.FromPacket(data);

Console.WriteLine(newP);
```

```shell
PeoplePacket { Age = 10, Name = John }
```

## How to create a packet schema
Decorate the attribute of `class` or `record` with `GenPackable`. At this point, the target must be given `partial`.
GenPack's packet schema is represented by creating a `PacketSchema` using the `PacketSchemaBuilder`.

```csharp
[GenPackable]
public partial record PeoplePacket
{
    public readonly static PacketSchema Schema = PacketSchemaBuilder.Create()
        .@short("Age", "Age description")
        .@string("Name", "Name description")
        .Build();
}
```

The format beginning with `@` means the schema property to be created. For example, `@short("Age", "Age description")` gives the `Age` property the type `short` and the description `Age description`.
This translates to the following,

```csharp
        /// <summary>
        /// Age description
        /// </summary>
        public short Age { get; set; }
```
You can then use the auto-generated properties.

```csharp
var p = new PeoplePacket()
p.Age = 32;
```

### Schema Properties
| Property        | Description         | Bits | Arguments                        |
|-----------------|---------------------|------|----------------------------------|
| @byte           | byte                |   8  | property name, description       |
| @sbyte          | signedyte           |   8  | property name, description       |
| @short          | short int           |  16  | property name, description       |
| @ushort         | unsigned short int  |  16  | property name, description       |
| @int            | int                 |  32  | property name, description       |
| @uint           | unsigned int        |  32  | property name, description       |
| @long           | long int            |  64  | property name, description       |
| @ulong          | unsigned long int   |  64  | property name, description       |
| @float          | single float        |  32  | property name, description       |
| @double         | double float        |  64  | property name, description       |
| @string         | string              |   N  | property name, description       |
| @object\<type\> | genpackable object  |   N  | property name, description       |
| @list\<type\>   | variable list       |   N  | property name, description       |
| @dict\<type\>   | variable dictionary |   N  | property name, description       |
| @array\<type\>  | fixed array         |   N  | property name, size, description |

## Tasks
- [ ] Support for Endian, string Encoding.
- [ ] Support for checksums.
- [ ] Support 8-bit, 16-bit, 32-bit, 64-bit, or variable 7-bit sizes for `@list` and `@dict`.
- [ ] Automatically select and deserialize target structures based on packet command(identification code).
- [ ] Generate JSON and gRPC schema with `PacketSchema`.
- [ ] Process device packets with uncomplicated packet structures.
- [ ] Process structures with complex packets, such as PLCs.
- [ ] Process packets that require speed, such as `MemoryPack`.

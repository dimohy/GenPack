using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using SchemaItem = (string SchemaName, string SchemaType, bool IsDefaultType, Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentSyntax[] Arguments);
using SchemaConfig = (string EndianValue, string StringEncodingValue);

namespace GenPack;

[Generator]
public class GenPackGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUGGENERATOR
        if (Debugger.IsAttached is false)
        {
            Debugger.Launch();
        }
#endif

        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
            transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static c => c is not null);

        var compilationAndMethods = context.CompilationProvider.Combine(classDeclarations.Collect());
        context.RegisterSourceOutput(compilationAndMethods, static (spc, source) => Execute(source.Left, source.Right, spc));

        // ------

        static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax or RecordDeclarationSyntax && (node as TypeDeclarationSyntax)!.AttributeLists.Count > 0;
        static TypeDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax or RecordDeclarationSyntax)
            {
                var typeDeclaration = (context.Node as TypeDeclarationSyntax)!;
                foreach (var attributeList in typeDeclaration.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                            continue;

                        var attributeContainingType = attributeSymbol.ContainingType;
                        var fullname = attributeContainingType.ToDisplayString();
                        if (fullname is $"{nameof(GenPack)}.{nameof(GenPackableAttribute)}")
                            return typeDeclaration;
                    }
                }
            }

            return null;
        }
    }

    private static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty == true)
            return;

        var distinctClasses = classes.Distinct();
        foreach (var @class in distinctClasses)
        {
            if (@class is null)
                continue;

            var @namespace = GetNamespace(@class);

            var classSymbol = compilation.GetSemanticModel(@class.SyntaxTree).GetDeclaredSymbol(@class);
            if (classSymbol is null)
                continue;

            var classAttributes = classSymbol.GetAttributes();
            var packableAttribute = classAttributes.FirstOrDefault(a => a.AttributeClass?.Name == nameof(GenPackableAttribute));
            if (packableAttribute is null)
                continue;

            if (classSymbol.GetMembers().FirstOrDefault(m => m is IFieldSymbol fs && fs.IsStatic is true && fs.Type.Name is nameof(PacketSchema)) is not IFieldSymbol packetSchema)
                continue;

            if (packetSchema.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not VariableDeclaratorSyntax packetSchemaSyntax)
                continue;

            var schemaBuilder = new StringBuilder();
            var (schemaItems, schemaConfig) = ParseSchema(compilation, packetSchemaSyntax.DescendantNodes());

            schemaBuilder.AppendLine("#pragma warning disable CS0219");
            
            if (string.IsNullOrEmpty(@namespace) is false)
                schemaBuilder.AppendLine($$"""
                    namespace {{@namespace}}
                    {
                    """);

            schemaBuilder.AppendLine($$"""
                {{D(1)}}public partial {{(@class is ClassDeclarationSyntax ? "class" : "record")}} {{classSymbol.Name}} : GenPack.IGenPackable
                {{D(1)}}{
                """);

            AddProperties(schemaBuilder, schemaItems);
            AddMethods(schemaBuilder, string.IsNullOrEmpty(@namespace) ? classSymbol.Name : $"{@namespace}.{classSymbol.Name}", schemaItems, schemaConfig);

            schemaBuilder.AppendLine($$"""{{D(1)}}}""");

            if (string.IsNullOrEmpty(@namespace) is false)
                schemaBuilder.AppendLine("}");

            context.AddSource($"{classSymbol.Name}Schema.g.cs", SourceText.From(schemaBuilder.ToString(), Encoding.UTF8));
        }
    }

    private static (IReadOnlyList<SchemaItem>, SchemaConfig) ParseSchema(Compilation compilation, IEnumerable<SyntaxNode> nodes)
    {
        List<SchemaItem> result = [];
        SchemaConfig config = ("GenPack.UnitEndian.Little", "GenPack.StringEncoding.UTF8");

        var invocationNodes = nodes.OfType<InvocationExpressionSyntax>().Reverse().ToArray();
        if (invocationNodes.Length is 0)
            return (result, config);
        
        var createNode = invocationNodes.FirstOrDefault(node => 
            node.ChildNodes().First().TryGetInferredMemberName() is nameof(PacketSchemaBuilder.Create));
        
        if (createNode is null)
            return (result, config);

        // Parse Create method arguments for endian and string encoding
        var createArgumentList = createNode.ChildNodes().OfType<ArgumentListSyntax>().FirstOrDefault();
        if (createArgumentList is not null)
        {
            var createArguments = createArgumentList.Arguments.ToArray();
            if (createArguments.Length >= 1)
            {
                var endianArg = createArguments[0].Expression.ToString();
                if (endianArg.Contains("UnitEndian."))
                {
                    config.EndianValue = $"GenPack.{endianArg}";
                }
                else if (!endianArg.Contains("GenPack."))
                {
                    config.EndianValue = $"GenPack.UnitEndian.{endianArg}";
                }
                else
                {
                    config.EndianValue = endianArg;
                }
            }
            if (createArguments.Length >= 2)
            {
                var encodingArg = createArguments[1].Expression.ToString();
                if (encodingArg.Contains("StringEncoding."))
                {
                    config.StringEncodingValue = $"GenPack.{encodingArg}";
                }
                else if (!encodingArg.Contains("GenPack."))
                {
                    config.StringEncodingValue = $"GenPack.StringEncoding.{encodingArg}";
                }
                else
                {
                    config.StringEncodingValue = encodingArg;
                }
            }
        }

        foreach (var node in invocationNodes)
        {
            var childNodes = node.ChildNodes().ToArray();
            if (childNodes.Length < 2)
                continue;

            string schemaName = childNodes[0].TryGetInferredMemberName()!;
            string schemaType = schemaName;
            // Finding generic formats
            if (string.IsNullOrEmpty(schemaName) is true && childNodes[0].ChildNodes().Last() is GenericNameSyntax gns)
            {
                schemaName = gns.Identifier.Value?.ToString() ?? "object";
                schemaType = gns.TypeArgumentList.Arguments.First().ToString();
                // TODO: GetSymbolsWithName()는 일치하는 이름의 모든 유형을 반환하므로 이후 정확한 제네릭 유형만 가져오는 것으로 수정하여야 함
                schemaType = compilation.GetSymbolsWithName(schemaType).FirstOrDefault(x => x is INamedTypeSymbol)?.ToDisplayString() ?? schemaType;
            }
            var arguments = childNodes[1].ChildNodes().OfType<ArgumentSyntax>().ToArray();

            if (schemaName is nameof(PacketSchemaBuilder.Build) is true)
                break;

            if (schemaName is nameof(PacketSchemaBuilder.Create) is true)
                continue;

            // Check if arguments count is greater than 0 and handle accordingly
            if (arguments.Length > 0)
            {
                // Extract the argument type if it's an Enum, otherwise keep the original schema type
                var argumentType = arguments[0].DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault()?.Identifier.Text;
                schemaType = compilation.GetTypeByMetadataName(argumentType ?? string.Empty)?.ToDisplayString() ?? schemaType;
            }

            result.Add((schemaName, schemaType, PacketSchema.IsDefaultType(schemaType), arguments));
        }

        return (result, config);
    }

    private static void AddMethods(StringBuilder sb, string className, IReadOnlyList<SchemaItem> items, SchemaConfig config)
    {
        if (items.Count is 0)
            return;

        AddToPackMethod(sb, items, config);
        AddFromPackMethod(sb, className, items, config);
    }

    private static void AddToPackMethod(StringBuilder sb, IReadOnlyList<SchemaItem> items, SchemaConfig config)
    {
        sb.AppendLine($$"""
            {{D(2)}}public byte[] ToPacket()
            {{D(2)}}{
            {{D(2)}}    using var ms = new System.IO.MemoryStream();
            {{D(2)}}    ToPacket(ms);
            {{D(2)}}    return ms.ToArray();
            {{D(2)}}}
            """);

        sb.AppendLine($$"""
            {{D(2)}}public void ToPacket(System.IO.Stream stream)
            {{D(2)}}{
            {{D(2)}}    using var writer = new GenPack.IO.EndianAwareBinaryWriter(stream, {{config.EndianValue}}, {{config.StringEncodingValue}});
            {{D(2)}}    ToPacket(writer);
            {{D(2)}}}
            """);

        sb.AppendLine($$"""
            {{D(2)}}public void ToPacket(GenPack.IO.EndianAwareBinaryWriter writer)
            {{D(2)}}{
            """);

        foreach (var item in items)
        {
            switch (item.SchemaName)
            {
                case nameof(PacketSchemaBuilder.@byte):
                case nameof(PacketSchemaBuilder.@sbyte):
                case nameof(PacketSchemaBuilder.@short):
                case nameof(PacketSchemaBuilder.@ushort):
                case nameof(PacketSchemaBuilder.@int):
                case nameof(PacketSchemaBuilder.@uint):
                case nameof(PacketSchemaBuilder.@long):
                case nameof(PacketSchemaBuilder.@ulong):
                case nameof(PacketSchemaBuilder.@float):
                case nameof(PacketSchemaBuilder.@double):
                case nameof(PacketSchemaBuilder.@string):
                    {
                        var propertyName = GetPropertyName(item);
                        sb.AppendLine($"{D(3)}writer.Write({propertyName});");
                    }
                    break;
                case nameof(PacketSchemaBuilder.@object):
                    {
                        var propertyName = GetPropertyName(item);
                        sb.AppendLine($"{D(3)}{propertyName}.ToPacket(writer);");
                    }
                    break;
                case nameof(PacketSchemaBuilder.@list):
                    {
                        var propertyName = GetPropertyName(item);
                        var writeMethod = item.IsDefaultType is true ? "writer.Write(item)" : "item.ToPacket(writer)";

                        sb.AppendLine($$"""
                        {{D(3)}}writer.Write7BitEncodedInt({{propertyName}}.Count);
                        {{D(3)}}foreach (var item in {{propertyName}})
                        {{D(3)}}{
                        {{D(3)}}    {{writeMethod}};
                        {{D(3)}}}
                        """);
                    }
                    break;
                case nameof(PacketSchemaBuilder.@dict):
                    {
                        var propertyName = GetPropertyName(item);
                        var writeMethod = item.IsDefaultType is true ? "writer.Write(item.Value)" : "item.Value.ToPacket(writer)";

                        sb.AppendLine($$"""
                        {{D(3)}}writer.Write7BitEncodedInt({{propertyName}}.Count);
                        {{D(3)}}foreach (var item in {{propertyName}})
                        {{D(3)}}{
                        {{D(3)}}    writer.Write(item.Key);
                        {{D(3)}}    {{writeMethod}};
                        {{D(3)}}}
                        """);
                    }
                    break;
                case nameof(PacketSchemaBuilder.@array):
                    {
                        var propertyName = GetPropertyName(item);
                        var writeMethod = item.IsDefaultType is true ? "writer.Write(item)" : "item.ToPacket(writer)";
                        if (item.IsDefaultType is true)
                        {
                            if (item.SchemaType is nameof(PacketSchemaBuilder.@byte))
                            {
                                sb.AppendLine($"{D(3)}writer.Write({propertyName});");
                                break;
                            }
                        }

                        sb.AppendLine($$"""
                        {{D(3)}}foreach (var item in {{propertyName}})
                        {{D(3)}}{
                        {{D(3)}}    {{writeMethod}};
                        {{D(3)}}}
                        """);
                    }
                    break;
                case nameof(PacketSchemaBuilder.BeginChecksumRegion):
                case nameof(PacketSchemaBuilder.BeginPointChecksum):
                    sb.AppendLine($"{D(3)}writer.BeginChecksumRegion();");
                    break;
                case nameof(PacketSchemaBuilder.EndChecksumRegion):
                case nameof(PacketSchemaBuilder.EndPointChecksum):
                    sb.AppendLine($"{D(3)}writer.EndChecksumRegion();");
                    break;
                case nameof(PacketSchemaBuilder.@checksum):
                case nameof(PacketSchemaBuilder.@checkum):
                    {
                        string checksumType;
                        string propertyName = "";
                        
                        if (item.SchemaName == nameof(PacketSchemaBuilder.@checksum))
                        {
                            propertyName = GetPropertyName(item);
                            checksumType = item.Arguments.Length > 1 ? item.Arguments[1].Expression.ToString() : "GenPack.ChecksumType.Sum8";
                        }
                        else
                        {
                            // Legacy @checkum method
                            checksumType = item.Arguments.Length > 0 ? item.Arguments[0].Expression.ToString() : "GenPack.ChecksumType.Sum8";
                        }
                        
                        // Ensure ChecksumType has GenPack namespace prefix
                        if (!checksumType.Contains("GenPack.") && checksumType.Contains("ChecksumType."))
                        {
                            checksumType = $"GenPack.{checksumType}";
                        }
                        
                        if (!string.IsNullOrEmpty(propertyName))
                        {
                            sb.AppendLine($"{D(3)}{propertyName} = ({GetChecksumPropertyType(checksumType)})writer.WriteChecksum({checksumType});");
                        }
                        else
                        {
                            sb.AppendLine($"{D(3)}writer.WriteChecksum({checksumType});");
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        sb.AppendLine($$"""{{D(2)}}}""");
    }

    private static void AddFromPackMethod(StringBuilder sb, string className, IReadOnlyList<SchemaItem> items, SchemaConfig config)
    {
        sb.AppendLine($$"""
            {{D(2)}}public static {{className}} FromPacket(byte[] data)
            {{D(2)}}{
            {{D(2)}}    using var ms = new System.IO.MemoryStream(data);
            {{D(2)}}    return FromPacket(ms);
            {{D(2)}}}
            """);

        sb.AppendLine($$"""
            {{D(2)}}public static {{className}} FromPacket(System.IO.Stream stream)
            {{D(2)}}{
            {{D(2)}}    using var reader = new GenPack.IO.EndianAwareBinaryReader(stream, {{config.EndianValue}}, {{config.StringEncodingValue}});
            {{D(2)}}    return FromPacket(reader);
            {{D(2)}}}
            """);

        sb.AppendLine($$"""
            {{D(2)}}public static {{className}} FromPacket(GenPack.IO.EndianAwareBinaryReader reader)
            {{D(2)}}{
            {{D(2)}}    {{className}} result = new {{className}}();
            {{D(2)}}    int size = 0;
            {{D(2)}}    byte[] buffer = null;
            """);

        foreach (var item in items)
        {
            switch (item.SchemaName)
            {
                case nameof(PacketSchemaBuilder.@byte):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadByte();");
                    break;
                case nameof(PacketSchemaBuilder.@sbyte):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadSByte();");
                    break;
                case nameof(PacketSchemaBuilder.@short):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadInt16();");
                    break;
                case nameof(PacketSchemaBuilder.@ushort):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadUInt16();");
                    break;
                case nameof(PacketSchemaBuilder.@int):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadInt32();");
                    break;
                case nameof(PacketSchemaBuilder.@uint):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadUInt32();");
                    break;
                case nameof(PacketSchemaBuilder.@long):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadInt64();");
                    break;
                case nameof(PacketSchemaBuilder.@ulong):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadUInt64();");
                    break;
                case nameof(PacketSchemaBuilder.@float):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadSingle();");
                    break;
                case nameof(PacketSchemaBuilder.@double):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadDouble();");
                    break;
                case nameof(PacketSchemaBuilder.@string):
                    sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.ReadString();");
                    break;
                case nameof(PacketSchemaBuilder.@object):
                    {
                        sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = {item.SchemaType}.FromPacket(reader);");
                    }
                    break;
                case nameof(PacketSchemaBuilder.@list):
                    {
                        var propertyName = GetPropertyName(item);
                        var readMethod = item.IsDefaultType is true ? GetReadMethod(item.SchemaType) : $"{item.SchemaType}.FromPacket(reader)";

                        sb.AppendLine($$"""
                        {{D(3)}}size = reader.Read7BitEncodedInt();
                        {{D(3)}}for (var i = 0; i < size; i++)
                        {{D(3)}}{
                        {{D(3)}}    result.{{propertyName}}.Add({{readMethod}});
                        {{D(3)}}}
                        """);
                    }
                    break;
                case nameof(PacketSchemaBuilder.@dict):
                    {
                        var propertyName = GetPropertyName(item);
                        var readMethod = item.IsDefaultType is true ? GetReadMethod(item.SchemaType) : $"{item.SchemaType}.FromPacket(reader)";

                        sb.AppendLine($$"""
                        {{D(3)}}size = reader.Read7BitEncodedInt();
                        {{D(3)}}for (var i = 0; i < size; i++)
                        {{D(3)}}{
                        {{D(3)}}    result.{{propertyName}}[reader.ReadString()] = {{readMethod}};
                        {{D(3)}}}
                        """);
                    }
                    break;
                case nameof(PacketSchemaBuilder.@array):
                    {
                        var propertyName = GetPropertyName(item);
                        var readMethod = item.IsDefaultType is true ? GetReadMethod(item.SchemaType) : $"{item.SchemaType}.FromPacket(reader)";
                        var length = int.Parse(item.Arguments[1].Expression.ToString());

                        if (item.IsDefaultType is true)
                        {
                            if (item.SchemaType is nameof(PacketSchemaBuilder.@byte))
                            {
                                sb.AppendLine($$"""
                                    {{D(3)}}buffer = reader.ReadBytes({{length}});
                                    {{D(3)}}Array.Copy(buffer, result.{{propertyName}}, {{length}});
                                    """);
                                break;
                            }
                        }

                        sb.AppendLine($$"""
                        {{D(3)}}for (var i = 0; i < {{length}}; i++)
                        {{D(3)}}{
                        {{D(3)}}    result.{{propertyName}}[i] = {{readMethod}};
                        {{D(3)}}}
                        """);
                    }
                    break;
                case nameof(PacketSchemaBuilder.BeginChecksumRegion):
                case nameof(PacketSchemaBuilder.BeginPointChecksum):
                    sb.AppendLine($"{D(3)}reader.BeginChecksumRegion();");
                    break;
                case nameof(PacketSchemaBuilder.EndChecksumRegion):
                case nameof(PacketSchemaBuilder.EndPointChecksum):
                    sb.AppendLine($"{D(3)}reader.EndChecksumRegion();");
                    break;
                case nameof(PacketSchemaBuilder.@checksum):
                case nameof(PacketSchemaBuilder.@checkum):
                    {
                        if (item.SchemaName == nameof(PacketSchemaBuilder.@checksum))
                        {
                            var propertyName = GetPropertyName(item);
                            var checksumType = item.Arguments.Length > 1 ? item.Arguments[1].Expression.ToString() : "GenPack.ChecksumType.Sum8";
                            
                            // Ensure ChecksumType has GenPack namespace prefix
                            if (!checksumType.Contains("GenPack.") && checksumType.Contains("ChecksumType."))
                            {
                                checksumType = $"GenPack.{checksumType}";
                            }
                            
                            sb.AppendLine($"{D(3)}result.{propertyName} = ({GetChecksumPropertyType(checksumType)})reader.ReadAndValidateChecksum({checksumType});");
                        }
                        else
                        {
                            // Legacy @checkum method - just validate without storing
                            var checksumType = item.Arguments.Length > 0 ? item.Arguments[0].Expression.ToString() : "GenPack.ChecksumType.Sum8";
                            
                            // Ensure ChecksumType has GenPack namespace prefix
                            if (!checksumType.Contains("GenPack.") && checksumType.Contains("ChecksumType."))
                            {
                                checksumType = $"GenPack.{checksumType}";
                            }
                            
                            sb.AppendLine($"{D(3)}reader.ValidateChecksum({checksumType});");
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        sb.AppendLine($$"""{{D(3)}}return result;""");
        sb.AppendLine($$"""{{D(2)}}}""");
    }

    private static string GetReadMethod(string schemaType) => schemaType switch
    {
        nameof(PacketSchemaBuilder.@byte) => "reader.ReadByte()",
        nameof(PacketSchemaBuilder.@sbyte) => "reader.ReadSByte()",
        nameof(PacketSchemaBuilder.@short) => "reader.ReadInt16()",
        nameof(PacketSchemaBuilder.@ushort) => "reader.ReadUInt16()",
        nameof(PacketSchemaBuilder.@int) => "reader.ReadInt32()",
        nameof(PacketSchemaBuilder.@uint) => "reader.ReadUInt32()",
        nameof(PacketSchemaBuilder.@long) => "reader.ReadInt64()",
        nameof(PacketSchemaBuilder.@ulong) => "reader.ReadUInt64()",
        nameof(PacketSchemaBuilder.@float) => "reader.ReadSingle()",
        nameof(PacketSchemaBuilder.@double) => "reader.ReadDouble()",
        nameof(PacketSchemaBuilder.@string) => "reader.ReadString()",
        _ => $"reader.Read{PacketSchema.GetClsType(schemaType)}()"
    };

    private static string GetPropertyName(SchemaItem item) => item.Arguments[0].Expression.ToString()[1..^1];
    
    private static void AddProperties(StringBuilder sb, IReadOnlyList<SchemaItem> items)
    {
        foreach (var item in items)
        {
            switch (item.SchemaName)
            {
                case nameof(PacketSchemaBuilder.@byte):
                case nameof(PacketSchemaBuilder.@sbyte):
                case nameof(PacketSchemaBuilder.@short):
                case nameof(PacketSchemaBuilder.@ushort):
                case nameof(PacketSchemaBuilder.@int):
                case nameof(PacketSchemaBuilder.@uint):
                case nameof(PacketSchemaBuilder.@long):
                case nameof(PacketSchemaBuilder.@ulong):
                case nameof(PacketSchemaBuilder.@float):
                case nameof(PacketSchemaBuilder.@double):
                case nameof(PacketSchemaBuilder.@string):
                    AddProperty(sb, item);
                    break;
                case nameof(PacketSchemaBuilder.@object):
                    AddObjectProperty(sb, item);
                    break;
                case nameof(PacketSchemaBuilder.@list):
                    AddListProperty(sb, item);
                    break;
                case nameof(PacketSchemaBuilder.@dict):
                    AddDictProperty(sb, item);
                    break;
                case nameof(PacketSchemaBuilder.@array):
                    AddArrayProperty(sb, item);
                    break;
                case nameof(PacketSchemaBuilder.@checksum):
                    AddChecksumProperty(sb, item);
                    break;
                case nameof(PacketSchemaBuilder.BeginChecksumRegion):
                case nameof(PacketSchemaBuilder.EndChecksumRegion):
                case nameof(PacketSchemaBuilder.BeginPointChecksum):
                case nameof(PacketSchemaBuilder.EndPointChecksum):
                case nameof(PacketSchemaBuilder.@checkum):
                    // These don't generate properties
                    break;
                default:
                    break;
            }
        }
    }

    private static void AddProperty(StringBuilder sb, SchemaItem item)
    {
        var @type = item.SchemaType;
        var defaultSet = "";
        if (type is "string")
            defaultSet = " = string.Empty;";

        var propertyName = item.Arguments[0].Expression.ToString()[1..^1];

        if (item.Arguments.Length > 1)
        {
            var desc = item.Arguments[1].Expression.ToString()[1..^1];
            if (string.IsNullOrWhiteSpace(desc) is false)
            {
                sb.AppendLine($$"""
                            {{D(2)}}/// <summary>
                            {{D(2)}}/// {{desc}}
                            {{D(2)}}/// </summary>
                            """);
            }
        }

        sb.AppendLine($$"""
            {{D(2)}}public {{@type}} {{propertyName}} { get; set; }{{defaultSet}}
            """);
    }

    private static void AddObjectProperty(StringBuilder sb, SchemaItem item)
    {
        var @type = item.SchemaType;
        var propertyName = item.Arguments[0].Expression.ToString()[1..^1];

        if (item.Arguments.Length > 1)
        {
            var desc = item.Arguments[1].Expression.ToString()[1..^1];
            if (string.IsNullOrWhiteSpace(desc) is false)
            {
                sb.AppendLine($$"""
                            {{D(2)}}/// <summary>
                            {{D(2)}}/// {{desc}}
                            {{D(2)}}/// </summary>
                            """);
            }
        }

        sb.AppendLine($$"""
            {{D(2)}}public {{@type}} {{propertyName}} { get; set; }
            """);
    }

    private static void AddListProperty(StringBuilder sb, SchemaItem item)
    {
        var @type = item.SchemaType;
        var defaultSet = $" = new List<{@type}>();";
        var propertyName = item.Arguments[0].Expression.ToString()[1..^1];

        if (item.Arguments.Length > 1)
        {
            var desc = item.Arguments[1].Expression.ToString()[1..^1];
            if (string.IsNullOrWhiteSpace(desc) is false)
            {
                sb.AppendLine($$"""
                            {{D(2)}}/// <summary>
                            {{D(2)}}/// {{desc}}
                            {{D(2)}}/// </summary>
                            """);
            }
        }

        sb.AppendLine($$"""
            {{D(2)}}public System.Collections.Generic.IList<{{@type}}> {{propertyName}} { get; }{{defaultSet}}
            """);
    }

    private static void AddDictProperty(StringBuilder sb, SchemaItem item)
    {
        var @type = item.SchemaType;
        var defaultSet = $" = new Dictionary<string, {@type}>();";
        var propertyName = item.Arguments[0].Expression.ToString()[1..^1];

        if (item.Arguments.Length > 1)
        {
            var desc = item.Arguments[1].Expression.ToString()[1..^1];
            if (string.IsNullOrWhiteSpace(desc) is false)
            {
                sb.AppendLine($$"""
                            {{D(2)}}/// <summary>
                            {{D(2)}}/// {{desc}}
                            {{D(2)}}/// </summary>
                            """);
            }
        }

        sb.AppendLine($$"""
            {{D(2)}}public System.Collections.Generic.IDictionary<string, {{@type}}> {{propertyName}} { get; }{{defaultSet}}
            """);
    }

    private static void AddArrayProperty(StringBuilder sb, SchemaItem item)
    {
        var @type = item.SchemaType;
        var length = 0;
        if (item.Arguments.Length > 1)
            length = int.Parse(item.Arguments[1].Expression.ToString());
        var defaultSet = $" = new {@type}[{length}];";
        var propertyName = item.Arguments[0].Expression.ToString()[1..^1];

        if (item.Arguments.Length > 2)
        {
            var desc = item.Arguments[2].Expression.ToString()[1..^1];
            if (string.IsNullOrWhiteSpace(desc) is false)
            {
                sb.AppendLine($$"""
                            {{D(2)}}/// <summary>
                            {{D(2)}}/// {{desc}}
                            {{D(2)}}/// </summary>
                            """);
            }
        }

        sb.AppendLine($$"""
            {{D(2)}}public {{@type}}[] {{propertyName}} { get; }{{defaultSet}}
            """);
    }

    private static void AddChecksumProperty(StringBuilder sb, SchemaItem item)
    {
        var propertyName = item.Arguments[0].Expression.ToString()[1..^1];
        var checksumType = item.Arguments.Length > 1 ? item.Arguments[1].Expression.ToString() : "ChecksumType.Sum8";
        
        // Determine the property type based on checksum type
        var propertyType = GetChecksumPropertyType(checksumType);

        if (item.Arguments.Length > 2)
        {
            var desc = item.Arguments[2].Expression.ToString()[1..^1];
            if (string.IsNullOrWhiteSpace(desc) is false)
            {
                sb.AppendLine($$"""
                            {{D(2)}}/// <summary>
                            {{D(2)}}/// {{desc}}
                            {{D(2)}}/// </summary>
                            """);
            }
        }

        sb.AppendLine($$"""
            {{D(2)}}public {{propertyType}} {{propertyName}} { get; set; }
            """);
    }

    private static string GetChecksumPropertyType(string checksumType)
    {
        // Extract the enum value from the checksum type expression
        var enumValue = checksumType.Contains(".") ? checksumType.Split('.').Last() : checksumType;
        
        return enumValue switch
        {
            "Sum8" => "byte",
            "XorSum" => "byte", 
            "Lrc8" => "byte",
            "Sum16" => "ushort",
            "Fletcher16" => "ushort",
            "Crc16" => "ushort",
            "Crc16Ccitt" => "ushort",
            "Crc32" => "uint",
            "Crc32C" => "uint",
            _ => "byte[]" // Default to byte array for unknown types
        };
    }

    /// <summary>
    /// class / enum / struct가 선언된 네임스페이스 반환
    /// </summary>
    /// <param name="syntax"></param>
    /// <returns></returns>
    private static string GetNamespace(BaseTypeDeclarationSyntax syntax)
    {
        var nameSpace = string.Empty;

        var potentialNamespaceParent = syntax.Parent;

        // 네임스페이스에 도달할 때까지 중첩된 클래스 등에서 "밖으로" 계속 이동
        // 또는 부모가 없을 때까지
        while (potentialNamespaceParent != null &&
                potentialNamespaceParent is not NamespaceDeclarationSyntax
                && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
        {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

        // 더 이상 네임스페이스 선언이 없을 때까지 반복하여 최종 네임스페이스를 빌드
        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            // 네임스페이스가 있으므로 유형으로 사용
            nameSpace = namespaceParent.Name.ToString();

            // 중첩된 네임스페이스가 없을 때까지 네임스페이스 선언을 "밖으로" 계속 이동
            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }

                // 외부 네임스페이스를 최종 네임스페이스에 접두사로 추가
                nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                namespaceParent = parent;
            }
        }

        // 최종 네임스페이스 반환
        return nameSpace;
    }

    private static string D(int depths) => depths switch
    {
        0 => "",
        1 => "    ",
        2 => "        ",
        3 => "            ",
        4 => "                ",
        5 => "                    ",
        _ => throw new ArgumentOutOfRangeException(nameof(depths))
    };
}

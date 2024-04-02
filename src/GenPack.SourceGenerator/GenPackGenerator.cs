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

        static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;
        static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax classDeclaration)
            {
                foreach (var attributeList in classDeclaration.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                            continue;

                        var attributeContainingType = attributeSymbol.ContainingType;
                        var fullname = attributeContainingType.ToDisplayString();
                        if (fullname is $"{nameof(GenPack)}.{nameof(GenPackableAttribute)}")
                            return classDeclaration;
                    }
                }
            }

            return null;
        }
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
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
            var schemaItems = ParseSchema(compilation, packetSchemaSyntax.DescendantNodes());

            schemaBuilder.AppendLine("#pragma warning disable CS0219");
            
            if (string.IsNullOrEmpty(@namespace) is false)
                schemaBuilder.AppendLine($$"""
                    namespace {{@namespace}}
                    {
                    """);

            schemaBuilder.AppendLine($$"""
                {{D(1)}}public partial class {{classSymbol.Name}} : GenPack.IGenPackable
                {{D(1)}}{
                """);

            AddProperties(schemaBuilder, schemaItems);
            AddMethods(schemaBuilder, string.IsNullOrEmpty(@namespace) ? classSymbol.Name : $"{@namespace}.{classSymbol.Name}", schemaItems);

            schemaBuilder.AppendLine($$"""{{D(1)}}}""");

            if (string.IsNullOrEmpty(@namespace) is false)
                schemaBuilder.AppendLine("}");

            context.AddSource($"{classSymbol.Name}Schema.g.cs", SourceText.From(schemaBuilder.ToString(), Encoding.UTF8));
        }
    }

    private static IReadOnlyList<SchemaItem> ParseSchema(Compilation compilation, IEnumerable<SyntaxNode> nodes)
    {
        List<SchemaItem> result = [];

        var invocationNodes = nodes.OfType<InvocationExpressionSyntax>().Reverse().ToArray();
        if (invocationNodes.Length is 0)
            return result;
        if (invocationNodes.First().ChildNodes().First().TryGetInferredMemberName() is not nameof(PacketSchemaBuilder.Create))
            return result;

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
                schemaType = compilation.GetSymbolsWithName(schemaType).FirstOrDefault()?.ToDisplayString() ?? schemaType;
            }
            var arguments = childNodes[1].ChildNodes().OfType<ArgumentSyntax>().ToArray();

            if (schemaName is nameof(PacketSchemaBuilder.Build) is true)
                break;

            result.Add((schemaName, schemaType, PacketSchema.IsDefaultType(schemaType), arguments));
        }

        return result;
    }

    private static void AddMethods(StringBuilder sb, string className, IReadOnlyList<SchemaItem> items)
    {
        if (items.Count is 0)
            return;

        var createMethod = items.First();
        var methods = items.Skip(1);

        addToPackMethod();
        addFromPackMethod(className);

        // ------

        void addToPackMethod()
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
                {{D(2)}}    System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);
                """);


            foreach (var item in methods)
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
                    case nameof(PacketSchemaBuilder.single):
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
                            sb.AppendLine($"{D(3)}{propertyName}.ToPacket(stream);");
                        }
                        break;
                    case nameof(PacketSchemaBuilder.@list):
                        {
                            var propertyName = GetPropertyName(item);
                            var writeMethod = item.IsDefaultType is true ? "writer.Write(item)" : "item.ToPacket(stream)";

                            sb.AppendLine($$"""
                            {{D(3)}}writer.Write({{propertyName}}.Count);
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
                            var writeMethod = item.IsDefaultType is true ? "writer.Write(item.Value)" : "item.Value.ToPacket(stream)";

                            sb.AppendLine($$"""
                            {{D(3)}}writer.Write({{propertyName}}.Count);
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
                            var writeMethod = item.IsDefaultType is true ? "writer.Write(item)" : "item.ToPacket(stream)";
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
                    case nameof(PacketSchemaBuilder.BeginPointChecksum):
                        break;
                    case nameof(PacketSchemaBuilder.EndPointChecksum):
                        break;
                    case nameof(PacketSchemaBuilder.@checkum):
                        break;
                    default:
                        break;
                }
            }

            sb.AppendLine($$"""{{D(2)}}}""");
        }

        void addFromPackMethod(string className)
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
                {{D(2)}}    {{className}} result = new {{className}}();
                {{D(2)}}    System.IO.BinaryReader reader = new System.IO.BinaryReader(stream);
                {{D(2)}}    int size = 0;
                {{D(2)}}    byte[] buffer = null;
                """);


            foreach (var item in methods)
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
                    case nameof(PacketSchemaBuilder.single):
                    case nameof(PacketSchemaBuilder.@double):
                    case nameof(PacketSchemaBuilder.@string):
                        {
                            sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = reader.Read{PacketSchema.GetClsType(item.SchemaType)}();");
                        }
                        break;
                    case nameof(PacketSchemaBuilder.@object):
                        {
                            sb.AppendLine($"{D(3)}result.{GetPropertyName(item)} = {item.SchemaType}.FromPacket(stream);");
                        }
                        break;
                    case nameof(PacketSchemaBuilder.@list):
                        {
                            var propertyName = GetPropertyName(item);
                            var readMethod = item.IsDefaultType is true ? $"reader.Read{PacketSchema.GetClsType(item.SchemaType)}()" : $"{item.SchemaType}.FromPacket(stream)";

                            sb.AppendLine($$"""
                            {{D(3)}}size = reader.ReadInt32();
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
                            var readMethod = item.IsDefaultType is true ? $"reader.Read{PacketSchema.GetClsType(item.SchemaType)}()" : $"{item.SchemaType}.FromPacket(stream)";

                            sb.AppendLine($$"""
                            {{D(3)}}size = reader.ReadInt32();
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
                            var readMethod = item.IsDefaultType is true ? $"reader.Read{PacketSchema.GetClsType(item.SchemaType)}()" : $"{item.SchemaType}.FromPacket(stream)";
                            var length = int.Parse(item.Arguments[1].Expression.ToString());

                            if (item.IsDefaultType is true)
                            {
                                // TODO: Read(Span<byte>)는 .NET Standard 2.0에서 지원하지 않으므로 차후 TargetFramework을 확인해서 적용할 수 있도록 수정해야 함
                                //if (item.SchemaType is nameof(PacketSchemaBuilder.@byte))
                                //{
                                //    sb.AppendLine($"{D(3)}reader.Read(result.{propertyName}.AsSpan());");
                                //    break;
                                //}

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
                            {{D(3)}}size = reader.ReadInt32();
                            {{D(3)}}for (var i = 0; i < size; i++)
                            {{D(3)}}{
                            {{D(3)}}    result.{{propertyName}}[i] = {{readMethod}};
                            {{D(3)}}}
                            """);
                        }
                        break;
                    case nameof(PacketSchemaBuilder.BeginPointChecksum):
                        break;
                    case nameof(PacketSchemaBuilder.EndPointChecksum):
                        break;
                    case nameof(PacketSchemaBuilder.@checkum):
                        break;
                    default:
                        break;
                }
            }

            sb.AppendLine($$"""{{D(3)}}return result;""");
            sb.AppendLine($$"""{{D(2)}}}""");
        }
    }

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
                case nameof(PacketSchemaBuilder.single):
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
                case nameof(PacketSchemaBuilder.BeginPointChecksum):
                    break;
                case nameof(PacketSchemaBuilder.EndPointChecksum):
                    break;
                case nameof(PacketSchemaBuilder.@checkum):
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

    private static string D(int depth) => new(' ', depth * 4);
}

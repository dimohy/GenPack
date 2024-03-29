using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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

            var packetSchemaSyntax = packetSchema.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax ;
            if (packetSchemaSyntax is null)
                continue;

            var schemaBuilder = new StringBuilder();
            var methodBuilder = new StringBuilder();

            if (string.IsNullOrEmpty(@namespace) is false)
                schemaBuilder.AppendLine($$"""namespace {{@namespace}} {""");

            schemaBuilder.AppendLine($$"""
                    public partial class {{classSymbol.Name}} : GenPack.IGenPackable
                    {
                """);

            ProcessPacketSchemaSyntax(schemaBuilder, methodBuilder, packetSchemaSyntax.DescendantNodes());

            schemaBuilder.AppendLine("    }");

            if (string.IsNullOrEmpty(@namespace) is false)
                schemaBuilder.AppendLine("}");

            context.AddSource($"{classSymbol.Name}Schema.g.cs", SourceText.From(schemaBuilder.ToString(), Encoding.UTF8));
        }
    }

    private static void ProcessPacketSchemaSyntax(StringBuilder sb, StringBuilder mb, IEnumerable<SyntaxNode> nodes)
    {
        var invocationNodes = nodes.OfType<InvocationExpressionSyntax>().Reverse().ToArray();
        if (invocationNodes.Length is 0)
            return;
        if (invocationNodes.First().ChildNodes().First().TryGetInferredMemberName() is not nameof(PacketSchemaBuilder.Create))
            return;

        foreach (var node in invocationNodes)
        {
            var childNodes = node.ChildNodes().ToArray();
            if (childNodes.Length < 2)
                continue;

            var methodName = childNodes[0].TryGetInferredMemberName();
            var arguments = childNodes[1].ChildNodes().OfType<ArgumentSyntax>().ToArray();

            switch (methodName)
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
                    AddProperty(sb, mb, methodName, arguments);
                    break;
                case nameof(PacketSchemaBuilder.BeginPointChecksum):
                    break;
                case nameof(PacketSchemaBuilder.EndPointChecksum):
                    break;
                case nameof(PacketSchemaBuilder.@checkum):
                    break;
                case nameof(PacketSchemaBuilder.Build):
                    break;
                default:
                    break;
            }
        }
    }

    private static void AddProperty(StringBuilder sb, StringBuilder mb, string methodName, ArgumentSyntax[] arguments)
    {
        if (arguments.Length > 1)
        {
            var desc = arguments[1].Expression.ToString().Replace("\"", "");
            if (string.IsNullOrWhiteSpace(desc) is false)
            {
                sb.AppendLine($$"""
                                    /// <summary>
                                    /// {{desc}}
                                    /// </summary>
                            """);
            }
        }

        sb.AppendLine($$"""        public {{methodName}} {{arguments[0].Expression.ToString().Replace("\"", "")}} { get; set; }""");
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
}

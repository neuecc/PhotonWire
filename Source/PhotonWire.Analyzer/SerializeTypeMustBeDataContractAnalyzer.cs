using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PhotonWire.Analyzer
{
    internal class ContextReporter
    {
        readonly List<Diagnostic> diagnostics = new List<Diagnostic>();
        readonly SyntaxNodeAnalysisContext context;
        bool hasContext;

        public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

        public ContextReporter()
        {

        }

        public ContextReporter(SyntaxNodeAnalysisContext context)
        {
            this.context = context;
            this.hasContext = true;
        }

        public SemanticModel SemanticModel => (hasContext) ? context.SemanticModel : null;

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            diagnostics.Add(diagnostic);
            if (hasContext)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
    }


    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SerializeTypeMustBeDataContractAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SerializeTypeMustBeDataContract";
        internal const string Title = "Serialize type must be DataContract.";
        internal const string MessageFormat = "Serialize Type:{0} must be DataContract and all properties must have DataMemberAttribute with Order.";
        internal const string Category = "Usage";

        internal static DiagnosticDescriptor SerializeTypeMustBeDataContract = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Serialize type must be DataContract and all properties must have DataMemberAttribute with Order.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(SerializeTypeMustBeDataContract); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var model = context.SemanticModel;

            var classDeclaration = context.Node as ClassDeclarationSyntax;
            if (classDeclaration == null) return;

            var declaredSymbol = model.GetDeclaredSymbol(classDeclaration);
            if (declaredSymbol == null) return;
            if (declaredSymbol.IsAbstract) return;

            var reporter = new ContextReporter(context);
            var hub = declaredSymbol.FindBaseTargetType("PhotonWire.Server.Hub<T>");
            if (hub != null)
            {
                // Verify HubClient
                var clientType = hub.TypeArguments[0];
                VerifyTypeMethods(reporter, clientType);
            }
            else
            {
                // check ServerHub, ReceiveServerHub
                hub = declaredSymbol.FindBaseTargetType("PhotonWire.Server.ServerToServer.ServerHub");
                if (hub == null)
                {
                    hub = declaredSymbol.FindBaseTargetType("PhotonWire.Server.ServerToServer.ReceiveServerHub");
                    if (hub == null)
                    {
                        return; // is not hub method, end analyze
                    }
                }
            }

            VerifyTypeMethods(reporter, declaredSymbol);
        }

        internal static void VerifyTypeMethods(ContextReporter context, ITypeSymbol type)
        {
            var methods = type.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(x => x.MethodKind == MethodKind.Ordinary)
                .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                .Where(x => !x.IsStatic)
                .ToArray();

            var alreadyAnalyzed = new HashSet<ITypeSymbol>();
            foreach (var method in methods)
            {
                var methodDecl = method.DeclaringSyntaxReferences[0].GetSyntax() as MethodDeclarationSyntax;
                if (methodDecl == null) return;

                VerifyType(context, methodDecl.ReturnType.GetLocation(), method.ReturnType, alreadyAnalyzed);
                foreach (var item in method.Parameters.Zip(methodDecl.ParameterList.Parameters, (symbol, syntax) => new { symbol, syntax }))
                {
                    VerifyType(context, item.syntax.Type.GetLocation(), item.symbol.Type, alreadyAnalyzed);
                }
            }
        }

        internal static void VerifyType(ContextReporter context, Location reportLocation, ITypeSymbol type, HashSet<ITypeSymbol> alreadyAnalyzed)
        {
            if (type.TypeKind == TypeKind.Array)
            {
                var array = type as IArrayTypeSymbol;
                VerifyType(context, reportLocation, array.ElementType, alreadyAnalyzed);
                return;
            }

            var namedTypeSymbol = (type as INamedTypeSymbol);
            if (namedTypeSymbol != null && namedTypeSymbol.IsGenericType)
            {
                foreach (var item in namedTypeSymbol.TypeArguments)
                {
                    VerifyType(context, reportLocation, item, alreadyAnalyzed);
                }
                return;
            }

            if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
            {
                if (type.Locations[0].Kind == LocationKind.MetadataFile)
                {
                    return;
                }

                if (!alreadyAnalyzed.Add(type)) return;

                var typeLocation = type.Locations[0];
                var members = type.GetMembers().Where(x => x is IFieldSymbol || x is IPropertySymbol)
                    .Where(x=> x.CanBeReferencedByName)
                    .ToArray();
                if (members.Length != 0)
                {
                    var hasDiagnostic = false;
                    foreach (var member in members)
                    {
                        if (!hasDiagnostic)
                        {
                            hasDiagnostic = VerifyMember(context, reportLocation, member, typeLocation);
                        }

                        VerifyType(context, reportLocation, (member as IFieldSymbol)?.Type ?? (member as IPropertySymbol)?.Type ?? null, alreadyAnalyzed);
                    }

                    if (!hasDiagnostic)
                    {
                        var dataContract = type.GetAttributes().FirstOrDefault(y => y.AttributeClass.ToString() == "System.Runtime.Serialization.DataContractAttribute");
                        if (dataContract == null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(SerializeTypeMustBeDataContract, reportLocation, new[] { typeLocation }, type.ToString()));
                        }
                    }
                }
            }
        }

        static bool VerifyMember(ContextReporter context, Location reportLocation, ISymbol member, Location typeLocation)
        {
            if (member.Locations[0].Kind == LocationKind.MetadataFile)
            {
                return false; // in metadata, I don't know...
            }

            if (member is IPropertySymbol)
            {
                // get only property
                var prop = member as IPropertySymbol;
                if (prop.SetMethod == null)
                {
                    return false;
                }
            }

            var attr = member.GetAttributes();
            if (attr.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(SerializeTypeMustBeDataContract, reportLocation, new[] { typeLocation }, member.ContainingType.ToString()));
                return true;
            }

            if (attr.Any(x => x.AttributeClass.ToString() == typeof(System.Runtime.Serialization.IgnoreDataMemberAttribute).FullName))
            {
                return false;
            }

            var dataMemberAttr = attr.FirstOrDefault(y => y.AttributeClass.ToString() == typeof(System.Runtime.Serialization.DataMemberAttribute).FullName);
            if (dataMemberAttr == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(SerializeTypeMustBeDataContract, reportLocation, new[] { typeLocation }, member.ContainingType.ToString()));
                return true;
            }

            var hasOrder = dataMemberAttr.NamedArguments.Any(x => x.Key == "Order");
            if (!hasOrder)
            {
                context.ReportDiagnostic(Diagnostic.Create(SerializeTypeMustBeDataContract, reportLocation, new[] { typeLocation }, member.ContainingType.ToString()));
                return true;
            }

            return false;
        }
    }
}
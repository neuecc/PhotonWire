using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PhotonWire.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PhotonWireHubAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PhotonWireHubAnalyzer";

        private const string Title = "PhotonWireAnalyzer";
        private const string Category = "Usage";

        private static DiagnosticDescriptor HubNeedsHubAttribute = new DiagnosticDescriptor(DiagnosticId, Title, "Hub must needs HubAttribute.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Hub must needs HubAttribute.");
        private static DiagnosticDescriptor MethodNeedsOperationAttribute = new DiagnosticDescriptor(DiagnosticId, Title, "Method must needs OperationAttribute.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Method must needs OperationAttribute.");
        private static DiagnosticDescriptor OperationIdMustBeUniqueAttribute = new DiagnosticDescriptor(DiagnosticId, Title, "Conflicts OperationId:{0}, operationId must be Unique per Hub.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "OperationId must be Unique per Hub.");
        private static DiagnosticDescriptor ClientVerify = new DiagnosticDescriptor(DiagnosticId, Title, "{0}.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Client must follow several rules.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(HubNeedsHubAttribute, MethodNeedsOperationAttribute, OperationIdMustBeUniqueAttribute, ClientVerify);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var model = context.SemanticModel;

            var classDeclaration = context.Node as ClassDeclarationSyntax;
            if (classDeclaration == null) return;

            var declaredSymbol = model.GetDeclaredSymbol(classDeclaration);
            if (declaredSymbol == null) return;
            if (declaredSymbol.IsAbstract) return;

            var hub = declaredSymbol.FindBaseTargetType("PhotonWire.Server.Hub<T>");
            if (hub == null) return;

            var hubAttribute = declaredSymbol.GetAttributes().FindAttribute("PhotonWire.Server.HubAttribute");
            if (hubAttribute == null)
            {
                // if type == hub, needs attribute
                context.ReportDiagnostic(Diagnostic.Create(HubNeedsHubAttribute, classDeclaration.GetLocation()));
                return;
            }

            var methods = declaredSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(x => x.MethodKind == MethodKind.Ordinary)
                .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                .Where(x => !x.IsStatic)
                .ToArray();

            // all member needs OperationAttribute
            var attrs = methods.Select(x => x.GetAttributes().FindAttribute("PhotonWire.Server.OperationAttribute")).ToArray();

            var set = new HashSet<byte>();
            for (int i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                var a = attrs[i];

                if (a == null)
                {
                    // attr needs OperationAttribute;
                    context.ReportDiagnostic(Diagnostic.Create(MethodNeedsOperationAttribute, m.Locations[0]));
                    continue;
                }

                if (a.ConstructorArguments.Length != 1) continue;
                var id = (byte)a.ConstructorArguments[0].Value;
                if (!set.Add(id))
                {
                    // Report Diagnostics
                    var location = Location.Create(a.ApplicationSyntaxReference.SyntaxTree, a.ApplicationSyntaxReference.Span);
                    context.ReportDiagnostic(Diagnostic.Create(OperationIdMustBeUniqueAttribute, location, id));
                }
            }

            var clientType = hub.TypeArguments[0];
            VerifyClient(context, declaredSymbol, clientType);
        }

        static void VerifyClient(SyntaxNodeAnalysisContext context, ITypeSymbol hubType, ITypeSymbol clientType)
        {
            if (clientType.TypeKind != TypeKind.Interface)
            {
                context.ReportDiagnostic(Diagnostic.Create(ClientVerify, hubType.Locations[0], "Hub<T>'s T must be interface."));
                return;
            }

            var set = new HashSet<byte>();
            foreach (var item in clientType.GetMembers())
            {
                if (item.IsImplicitlyDeclared) continue;

                if (item.Kind != SymbolKind.Method)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClientVerify, item.Locations[0], "Interface's member must only be method."));
                    continue;
                }

                var method = item as IMethodSymbol;

                if (!method.ReturnsVoid)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClientVerify, item.Locations[0], "Interface's member must return void."));
                    continue;
                }

                // Verify Parameter
                foreach (var p in method.Parameters)
                {
                    if (p.RefKind != RefKind.None)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ClientVerify, p.Locations[0], "Interface's parameter must not take out or ref."));
                        continue;
                    }
                    var disp = p.Type.OriginalDefinition?.ToDisplayString();
                    if (disp == "System.Threading.Tasks.Task" || disp == "System.Threading.Tasks.Task<TResult>")
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ClientVerify, p.Locations[0], "Interface's parameter must not take Task."));
                        continue;
                    }
                }

                // Verify Attribute

                var attr = method.GetAttributes().FindAttribute("PhotonWire.Server.OperationAttribute");
                if (attr == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClientVerify, item.Locations[0], "Interface's method must put OperationAttribute."));
                    continue;
                }

                if (attr.ConstructorArguments.Length != 1) continue;

                var id = (byte)attr.ConstructorArguments[0].Value;
                if (!set.Add(id))
                {
                    var location = Location.Create(attr.ApplicationSyntaxReference.SyntaxTree, attr.ApplicationSyntaxReference.Span);
                    context.ReportDiagnostic(Diagnostic.Create(ClientVerify, location, $"Conflicts OperationId:{id}, operationId must be Unique per Interface."));
                    continue;
                }
            }
        }
    }
}
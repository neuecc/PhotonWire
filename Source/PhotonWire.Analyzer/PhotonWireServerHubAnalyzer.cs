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
    public class PhotonWireServerHubAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PhotonWireServerHubAnalyzer";
        public const string DisableWarningDiagnosticId = "PhotonWireServerHubDisableWarningAnalyzer";
        private const string Title = "PhotonWireServerHubAnalyzer";
        private const string Category = "Usage";

        private static DiagnosticDescriptor HubNeedsHubAttribute = new DiagnosticDescriptor(DiagnosticId, Title, "Hub must needs HubAttribute.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Hub must needs HubAttribute.");
        private static DiagnosticDescriptor MethodNeedsOperationAttribute = new DiagnosticDescriptor(DiagnosticId, Title, "Method must needs OperationAttribute.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Method must needs OperationAttribute.");
        private static DiagnosticDescriptor OperationIdMustBeUniqueAttribute = new DiagnosticDescriptor(DiagnosticId, Title, "Conflicts OperationId:{0}, operationId must be Unique per Hub.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "OperationId must be Unique per Hub.");
        private static DiagnosticDescriptor MethodVerify = new DiagnosticDescriptor(DiagnosticId, Title, "{0}.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Method must follow several rules.");
        private static DiagnosticDescriptor DisableTaskWarning = new DiagnosticDescriptor(DisableWarningDiagnosticId, Title, "Append disable/restore task warning.", Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: "DisableTaskWarning for use async.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(HubNeedsHubAttribute, MethodNeedsOperationAttribute, OperationIdMustBeUniqueAttribute, MethodVerify, DisableTaskWarning);

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

            var hub = declaredSymbol.FindBaseTargetType("PhotonWire.Server.ServerToServer.ServerHub");
            if (hub == null)
            {
                hub = declaredSymbol.FindBaseTargetType("PhotonWire.Server.ServerToServer.ReceiveServerHub");
                if (hub == null)
                {
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(DisableTaskWarning, classDeclaration.GetLocation()));

            var hubAttribute = declaredSymbol.GetAttributes().FindAttribute("PhotonWire.Server.HubAttribute");
            if (hubAttribute == null)
            {
                // if type == hub, needs attribute
                context.ReportDiagnostic(Diagnostic.Create(HubNeedsHubAttribute, classDeclaration.GetLocation()));
                return;
            }

            var methods = declaredSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(x => x.MethodKind == MethodKind.Ordinary && !x.IsImplicitlyDeclared)
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

                VerifyMethod(context, m);
            }
        }

        static void VerifyMethod(SyntaxNodeAnalysisContext context, ISymbol methodSymbol)
        {
            var method = methodSymbol as IMethodSymbol;

            var returnType = method.ReturnType?.OriginalDefinition?.ToDisplayString();
            if (!(returnType == "System.Threading.Tasks.Task" || returnType == "System.Threading.Tasks.Task<TResult>"))
            {
                context.ReportDiagnostic(Diagnostic.Create(MethodVerify, method.Locations[0], "ServerHub's method must return Task or Task<T>."));
                return;
            }

            if (!method.IsVirtual)
            {
                context.ReportDiagnostic(Diagnostic.Create(MethodVerify, method.Locations[0], "ServerHub's method must be virtual."));
                return;
            }

            // Verify Parameter
            foreach (var p in method.Parameters)
            {
                if (p.RefKind != RefKind.None)
                {
                    context.ReportDiagnostic(Diagnostic.Create(MethodVerify, p.Locations[0], "Interface's parameter must not take out or ref."));
                    continue;
                }

                var disp = p.Type.OriginalDefinition?.ToDisplayString();
                if (disp == "System.Threading.Tasks.Task" || disp == "System.Threading.Tasks.Task<TResult>")
                {
                    context.ReportDiagnostic(Diagnostic.Create(MethodVerify, p.Locations[0], "Interface's parameter must not take Task."));
                    continue;
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace PhotonWire.Analyzer
{
    // #pragma warning disable CS1998
    // #pragma warning restore CS1998

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisableTaskWarningCodeFixProvider)), Shared]
    public class DisableTaskWarningCodeFixProvider : CodeFixProvider
    {
        static readonly SyntaxTrivia DisableTaskWarning = CSharpSyntaxTree.ParseText("#pragma warning disable CS1998").GetRoot().FindTrivia(0);
        static readonly SyntaxTrivia RestoreTaskWarning = CSharpSyntaxTree.ParseText("#pragma warning restore CS1998").GetRoot().FindTrivia(0);

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(PhotonWireServerHubAnalyzer.DisableWarningDiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            var model = await context.Document.GetSemanticModelAsync();
            var diagnostic = context.Diagnostics.First();

            if (root.ToFullString().Contains("#pragma warning disable"))
            {
                return;
            }

            context.RegisterCodeFix(CodeAction.Create("Disable 'async method lacks await' warning", new Func<CancellationToken, Task<Document>>(c =>
            {
                var newRoot = root.WithLeadingTrivia(DisableTaskWarning, SyntaxFactory.CarriageReturnLineFeed)
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed, RestoreTaskWarning);

                var newDocument = context.Document.WithSyntaxRoot(newRoot);

                return Task.FromResult(newDocument);
            }), nameof(DisableTaskWarningCodeFixProvider)), diagnostic);
        }
    }
}
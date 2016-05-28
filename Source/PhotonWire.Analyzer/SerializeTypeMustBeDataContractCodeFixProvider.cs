using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System;

namespace PhotonWire.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SerializeTypeMustBeDataContractCodeFixProvider)), Shared]
    public class SerializeTypeMustBeDataContractCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SerializeTypeMustBeDataContractAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var targetNode = root.FindNode(context.Span);
            var targetType = model.GetTypeInfo(targetNode).Type;
            if (targetType == null) return;

            if (targetType.GetMembers().Length == 0) return;

            var reporter = new ContextReporter();
            SerializeTypeMustBeDataContractAnalyzer.VerifyType(reporter, null, targetType, new System.Collections.Generic.HashSet<ITypeSymbol>());

            foreach (var item in reporter.Diagnostics)
            {
                var location = item.AdditionalLocations[0];
                var targetSyntax = location.SourceTree.GetCompilationUnitRoot().FindNode(location.SourceSpan) as TypeDeclarationSyntax;

                var targetDocument = context.Document.Project.Solution.GetDocument(targetSyntax.SyntaxTree);
                var targetSemanticModel = await targetDocument.GetSemanticModelAsync().ConfigureAwait(false);
                var targetRoot = await targetDocument.GetSyntaxRootAsync().ConfigureAwait(false) as CompilationUnitSyntax;

                var action = CodeAction.Create("Add DataMember with Order", c => AddDataMemberWithOrder(targetDocument, targetSemanticModel, targetRoot, targetSyntax, c), location.ToString());
                context.RegisterCodeFix(action, context.Diagnostics.First());
            }
        }

        static async Task<Document> AddDataMemberWithOrder(Document document, SemanticModel model, CompilationUnitSyntax root, TypeDeclarationSyntax typeSyntax, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document);

            var dc = typeSyntax.FindAttribute(model, "System.Runtime.Serialization.DataContractAttribute");
            if (dc == null)
            {
                var dataContract = SyntaxFactory.ParseCompilationUnit("[DataContract]").DescendantNodes().OfType<AttributeListSyntax>().First().WithFormat();
                editor.AddAttribute(typeSyntax, dataContract);
            }

            var properties = typeSyntax
                .Members
                .Select(x =>
                {
                    if (x is FieldDeclarationSyntax) return new MemberSyntax(x as FieldDeclarationSyntax);
                    if (x is PropertyDeclarationSyntax) return new MemberSyntax(x as PropertyDeclarationSyntax);
                    return null;
                })
                .Where(x => x != null)
                .ToArray();

            // get max order.
            var order = properties.Select(prop => prop.AttributeLists
                    .Select(x => x.DescendantNodes().OfType<AttributeSyntax>().FirstOrDefault())
                    .Where(x => x != null)
                    .Where(x => model.GetTypeInfo(x).Type.Name == "DataMemberAttribute")
                    .FirstOrDefault())
                .Where(x => x != null)
                .Where(x => x.ArgumentList != null)
                .Select(a => a.ArgumentList.Arguments.FirstOrDefault(x => x?.NameEquals?.Name?.ToFullString()?.Trim() == "Order"))
                .Where(x => x != null)
                .Select(x => (int?)(x.Expression as LiteralExpressionSyntax)?.Token.Value ?? -1)
                .DefaultIfEmpty(-1)
                .Max() + 1;

            foreach (var node in properties)
            {
                var existingDataMember = node.AttributeLists
                    .Select(x => x.DescendantNodes().OfType<AttributeSyntax>().FirstOrDefault())
                    .Where(x => x != null)
                    .Where(x => model.GetTypeInfo(x).Type.Name == "DataMemberAttribute")
                    .FirstOrDefault();

                if (existingDataMember == null)
                {
                    // new
                    var attr = SyntaxFactory.ParseCompilationUnit($"[DataMember(Order = {order++})]")
                        .DescendantNodes()
                        .OfType<AttributeListSyntax>()
                        .First()
                        .WithLeadingTrivia(node.GetLeadingTrivia())
                        .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

                    var indent = node.GetLeadingTrivia().LastOrDefault(x => x.IsKind(SyntaxKind.WhitespaceTrivia));
                    editor.ReplaceNode(node.Self, node
                        .WithoutLeadingTrivia()
                        .WithLeadingTrivia(indent)
                        .AddAttributeLists(attr).Self);
                }
                else
                {
                    // replace existing attr
                    var argList = existingDataMember.ArgumentList;
                    if (argList == null)
                    {
                        // new if not exists AttributeArgumentList
                        var newArgList = SyntaxFactory.ParseAttributeArgumentList($"(Order = {order++})");
                        var newDataMember = existingDataMember.WithArgumentList(newArgList);
                        editor.ReplaceNode(node.Self, node.ReplaceNode(existingDataMember, newDataMember));
                    }
                    else
                    {
                        var orderProp = argList.Arguments.FirstOrDefault(x => x?.NameEquals?.Name?.ToFullString()?.Trim() == "Order");
                        if (orderProp != null)
                        {
                            // if have Order, do nothing
                        }
                        else
                        {
                            // if Order does not exists, add Order
                            var newOrder = SyntaxFactory.AttributeArgument(
                                SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Order").WithTrailingTrivia(SyntaxFactory.Space)),
                                null,
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(order++))
                                    .WithLeadingTrivia(SyntaxFactory.Space))
                                    .WithLeadingTrivia(SyntaxFactory.Space);

                            var newArgList = argList.WithArguments(argList.Arguments.Add(newOrder));
                            editor.ReplaceNode(node.Self, node.ReplaceNode(argList, newArgList));
                        }
                    }
                }
            }

            var newDocument = editor.GetChangedDocument();
            var newRoot = editor.GetChangedRoot() as CompilationUnitSyntax;
            newDocument = newDocument.WithSyntaxRoot(newRoot.WithUsing("System.Runtime.Serialization"));

            return newDocument;
        }
    }


    public class MemberSyntax
    {
        public SyntaxList<AttributeListSyntax> AttributeLists { get; }
        public Func<SyntaxTriviaList> GetLeadingTrivia { get; }
        public Func<SyntaxNode, SyntaxNode, SyntaxNode> ReplaceNode { get; }
        public Func<MemberSyntax> WithoutLeadingTrivia { get; }
        public Func<SyntaxTrivia, MemberSyntax> WithLeadingTrivia { get; }
        public Func<AttributeListSyntax, MemberSyntax> AddAttributeLists { get; }
        public SyntaxNode Self { get; }

        public MemberSyntax(PropertyDeclarationSyntax prop)
        {
            this.AttributeLists = prop.AttributeLists;
            this.GetLeadingTrivia = () => prop.GetLeadingTrivia();
            this.ReplaceNode = (x, y) => prop.ReplaceNode(x, y);
            this.WithoutLeadingTrivia = () => new MemberSyntax(prop.WithoutLeadingTrivia());
            this.WithLeadingTrivia = x => new MemberSyntax(prop.WithLeadingTrivia(x));
            this.AddAttributeLists = xs => new MemberSyntax(prop.AddAttributeLists(xs));
            this.Self = prop;
        }

        public MemberSyntax(FieldDeclarationSyntax field)
        {
            this.AttributeLists = field.AttributeLists;
            this.GetLeadingTrivia = () => field.GetLeadingTrivia();
            this.ReplaceNode = (x, y) => field.ReplaceNode(x, y);
            this.WithoutLeadingTrivia = () => new MemberSyntax(field.WithoutLeadingTrivia());
            this.WithLeadingTrivia = x => new MemberSyntax(field.WithLeadingTrivia(x));
            this.AddAttributeLists = xs => new MemberSyntax(field.AddAttributeLists(xs));
            this.Self = field;
        }
    }
}
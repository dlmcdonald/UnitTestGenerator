using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using UnitTestGenerator.Models;
using UnitTestGenerator.Services;

namespace UnitTestGenerator.QuickInfo
{
    sealed class RelatedTestsQuickInfoSource : IAsyncQuickInfoSource
    {
        // Copied from KnownMonikers, because Mono doesn't support ImageMoniker type.
        private static readonly ImageId AssemblyWarningImageId = new ImageId(
            new Guid("{ae27a6b0-e345-4288-96df-5eaf394ee369}"),
            200);

        readonly ITextBuffer _textBuffer;


        readonly IDocumentService _documentService;
        readonly INavigationService _navigationService;
        readonly IConfigurationService _configurationService;

        Project _unitTestProject;

        public RelatedTestsQuickInfoSource(ITextBuffer textBuffer, IDocumentService documentService, INavigationService navigationService, IConfigurationService configurationService)
        {
            _textBuffer = textBuffer;
            _documentService = documentService;
            _navigationService = navigationService;
            _configurationService = configurationService;

        }

        public void Dispose()
        {
            // This provider does not perform any cleanup.
        }

        public MethodDeclarationSyntax GetActiveMethodDeclarationSyntax(SyntaxNode syntaxRoot, SnapshotPoint snapshotPoint)
        {

            var span = TextSpan.FromBounds(snapshotPoint, snapshotPoint + 1);
            try
            {
                if (syntaxRoot.Span.IntersectsWith(span) && span.Start >= syntaxRoot.SpanStart && span.End <= syntaxRoot.Span.End)
                {
                    var syntaxNode = syntaxRoot.FindNode(span);
                    if (syntaxNode is MethodDeclarationSyntax method)
                    {
                        return method;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return null;
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(
            IAsyncQuickInfoSession session,
            CancellationToken cancellationToken)
        {
            var config = await _configurationService.GetConfiguration();
            if (config == null || string.IsNullOrWhiteSpace(config.UnitTestProjectName))
                return null;
            try
            {
                var triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);
                var line = triggerPoint.Value.GetContainingLine();
                var lineSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(
                    line.Extent,
                    SpanTrackingMode.EdgeInclusive);
                if (_documentService != null)
                {
                    var currentDocument = _documentService.GetCurrentDocument();
                    _unitTestProject = currentDocument.Project.Solution.Projects.FirstOrDefault(p => p.Name.Equals(config.UnitTestProjectName));
                    SyntaxNode syntaxRoot = null;
                    if (currentDocument.TryGetSyntaxTree(out var ast))
                    {
                        syntaxRoot = ast.GetRoot();
                    }

                    if (syntaxRoot != null && triggerPoint.HasValue)
                    {
                        var method = GetActiveMethodDeclarationSyntax(syntaxRoot, triggerPoint.Value);
                        
                        if (method != null)
                        {

                            var classSyntax = method.Parent as ClassDeclarationSyntax;

                            var references = await FindReferencesInUnitTestProject(method.Identifier.Text, classSyntax.Identifier.Text, currentDocument.Project.Solution);


                            if (references != null && references.Any())
                            {
                                var content = GetContent(references);
                                content.Insert(0, new ClassifiedTextElement(
                                    new ClassifiedTextRun(PredefinedClassificationTypeNames.String, $"Test Methods:")));



                                var contentContainer = new ContainerElement(
                                    ContainerElementStyle.Stacked,
                                    content);


                                return new QuickInfoItem(lineSpan, contentContainer);
                            }
                        }
                    }
                }
                
                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Got an exception!");
                Debug.WriteLine(e);
                return null;
            }

        }

        async Task<List<TestReference>> FindReferencesInUnitTestProject(string methodName, string className, Solution solution)
        {
            
            var references = new List<TestReference>();
            if (_unitTestProject == null)
                return null;
            foreach (var document in _unitTestProject.Documents)
            {
                var model = await document.GetSemanticModelAsync();

                var syntaxRoot = await document.GetSyntaxRootAsync();
                
                try
                {
                    var invocations = syntaxRoot.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => ((MemberAccessExpressionSyntax)i.Expression).Name.ToString().Equals(methodName)).ToList();
                    if (invocations != null && invocations.Any())
                    { 
                        foreach (var invocation in invocations)
                        {
                            var invokedSymbolInfo = model.GetSymbolInfo(invocation);
                            var method = GetContainingMethod(invocation);
                            if (method != null && invokedSymbolInfo.Symbol.ContainingSymbol.Name.EndsWith(className))
                            {
                                //var mr = new MethodReference { MethodName = method.Identifier.Text };
                                if (method.Parent is ClassDeclarationSyntax classSyntax)
                                {
                                    //var containingMethodSymbolInfo = model.GetSymbolInfo(method);
                                    var containingMethodSymbol = model.GetDeclaredSymbol(method);
                                    var existing = references.FirstOrDefault(r => classSyntax.Identifier.Text.Equals(r.ClassName));
                                    if (existing == null)
                                    {
                                        references.Add(new TestReference { ClassName = classSyntax.Identifier.Text, MethodReferences = new List<MethodReference> { new MethodReference { MethodName = method.Identifier.Text, MethodSymbol = containingMethodSymbol } } });
                                    }
                                    else
                                    {
                                        references.First(r => classSyntax.Identifier.Text.Equals(r.ClassName)).MethodReferences.Add( new MethodReference { MethodName = method.Identifier.Text, MethodSymbol = containingMethodSymbol });
                                    }
                                }
                            }
                            
                        }
                    }
                }
                catch (Exception)
                {
                    // Swallow the exception of type cast. 
                    // Could be avoided by a better filtering on above linq.
                    continue;
                }
            }
            return references;
        }

        MethodDeclarationSyntax GetContainingMethod(SyntaxNode syntaxNode)
        {
            while (syntaxNode.Parent != null)
            {
                syntaxNode = syntaxNode.Parent;
                if (syntaxNode is MethodDeclarationSyntax)
                    break;
            }
            if (syntaxNode is MethodDeclarationSyntax method)
                return method;
            return null;
        }

        List<ClassifiedTextElement> GetContent(List<TestReference> references)
        {
            var elements = new List<ClassifiedTextElement>();
            foreach (var classRef in references)
            {
                elements.Add(
                    new ClassifiedTextElement(
                        new ClassifiedTextRun(
                            ClassificationTypeNames.ClassName, $"{classRef.ClassName}")));
                foreach (var methodReference in classRef.MethodReferences)
                {
                    elements.Add(new ClassifiedTextElement(
                        new ClassifiedTextRun(
                                            ClassificationTypeNames.MethodName,
                                            $"\t{methodReference.MethodName}")));
                    
                }
            }
            return elements;
        }

        public void NavigateToDeclaration(ISymbol symbol)
        {
            _navigationService.Navigate(_unitTestProject, symbol);
        }
    }
}
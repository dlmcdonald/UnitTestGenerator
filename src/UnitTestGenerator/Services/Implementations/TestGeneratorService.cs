﻿using System;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Projects;
using UnitTestGenerator.Helpers;
using UnitTestGenerator.Models;
using UnitTestGenerator.Services.Interfaces;

namespace UnitTestGenerator.Services.Implementations
{
    [Export(typeof(ITestGeneratorService))]
    public class TestGeneratorService : ITestGeneratorService
    {
        readonly IConfigurationService _configurationService;
        readonly IFileService _fileService;
        public TestGeneratorService()
        {
            _configurationService = CompositionManager.GetExportedValue<IConfigurationService>();
            _fileService = CompositionManager.GetExportedValue<IFileService>();
        }

        public async Task<GeneratedTest> CreateGeneratedTestModel(MethodDeclarationSyntax method)
        {
            //Get the configuration model
            var config = await _configurationService.GetConfiguration();
            var generatedTest = new GeneratedTest();

            var projects = IdeApp.Workspace.GetAllProjects();
            var unitTestProject = projects.FirstOrDefault(p => p.Name == config.UnitTestProjectName);

            if (unitTestProject == null)
                return null;

            var classSyntax = method.Parent as ClassDeclarationSyntax;
            var namespaceSyntax = classSyntax.Parent as NamespaceDeclarationSyntax;

            var requiredFolders = namespaceSyntax.Name.ToString().Split('.').ToList();
            if (requiredFolders == null || !requiredFolders.Any())
                return null;

            //remove the first part of namespace as per setting
            if (config.RemoveFirst)
                requiredFolders.RemoveAt(0);

            //add the class name of the method as another folder
            requiredFolders.Add(classSyntax.Identifier.Text + config.UnitTestClassSuffix);

            var outputFile = unitTestProject.BaseDirectory.ToString() + "/";
            var namespaceId = config.UnitTestProjectName;
            foreach (var item in requiredFolders)
            {
                outputFile += item.Trim() + "/";
                namespaceId += "." + item.Trim();
            }
            outputFile += method.Identifier.Text + $"{config.UnitTestSuffix}.cs";
            generatedTest.FilePath = outputFile;
            generatedTest.Namespace = namespaceId;
            generatedTest.Name = method.Identifier.Text + config.UnitTestSuffix;
            return generatedTest;
        }

        public MethodDeclarationSyntax GetActiveMethodDeclarationSyntax()
        {
            var document = IdeApp.Workbench.ActiveDocument;
            if (document != null)
            {
                var analysisDocument = document.GetAnalysisDocument();
                var textView = document.GetContent<ITextView>();

                var caretOffset = textView.Caret.Position.BufferPosition;
                if (analysisDocument.TryGetSemanticModel(out _) && analysisDocument.TryGetSyntaxTree(out var ast))
                {
                    var syntaxRoot = ast.GetRoot();
                    var span = TextSpan.FromBounds(caretOffset, caretOffset + 1);
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
                }
            }
            return null;
        }

        public async Task<MonoDevelop.Ide.Gui.Document> OpenDocument(GeneratedTest generatedTestModel)
        {
            var config = await _configurationService.GetConfiguration();
            var projects = IdeApp.Workspace.GetAllProjects();
            var unitTestProject = projects.FirstOrDefault(p => p.Name == config.UnitTestProjectName);

            if (unitTestProject == null)
                return null;

            var file = unitTestProject.Files.FirstOrDefault(f => f.FilePath == generatedTestModel.FilePath);
            if (file == null)
            {
                try
                {
                    await _fileService.GenerateFile(generatedTestModel.Namespace, generatedTestModel.Name, generatedTestModel.FilePath);
                    file = new ProjectFile(generatedTestModel.FilePath, BuildAction.Compile)
                    {
                        Visible = true,
                    };
                    unitTestProject.AddFile(file);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    return null;
                }
            }
            var document = IdeApp.Workbench.OpenDocument(file.FilePath, unitTestProject, true).Result;
            return document;
        }

        public async Task GenerateUnitTest(string unitTestName, MethodDeclarationSyntax currentMethod, MonoDevelop.Ide.Gui.Document document)
        {
            var isTask = false;
            if (currentMethod.ReturnType is GenericNameSyntax taskSomethingReturnType)
            {
                isTask |= taskSomethingReturnType.Identifier.Text.Equals("Task");
            }
            else if (currentMethod.ReturnType is IdentifierNameSyntax taskReturnType)
            {
                isTask |= taskReturnType.Identifier.Text.Equals("Task");
            }

            var returnType = isTask ? "Task" : "void";
            var modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (isTask)
            {
                modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
            }
            var newMethod = GenerateUnitTestMethodDeclaration(returnType, modifiers, unitTestName);

            var analysisDoc = document.GetAnalysisDocument();
            var editor = await DocumentEditor.CreateAsync(analysisDoc);
            var cuRoot = editor.SemanticModel.SyntaxTree.GetCompilationUnitRoot();
            if (cuRoot == null)
                return;
            var lastMethod = cuRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().LastOrDefault();
            editor.InsertAfter(lastMethod, newMethod);

            if (isTask)
            {
                var taskUsing = cuRoot.Usings.FirstOrDefault(u => u.Name.ToString().Contains("System.Threading.Tasks"));
                if (taskUsing == null)
                {
                    taskUsing = GenerateTaskUsingSyntax();
                    var lastUsing = cuRoot.Usings.LastOrDefault();
                    editor.InsertAfter(lastUsing, taskUsing);
                }
            }

            var newDocument = editor.GetChangedDocument();

            var newRoot = await newDocument.GetSyntaxRootAsync();
            var textBuffer = document.GetContent<ITextBuffer>();
            Microsoft.CodeAnalysis.Workspace.TryGetWorkspace(textBuffer.AsTextContainer(), out var workspace);
            newRoot = Formatter.Format(newRoot, Formatter.Annotation, workspace);
            workspace.TryApplyChanges(newDocument.WithSyntaxRoot(newRoot).Project.Solution);
            await document.Save();
        }

        public MethodDeclarationSyntax GenerateUnitTestMethodDeclaration(string returnTypeName, SyntaxTokenList modifiers, string methodName)
        {

            return SyntaxFactory.MethodDeclaration(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                          modifiers: modifiers,
                          returnType: SyntaxFactory.ParseTypeName(returnTypeName),
                          explicitInterfaceSpecifier: null,
                          identifier: SyntaxFactory.Identifier(methodName),
                          typeParameterList: null,
                          parameterList: SyntaxFactory.ParameterList(),
                          constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                          body: SyntaxFactory.Block(),
                          semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                  // Annotate that this node should be formatted
                  .WithAdditionalAnnotations(Formatter.Annotation)
                  .WithAttributeLists(
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Attribute(
                                        SyntaxFactory.IdentifierName("Test"))))));
        }

        public UsingDirectiveSyntax GenerateTaskUsingSyntax()
        {
            var qualifiedName = SyntaxFactory.ParseName(" System.Threading.Tasks");
            var usingSmnt = SyntaxFactory.UsingDirective(qualifiedName);
            return usingSmnt;
        }
    }
}
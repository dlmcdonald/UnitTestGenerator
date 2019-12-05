using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gtk;
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
            _configurationService = CompositionManager.Instance.GetExportedValue<IConfigurationService>();
            _fileService = CompositionManager.Instance.GetExportedValue<IFileService>();
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
            generatedTest.ClassName = classSyntax.Identifier.Text;
            generatedTest.MethodName = method.Identifier.Text;

            //class constructor parameters
            var classConstructor = classSyntax.DescendantNodes().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();

            if (classConstructor.ParameterList != null && classConstructor.ParameterList.Parameters != null && classConstructor.ParameterList.Parameters.Any())
            {
                generatedTest.ClassConstructorParameters = new List<Parameter>();
                foreach (var parameter in classConstructor.ParameterList.Parameters)
                {
                    var identifier = parameter.Type as IdentifierNameSyntax;
                    generatedTest.ClassConstructorParameters.Add(new Parameter
                    {
                        Name = parameter.Identifier.Text,
                        ClassName = identifier.Identifier.Text
                    });
                }
            }

            //Method Parameters
            if (method.ParameterList.Parameters.Any())
            {
                generatedTest.MethodParameters = new List<Parameter>();
                foreach (var parameter in method.ParameterList.Parameters)
                {
                    var className = "";
                    if (parameter.Type is IdentifierNameSyntax ins)
                        className = ins.Identifier.Text;
                    else if (parameter.Type is PredefinedTypeSyntax pds)
                        className = pds.ToString();
                    else if (parameter.Type is GenericNameSyntax ns)
                        className = ns.ToString();
                    generatedTest.MethodParameters.Add(new Parameter
                    {
                        Name = parameter.Identifier.Text,
                        ClassName = className
                    });
                }
            }

            //Method return type calculation
            var isTask = false;
            if (method.ReturnType is GenericNameSyntax gns)
            {
                isTask |= gns.Identifier.Text.Equals("Task");
                if (isTask)
                {
                    generatedTest.ReturnType = gns.TypeArgumentList?.Arguments.FirstOrDefault()?.ToString();
                }
                else
                {
                    var rType = gns.Identifier.Text;
                    var arguments = string.Join(",", gns.TypeArgumentList?.Arguments.Select(a => a.ToString()));
                    if (!string.IsNullOrEmpty(arguments))
                    {
                        rType += $"<{arguments}>";
                    }
                    generatedTest.ReturnType = rType;
                }
            }
            else if (method.ReturnType is IdentifierNameSyntax ins)
            {
                isTask |= ins.Identifier.Text.Equals("Task");
                generatedTest.ReturnType = ins.ToString().Equals("Task") ? null : ins.ToString();
            }
            else if (method.ReturnType is PredefinedTypeSyntax pns)
            {
                generatedTest.ReturnType = pns.ToString().Equals("void") ? null : pns.ToString();
            }
            generatedTest.IsTask = isTask;
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
                if (analysisDocument.TryGetSyntaxTree(out var ast))
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
            var document = await IdeApp.Workbench.OpenDocument(file.FilePath, unitTestProject, true);
            return document;
        }

        public async Task GenerateUnitTest(string unitTestName, MethodDeclarationSyntax currentMethod, MonoDevelop.Ide.Gui.Document document, GeneratedTest generatedTestModel)
        {


            var returnType = generatedTestModel.IsTask ? "Task" : "void";
            var modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (generatedTestModel.IsTask)
            {
                modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
            }
            var newMethod = GenerateUnitTestMethodDeclaration(returnType, modifiers, unitTestName, generatedTestModel);

            var analysisDoc = document.GetAnalysisDocument();
            var editor = await DocumentEditor.CreateAsync(analysisDoc);
            var cuRoot = editor.SemanticModel.SyntaxTree.GetCompilationUnitRoot();
            if (cuRoot == null)
                return;
            var lastMethod = cuRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().LastOrDefault();
            editor.InsertAfter(lastMethod, newMethod);

            if (generatedTestModel.IsTask)
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

        public MethodDeclarationSyntax GenerateUnitTestMethodDeclaration(string returnTypeName, SyntaxTokenList modifiers, string methodName, GeneratedTest generatedTestModel)
        {
            var generateResult = true;
            var bodyStatements = new List<StatementSyntax>();
            
            var ctorParams = "";
            var addedArrange = false;

            if (generateResult && !string.IsNullOrWhiteSpace(generatedTestModel.ReturnType))
            {
                
                bodyStatements.Add(SyntaxFactory.ParseStatement($"var expected = default({generatedTestModel.ReturnType});\n").WithLeadingTrivia(SyntaxFactory.Comment("//Arrange\n")));
                addedArrange = true;
            }
            var methodParams = "";
            if (generatedTestModel.MethodParameters != null && generatedTestModel.MethodParameters.Any())
            {
                foreach (var parameter in generatedTestModel.MethodParameters)
                {
                    if (!addedArrange)
                    {
                        bodyStatements.Add(SyntaxFactory.ParseStatement($"var {parameter.Name} = default({parameter.ClassName});\n").WithLeadingTrivia(SyntaxFactory.Comment("//Arrange\n")));
                        addedArrange = true;
                    }
                    else
                        bodyStatements.Add(SyntaxFactory.ParseStatement($"var {parameter.Name} = default({parameter.ClassName});\n"));
                    methodParams += $"{parameter.Name}, ";
                }
            }

            foreach (var parameter in generatedTestModel.ClassConstructorParameters)
            {

                if (!addedArrange)
                {
                    bodyStatements.Add(SyntaxFactory.ParseStatement($"var {parameter.Name} = new Mock<{parameter.ClassName}>();\n").WithLeadingTrivia(SyntaxFactory.Comment("//Arrange\n")));
                    addedArrange = true;
                }
                else
                    bodyStatements.Add(SyntaxFactory.ParseStatement($"var {parameter.Name} = new Mock<{parameter.ClassName}>();\n"));
                ctorParams += $"{parameter.Name}.Object, ";
            }


            var classCtor = SyntaxFactory.ParseStatement($"var vm = new {generatedTestModel.ClassName}({ctorParams.TrimEnd(' ').TrimEnd(',')});\n").WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
            bodyStatements.Add(classCtor);

            if(generateResult)
            {
                var resultSection = string.IsNullOrWhiteSpace(generatedTestModel.ReturnType) ? "" : "var result = ";
                var awaitSection = generatedTestModel.IsTask ? "await " : "";
                bodyStatements.Add(SyntaxFactory.ParseStatement($"{resultSection}{awaitSection}vm.{generatedTestModel.MethodName}({methodParams.TrimEnd(' ').TrimEnd(',')});\n").WithLeadingTrivia(SyntaxFactory.Comment("//Act\n")));

                if (!string.IsNullOrWhiteSpace(generatedTestModel.ReturnType))
                {
                    bodyStatements.Add(SyntaxFactory.ParseStatement("Assert.That(expected == result);\n").WithLeadingTrivia(SyntaxFactory.Comment("//Assert\n")));
                }
                else
                {
                    bodyStatements.Add(SyntaxFactory.ParseStatement("Assert.That(true == true);\n").WithLeadingTrivia(SyntaxFactory.Comment("//Assert\n")));
                }
            }

           
            

            var method = SyntaxFactory.MethodDeclaration(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                          modifiers: modifiers,
                          returnType: SyntaxFactory.ParseTypeName(returnTypeName),
                          explicitInterfaceSpecifier: null,
                          identifier: SyntaxFactory.Identifier(methodName),
                          typeParameterList: null,
                          parameterList: SyntaxFactory.ParameterList(),
                          constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                          body: SyntaxFactory.Block(bodyStatements),
                          semicolonToken: SyntaxFactory.Token(SyntaxKind.None))
                  // Annotate that this node should be formatted
                  .WithAdditionalAnnotations(Formatter.Annotation)
                  .WithAttributeLists(
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Attribute(
                                        SyntaxFactory.IdentifierName("Test"))))));


            
            return method;
        }

        public UsingDirectiveSyntax GenerateTaskUsingSyntax()
        {
            var qualifiedName = SyntaxFactory.ParseName(" System.Threading.Tasks");
            var usingSmnt = SyntaxFactory.UsingDirective(qualifiedName);
            return usingSmnt;
        }
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using UnitTestGenerator.Models;

namespace UnitTestGenerator.Helpers
{
    public class UnitTestHelper
    {
        public GeneratedTest CreateGeneratedTestModel(MethodDeclarationSyntax method)
        {
            //Get the configuration model
            var config = new ConfigurationHelper().GetConfiguration();
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
                            SyntaxNode syntaxNode = syntaxRoot.FindNode(span);
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

        public MonoDevelop.Ide.Gui.Document OpenDocument(GeneratedTest generatedTestModel)
        {
            var config = new ConfigurationHelper().GetConfiguration();
            var projects = IdeApp.Workspace.GetAllProjects();
            var unitTestProject = projects.FirstOrDefault(p => p.Name == config.UnitTestProjectName);

            if (unitTestProject == null)
                return null;

            var file = unitTestProject.Files.FirstOrDefault(f => f.FilePath == generatedTestModel.FilePath);
            if (file == null)
            {
                try
                {
                    FileGenerator.GenerateFile(generatedTestModel.Namespace, generatedTestModel.Name, generatedTestModel.FilePath, "XFUnitTestNUnit");
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

        public void GenerateUnitTest(string _unitTestName, MethodDeclarationSyntax _currentMethod, MonoDevelop.Ide.Gui.Document _document)
        {
            //TODO: Investigate maybe using DocumentEditor to do changes instead of updaing syntax root
            var isTask = false;
            if (_currentMethod.ReturnType is GenericNameSyntax taskSomethingReturnType)
            {
                isTask |= taskSomethingReturnType.Identifier.Text.Equals("Task");
            }
            else if (_currentMethod.ReturnType is IdentifierNameSyntax taskReturnType)
            {
                isTask |= taskReturnType.Identifier.Text.Equals("Task");
            }

            var returnType = isTask ? "Task" : "void";
            var modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (isTask)
            {
                modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
            }
            var newMethod = GenerateUnitTestMethodDeclaration(returnType, modifiers, _unitTestName);
            var analysisDoc = _document.GetAnalysisDocument();
            analysisDoc.TryGetSyntaxRoot(out var syntaxRoot);

            var cds = syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            var newCds = cds.AddMembers(newMethod);

            var newRoot = syntaxRoot.ReplaceNode(cds, newCds);

            var textBuffer = _document.GetContent<ITextBuffer>();

            Microsoft.CodeAnalysis.Workspace.TryGetWorkspace(textBuffer.AsTextContainer(), out var workspace);
            newRoot = Formatter.Format(newRoot, Formatter.Annotation, workspace);
            workspace.TryApplyChanges(analysisDoc.WithSyntaxRoot(newRoot).Project.Solution);
            //check if we need to add the using statement for System.Threading.Tasks

            //TODO: Add this back when you figure out how it works
            //if (isTask)
            //{
            //    var nsds = cds.Parent as NamespaceDeclarationSyntax;
            //    var cus = nsds.Parent as CompilationUnitSyntax;
            //    var taskUsing = cus.Usings.FirstOrDefault(u => u.Name.ToString().Contains("System.Threading.Tasks"));
            //    if (taskUsing == null)
            //    {
                    
            //        taskUsing = GenerateTaskUsingSyntax();
            //        var newADoc = _document.GetAnalysisDocument();
            //        var editor = DocumentEditor.CreateAsync(newADoc).Result;
            //        var lastUsing = editor.SemanticModel.SyntaxTree.GetCompilationUnitRoot().Usings.FirstOrDefault();
            //        editor.InsertAfter(lastUsing, taskUsing);
            //        var updatedADoc = editor.GetChangedDocument();
            //        //var newCus = cus.AddUsings(taskUsing);
            //        //var usingnewRoot = newRoot.ReplaceNode(cus, newCus);

            //        //usingnewRoot = Formatter.Format(usingnewRoot, Formatter.Annotation, workspace);
            //        workspace.TryApplyChanges(updatedADoc.Project.Solution);
            //        //workspace.TryApplyChanges(analysisDoc.WithSyntaxRoot(usingnewRoot).Project.Solution);
            //    }                
            //}
           

            
            _document.Save().ConfigureAwait(false);
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
            //return SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Threading.Tasks"))
            //    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            var qualifiedName = SyntaxFactory.ParseName("System.Threading.Tasks");
            var usingSmnt = SyntaxFactory.UsingDirective(qualifiedName);
            return usingSmnt;
        }
    }
}

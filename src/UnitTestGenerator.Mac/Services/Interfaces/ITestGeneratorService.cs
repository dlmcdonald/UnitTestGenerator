using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnitTestGenerator.Models;

namespace UnitTestGenerator.Mac.Services.Interfaces
{
    public interface ITestGeneratorService
    {
        Task<GeneratedTest> CreateGeneratedTestModel(MethodDeclarationSyntax method, Document analysisDocument);
        MethodDeclarationSyntax GetActiveMethodDeclarationSyntax();
        Task<MonoDevelop.Ide.Gui.Document> OpenDocument(GeneratedTest generatedTestModel);
        Task GenerateUnitTest(string unitTestName, MethodDeclarationSyntax currentMethod, MonoDevelop.Ide.Gui.Document document, GeneratedTest generatedTestModel);
        MethodDeclarationSyntax GenerateUnitTestMethodDeclaration(string returnTypeName, SyntaxTokenList modifiers, string methodName, string annotation, GeneratedTest generatedTestModel);
        UsingDirectiveSyntax GenerateTaskUsingSyntax();
        UsingDirectiveSyntax GenerateUsingSyntax(string namespaceName);
    }
}

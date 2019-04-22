using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnitTestGenerator.Models;

namespace UnitTestGenerator.Services.Interfaces
{
    public interface ITestGeneratorService
    {
        Task<GeneratedTest> CreateGeneratedTestModel(MethodDeclarationSyntax method);
        MethodDeclarationSyntax GetActiveMethodDeclarationSyntax();
        Task<MonoDevelop.Ide.Gui.Document> OpenDocument(GeneratedTest generatedTestModel);
        Task GenerateUnitTest(string unitTestName, MethodDeclarationSyntax currentMethod, MonoDevelop.Ide.Gui.Document document);
        MethodDeclarationSyntax GenerateUnitTestMethodDeclaration(string returnTypeName, SyntaxTokenList modifiers, string methodName);
        UsingDirectiveSyntax GenerateTaskUsingSyntax();
    }
}

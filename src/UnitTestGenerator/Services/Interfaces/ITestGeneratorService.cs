using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnitTestGenerator.Models;

namespace UnitTestGenerator.Services.Interfaces
{
    public interface ITestGeneratorService
    {
        GeneratedTest CreateGeneratedTestModel(MethodDeclarationSyntax method);
        MethodDeclarationSyntax GetActiveMethodDeclarationSyntax();
        MonoDevelop.Ide.Gui.Document OpenDocument(GeneratedTest generatedTestModel);
        void GenerateUnitTest(string unitTestName, MethodDeclarationSyntax currentMethod, MonoDevelop.Ide.Gui.Document document);
        MethodDeclarationSyntax GenerateUnitTestMethodDeclaration(string returnTypeName, SyntaxTokenList modifiers, string methodName);
        UsingDirectiveSyntax GenerateTaskUsingSyntax();
    }
}

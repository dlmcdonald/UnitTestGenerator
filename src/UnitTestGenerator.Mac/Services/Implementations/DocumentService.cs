using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using UnitTestGenerator.Mac.Helpers;
using UnitTestGenerator.Services;

namespace UnitTestGenerator.Mac.Services
{
    [Export(typeof(IDocumentService))]
    public class DocumentService : IDocumentService
    {
        public Document GetCurrentDocument()
        {
            var document = IdeApp.Workbench.ActiveDocument;
            if (document != null)
            {
                var analysisDocument = document.GetAnalysisDocument();
                return analysisDocument;
                //if (analysisDocument.TryGetSyntaxTree(out var ast))
                //{
                //    return ast.GetRoot();
                //}
            }
            return null;
        }
    }
}

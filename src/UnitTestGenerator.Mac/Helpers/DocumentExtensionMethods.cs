using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Ide.Editor;

namespace UnitTestGenerator.Mac.Helpers
{
    public static class DocumentExtensionMethods
    {
        public static Document GetOpenDocumentInCurrentContext(this SourceTextContainer container)
        {
            if (Workspace.TryGetWorkspace(container, out var workspace))
            {
                var id = workspace.GetDocumentIdInCurrentContext(container);
                return workspace.CurrentSolution.GetDocument(id);
            }

            return null;
        }

        public static Document GetAnalysisDocument(this DocumentContext documentContext)
        {
            var textBuffer = documentContext.GetContent<ITextBuffer>();
            if (textBuffer != null && textBuffer.AsTextContainer() is SourceTextContainer container)
            {
                var document = container.GetOpenDocumentInCurrentContext();
                if (document != null)
                {
                    return document;
                }
            }

            return null;
        }

        public static Document GetAnalysisDocument(this MonoDevelop.Ide.Gui.Document documentContext)
        {
            var textBuffer = documentContext.GetContent<ITextBuffer>();
            if (textBuffer != null && textBuffer.AsTextContainer() is SourceTextContainer container)
            {
                var document = container.GetOpenDocumentInCurrentContext();
                if (document != null)
                {
                    return document;
                }
            }

            return null;
        }
    }
}

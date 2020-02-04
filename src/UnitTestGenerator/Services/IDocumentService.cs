using Microsoft.CodeAnalysis;

namespace UnitTestGenerator.Services
{
    public interface IDocumentService
    {
        Document GetCurrentDocument();
    }
}

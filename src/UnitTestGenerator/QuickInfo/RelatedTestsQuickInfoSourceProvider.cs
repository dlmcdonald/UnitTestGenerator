namespace AsyncQuickInfoDemo
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Utilities;
    using UnitTestGenerator.QuickInfo;
    using UnitTestGenerator.Services;

    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Related Tests Async Quick Info Provider")]
    [ContentType("any")]
    [Order(After ="default")]
    sealed class RelatedTestsQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        [Import(typeof(IDocumentService))]
        readonly IDocumentService _documentService;
        [Import(typeof(INavigationService))]
        readonly INavigationService _navigationService;
        [Import(typeof(IConfigurationService))]
        readonly IConfigurationService _configurationService;

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new RelatedTestsQuickInfoSource(textBuffer, _documentService, _navigationService, _configurationService);
        }
    }
}
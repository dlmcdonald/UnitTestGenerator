using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace UnitTestGenerator.Mac.Glyphs
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(TodoTag))]
    public class TodoTaggerProvider : ITaggerProvider
    {
        [Import]
        internal IClassifierAggregatorService _aggregatorService;


        public TodoTaggerProvider()
        {
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            return new TodoTagger(_aggregatorService.GetClassifier(buffer)) as ITagger<T>;
        }
    }
}

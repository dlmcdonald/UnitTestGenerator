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
    [TagType(typeof(TestableMethodTag))]
    public class TestableMethodTaggerProvider : ITaggerProvider
    {
        [Import]
        internal IClassifierAggregatorService _aggregatorService;


        public TestableMethodTaggerProvider()
        {
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            return new TestableMethodTagger(_aggregatorService.GetClassifier(buffer)) as ITagger<T>;
        }
    }
}

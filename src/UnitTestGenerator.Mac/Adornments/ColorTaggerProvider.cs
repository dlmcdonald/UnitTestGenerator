using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace UnitTestGenerator.Mac.Adornments
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(ColorTag))]
    sealed class ColorTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            return buffer.Properties.GetOrCreateSingletonProperty(() => new ColorTagger(buffer)) as ITagger<T>;
        }
    }
}

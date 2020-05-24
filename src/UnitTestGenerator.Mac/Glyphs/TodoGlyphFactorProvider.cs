using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace UnitTestGenerator.Mac.Glyphs
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("TodoGlyph")]
    [Order(After = "VsTextMarker")]
    [ContentType("code")]
    [TagType(typeof(TodoTag))]
    sealed class TodoGlyphFactorProvider : IGlyphFactoryProvider
    {
        public TodoGlyphFactorProvider()
        {
        }

        public IGlyphFactory GetGlyphFactory(ICocoaTextView textView, ICocoaTextViewMargin margin)
        {
            return new TodoGlyphFactory();
        }
    }
}

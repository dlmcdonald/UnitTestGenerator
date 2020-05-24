using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace UnitTestGenerator.Mac.Glyphs
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("TestableMethodGlyph")]
    [Order(After = "VsTextMarker")]
    [ContentType("code")]
    [TagType(typeof(TestableMethodTag))]
    sealed class TestableMethodGlyphFactoryProvider : IGlyphFactoryProvider
    {
        public TestableMethodGlyphFactoryProvider()
        {
        }

        public IGlyphFactory GetGlyphFactory(ICocoaTextView textView, ICocoaTextViewMargin margin)
        {
            return new TestableMethodGlyphFactory();
        }
    }
}

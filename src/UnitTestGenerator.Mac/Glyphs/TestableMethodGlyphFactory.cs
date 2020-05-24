using System;
using AppKit;
using CoreGraphics;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace UnitTestGenerator.Mac.Glyphs
{
    public class TestableMethodGlyphFactory : IGlyphFactory
    {
        const double _glyphSize = 16.0;

        public TestableMethodGlyphFactory()
        {
        }

        public object GenerateGlyph(ITextViewLine line, IGlyphTag tag)
        {
            // Ensure we can draw a glyph for this marker.
            if (tag == null || !(tag is TestableMethodTag))
            {
                return null;
            }

            var view = new CircleView(new CGRect(0, 0, _glyphSize, _glyphSize), CGColor.CreateSrgb(1, 0, 0, 1));
            return view;
        }
    }
}

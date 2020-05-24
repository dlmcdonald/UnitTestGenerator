using System;
using AppKit;
using CoreGraphics;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace UnitTestGenerator.Mac.Glyphs
{
    class TodoGlyphFactory : IGlyphFactory
    {
        const double _glyphSize = 16.0;

        public TodoGlyphFactory()
        {
        }

        public object GenerateGlyph(ITextViewLine line, IGlyphTag tag)
        {
            // Ensure we can draw a glyph for this marker.
            if (tag == null || !(tag is TodoTag))
            {
                return null;
            }

            var view = new NSView(new CGRect(0, 0, 16, 16));
            view.WantsLayer = true;
            view.Layer.BackgroundColor = CGColor.CreateSrgb(0, 1, 0, 1);



            return view;
        }
    }
}

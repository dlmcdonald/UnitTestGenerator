using System;
using AppKit;
using CoreGraphics;

namespace UnitTestGenerator.Mac.Glyphs
{
    public class CircleView : NSView
    {
        readonly CGColor _color;

        public CircleView(CGRect rect, CGColor color) : base(rect)
        {
            _color = color;
        }

        public override void DrawRect(CGRect dirtyRect)
        {
            base.DrawRect(dirtyRect);

            var context = NSGraphicsContext.CurrentContext.CGContext;
            context.SaveState();
            context.SetFillColor(_color);
            context.FillEllipseInRect(dirtyRect);
            context.RestoreState();
        }
    }
}

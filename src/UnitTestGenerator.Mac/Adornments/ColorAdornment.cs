using AppKit;
using CoreGraphics;

namespace UnitTestGenerator.Mac.Adornments
{
    sealed class ColorAdornment : NSView
    {
        internal ColorAdornment(ColorTag colorTag) : base(new CGRect(0, 0, 20, 10))
        {
            //this.BackgroundColor = colorTag.Color;
            //rect = new CGRect(0, 0, 20, 10);
            WantsLayer = true;
            Layer.BackgroundColor = colorTag.Color;
            //rect = new Rectangle()
            //{
            //    //Stroke = Brushes.Black,
            //    //StrokeThickness = 1,
            //    Width = 20,
            //    Height = 10,
            //};

            //Update(colorTag);

            //Content = rect;
        }

        //private Brush MakeBrush(Color color)
        //{
        //    var brush = new SolidColorBrush(color);
        //    brush.Freeze();
        //    return brush;
        //}

        internal void Update(ColorTag colorTag)
        {
            //rect.Fill = MakeBrush(colorTag.Color);
            //BackgroundColor = colorTag.Color;
            Layer.BackgroundColor = colorTag.Color;
        }
    }
}

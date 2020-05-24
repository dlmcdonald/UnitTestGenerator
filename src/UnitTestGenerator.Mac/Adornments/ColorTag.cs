using CoreGraphics;
using Microsoft.VisualStudio.Text.Tagging;

namespace UnitTestGenerator.Mac.Adornments
{
    /// <summary>
    /// Data tag indicating that the tagged text represents a color.
    /// </summary>
    /// <remarks>
    /// Note that this tag has nothing directly to do with adornments or other UI.
    /// This sample's adornments will be produced based on the data provided in these tags.
    /// This separation provides the potential for other extensions to consume color tags
    /// and provide alternative UI or other derived functionality over this data.
    /// </remarks>
    class ColorTag : ITag
    {
        internal ColorTag(CGColor color)
        {
            Color = color;
        }

        internal readonly CGColor Color;
    }
}

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using CoreGraphics;
using Microsoft.VisualStudio.Text;

namespace UnitTestGenerator.Mac.Adornments
{
    /// <summary>
    /// Determines which spans of text likely refer to color values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a data-only component. The tagging system is a good fit for presenting data-about-text.
    /// The <see cref="ColorAdornmentTagger"/> takes color tags produced by this tagger and creates corresponding UI for this data.
    /// </para>
    /// <para>
    /// This class is a sample usage of the <see cref="RegexTagger"/> utility base class.
    /// </para>
    /// </remarks>
    internal sealed class ColorTagger : RegexTagger<ColorTag>
    {
        internal ColorTagger(ITextBuffer buffer) : base(buffer, new[] { new Regex(@"\b(0[xX])?([0-9a-fA-F])+\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase) })
        //: base(buffer, new[] { new Regex(@"\b[\dA-F]{6}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase) })
        {
        }

        protected override ColorTag TryCreateTagForMatch(Match match)
        {
            var color = ParseColor(match.ToString());

            if (match.Length == 6 || match.Length == 8)
            {
                return new ColorTag(color);
            }

            return null;
        }

        private static CGColor ParseColor(string hexColor)
        {
            int number;

            //Rule out any any '0x' prefixes
            if (hexColor.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                hexColor = hexColor.Substring(2);
            }

            if (!int.TryParse(hexColor, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out number))
            {
                Debug.Fail("unable to parse " + hexColor);
                return new CGColor(0, 0);
            }

            var r = (byte)(number >> 16);
            var g = (byte)(number >> 8);
            var b = (byte)(number >> 0);
            //return new CGColor(CGColorSpace.CreateDeviceRGB(), new nfloat[] { r/255, g/255, b/255, 1 });
            return new CGColor((nfloat)r/255, (nfloat)g/255, (nfloat)b/255);
        }
    }
}

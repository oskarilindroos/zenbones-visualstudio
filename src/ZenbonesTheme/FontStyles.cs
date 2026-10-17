using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ZenbonesTheme
{
    [Flags]
    internal enum Style
    {
        None = 0,
        Bold = 1,
        Italic = 2,
        // Explicitly not bold/italic, to cancel a style that a classification
        // drawn underneath would otherwise show through.
        NoBold = 4,
        NoItalic = 8,
    }

    internal static partial class FontStyles
    {
        /// <summary>
        /// The zenbones styles for the variant whose editor background is
        /// <paramref name="background"/>, or null if it isn't a zenbones theme.
        /// </summary>
        public static IReadOnlyDictionary<string, Style> ForBackground(Color background)
        {
            int rgb = (background.R << 16) | (background.G << 8) | background.B;
            return s_byBackground.TryGetValue(rgb, out var styles) ? styles : null;
        }
    }
}

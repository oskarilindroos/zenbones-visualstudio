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
        // The last lookup, swapped as one object so concurrent callers never
        // see one variant's background paired with another's styles.
        private static Entry s_last;

        /// <summary>
        /// The zenbones styles for the variant whose editor background is
        /// <paramref name="background"/>, or null if it isn't a zenbones theme.
        /// </summary>
        public static IReadOnlyDictionary<string, Style> ForBackground(Color background)
        {
            int rgb = (background.R << 16) | (background.G << 8) | background.B;
            var last = s_last;
            if (last != null && last.Background == rgb)
            {
                return last.Styles;
            }
            var styles = Create(rgb);
            s_last = new Entry(rgb, styles);
            return styles;
        }

        private sealed class Entry
        {
            public Entry(int background, IReadOnlyDictionary<string, Style> styles)
            {
                Background = background;
                Styles = styles;
            }

            public int Background { get; }

            public IReadOnlyDictionary<string, Style> Styles { get; }
        }
    }
}

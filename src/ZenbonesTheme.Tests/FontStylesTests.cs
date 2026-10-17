using System.Windows.Media;
using Xunit;

namespace ZenbonesTheme.Tests
{
    public class FontStylesTests
    {
        // Normal background of zenbones_dark.
        internal static readonly Color ZenbonesDark = Color.FromRgb(0x1C, 0x19, 0x17);

        [Fact]
        public void KnownBackgroundReturnsThatVariantsStyles()
        {
            var styles = FontStyles.ForBackground(ZenbonesDark);

            Assert.NotNull(styles);
            Assert.Equal(Style.Italic, styles["comment"]);
            Assert.Equal(Style.Bold, styles["keyword"]);
            Assert.Equal(Style.NoBold, styles[ModifierClassification.Name]);
        }

        [Fact]
        public void LookupIgnoresCase()
        {
            // VS classification names are case-insensitive ("keyword" vs "Keyword").
            Assert.Equal(Style.Bold, FontStyles.ForBackground(ZenbonesDark)["KEYWORD"]);
        }

        [Fact]
        public void AlphaIsIgnored()
        {
            var translucent = Color.FromArgb(0x80, ZenbonesDark.R, ZenbonesDark.G, ZenbonesDark.B);
            Assert.NotNull(FontStyles.ForBackground(translucent));
        }

        [Fact]
        public void OtherBackgroundsAreNotZenbones()
        {
            Assert.Null(FontStyles.ForBackground(Color.FromRgb(0x1E, 0x1E, 0x1E))); // VS Dark
            Assert.Null(FontStyles.ForBackground(Colors.White));
        }
    }
}

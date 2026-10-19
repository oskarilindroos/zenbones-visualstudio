using Microsoft.VisualStudio.Settings;
using Moq;
using Xunit;

namespace ZenbonesTheme.Tests
{
    public class OptionsTests
    {
        [Theory]
        [InlineData(true, true, Style.Italic | Style.NoItalic | Style.Bold | Style.NoBold)]
        [InlineData(true, false, Style.Italic | Style.NoItalic)]
        [InlineData(false, true, Style.Bold | Style.NoBold)]
        [InlineData(false, false, Style.None)]
        public void EnabledStylesIncludeCancellingStyles(bool italic, bool bold, object expected)
        {
            // object: Style is internal, so it can't be a public method's parameter type.
            Assert.Equal((Style)expected, Options.EnabledStyles(italic, bold));
        }

        [Fact]
        public void ReadDefaultsToFontStylesOnAndModifierColorsOff()
        {
            // GetBoolean returns its default when the value isn't stored.
            var store = new Mock<SettingsStore>();
            store.Setup(s => s.GetBoolean(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string collection, string name, bool defaultValue) => defaultValue);

            Assert.Equal((true, true, false), Options.Read(store.Object));
        }

        [Fact]
        public void ReadReturnsStoredValues()
        {
            var store = new Mock<SettingsStore>();
            store.Setup(s => s.GetBoolean(Options.Collection, "Italic", true)).Returns(false);
            store.Setup(s => s.GetBoolean(Options.Collection, "Bold", true)).Returns(true);
            store.Setup(s => s.GetBoolean(Options.Collection, "ModifierColors", false)).Returns(true);

            Assert.Equal((false, true, true), Options.Read(store.Object));
        }

        [Fact]
        public void WriteCreatesTheCollectionWhenMissing()
        {
            var store = new Mock<WritableSettingsStore>();
            store.Setup(s => s.CollectionExists(Options.Collection)).Returns(false);

            Options.Write(store.Object, italic: false, bold: true, modifierColors: true);

            store.Verify(s => s.CreateCollection(Options.Collection), Times.Once);
            store.Verify(s => s.SetBoolean(Options.Collection, "Italic", false), Times.Once);
            store.Verify(s => s.SetBoolean(Options.Collection, "Bold", true), Times.Once);
            store.Verify(s => s.SetBoolean(Options.Collection, "ModifierColors", true), Times.Once);
        }

        [Fact]
        public void WriteReusesAnExistingCollection()
        {
            var store = new Mock<WritableSettingsStore>();
            store.Setup(s => s.CollectionExists(Options.Collection)).Returns(true);

            Options.Write(store.Object, italic: true, bold: false, modifierColors: false);

            store.Verify(s => s.CreateCollection(It.IsAny<string>()), Times.Never);
            store.Verify(s => s.SetBoolean(Options.Collection, "Bold", false), Times.Once);
        }
    }
}

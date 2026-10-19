using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Xunit;

namespace ZenbonesTheme.Tests
{
    /// <summary>Registration that VS reads from attributes and would fail silently on.</summary>
    public class RegistrationTests
    {
        [Fact]
        public void PackageRegistersTheOptionsPage()
        {
            var page = typeof(ZenbonesPackage).GetCustomAttribute<ProvideOptionPageAttribute>();

            Assert.Equal(typeof(OptionsPage), page.PageType);
            Assert.Equal("Zenbones", page.CategoryName);
            Assert.Equal("General", page.PageName);
        }

        [Fact]
        public void GuidsAreStable()
        {
            // Changing these would orphan users' stored settings.
            Assert.Equal("c3e8a4f1-5b2d-4c69-a0f7-2d8e6b9c1e45", typeof(ZenbonesPackage).GetCustomAttribute<GuidAttribute>().Value);
            Assert.Equal("6a0f2c9e-3d51-4b7a-8e24-91c5d7f0b3a2", typeof(OptionsPage).GetCustomAttribute<GuidAttribute>().Value);
        }

        [Theory]
        [InlineData(nameof(OptionsPage.Italic))]
        [InlineData(nameof(OptionsPage.Bold))]
        public void OptionsDefaultToOn(string property)
        {
            var info = typeof(OptionsPage).GetProperty(property);
            Assert.Equal(true, info.GetCustomAttribute<DefaultValueAttribute>().Value);
            Assert.Equal("Font styles", info.GetCustomAttribute<CategoryAttribute>().Category);
        }

        [Fact]
        public void ModifierColorsAreOnByDefault()
        {
            // Matches zenbones out of the box; the setting is there to turn it off.
            var info = typeof(OptionsPage).GetProperty(nameof(OptionsPage.ModifierColors));
            Assert.Equal(true, info.GetCustomAttribute<DefaultValueAttribute>().Value);
            Assert.Equal("Syntax", info.GetCustomAttribute<CategoryAttribute>().Category);
        }

        [Fact]
        public void ModifierClassificationHasNoColorsOfItsOwn()
        {
            // Without a zenbones theme the keyword formatting must show through.
            var format = new ModifierClassification.Format();

            Assert.Equal("Keyword - Modifier (Zenbones)", format.DisplayName);
            Assert.Null(format.ForegroundColor);
            Assert.Null(format.ForegroundBrush);
            Assert.Null(format.IsBold);
            Assert.Null(format.IsItalic);
        }

        [Fact]
        public void ModifierFormatIsOrderedAboveKeywords()
        {
            // Formats later in priority order win where classifications overlap.
            var type = typeof(ModifierClassification.Format);
            Assert.Contains(type.GetCustomAttributes<OrderAttribute>(), o => o.After == Priority.High);
            Assert.Equal(
                ModifierClassification.Name,
                type.GetCustomAttribute<ClassificationTypeAttribute>().ClassificationTypeNames);
            Assert.Equal(ModifierClassification.Name, type.GetCustomAttribute<NameAttribute>().Name);
        }

        [Fact]
        public void ModifierTaggerOnlyTagsCSharp()
        {
            var contentTypes = typeof(ModifierTaggerProvider).GetCustomAttributes<ContentTypeAttribute>().ToList();
            Assert.Equal(new[] { "CSharp" }, contentTypes.Select(c => c.ContentTypes));
        }
    }
}

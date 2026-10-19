using System;
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using Xunit;

namespace ZenbonesTheme.Tests
{
    public class FontStylerTests
    {
        private static readonly Color VsDark = Color.FromRgb(0x1E, 0x1E, 0x1E);

        private readonly FakeClassificationFormatMap _formatMap = new FakeClassificationFormatMap();
        private readonly FakeEditorFormatMap _editorFormatMap = new FakeEditorFormatMap();
        private readonly FakeClassificationTypeRegistry _registry =
            new FakeClassificationTypeRegistry("comment", "keyword", ModifierClassification.Name);
        private readonly List<Action> _scheduled = new List<Action>();
        private Style _enabled = Options.EnabledStyles(italic: true, bold: true);

        private FontStyler CreateStyler() =>
            new FontStyler(_formatMap, _editorFormatMap, _registry, () => _enabled, _scheduled.Add);

        private Microsoft.VisualStudio.Text.Formatting.TextFormattingRunProperties Explicit(string name) =>
            _formatMap.GetExplicitTextProperties(_registry[name]);

        private void RunScheduled()
        {
            var actions = _scheduled.ToArray();
            _scheduled.Clear();
            foreach (var action in actions)
            {
                action();
            }
        }

        [Fact]
        public void AppliesZenbonesStyles()
        {
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);

            CreateStyler().Apply();

            Assert.True(Explicit("comment").Italic);
            Assert.True(Explicit("keyword").Bold);
            // Drawn over "keyword", so it must cancel the bold explicitly.
            Assert.False(Explicit(ModifierClassification.Name).BoldEmpty);
            Assert.False(Explicit(ModifierClassification.Name).Bold);
            Assert.False(_formatMap.IsInBatchUpdate);
        }

        [Fact]
        public void LeavesOtherThemesAlone()
        {
            _editorFormatMap.SetBackground(VsDark);

            CreateStyler().Apply();

            Assert.Equal(0, _formatMap.Writes);
        }

        [Fact]
        public void FallsBackToTheDefaultBackground()
        {
            var brush = new SolidColorBrush(FontStylesTests.ZenbonesDark);
            brush.Freeze();
            _formatMap.DefaultTextProperties = _formatMap.DefaultTextProperties.SetBackgroundBrush(brush);

            CreateStyler().Apply();

            Assert.True(Explicit("keyword").Bold);
        }

        [Fact]
        public void ReadsABackgroundColorWhenThereIsNoBrush()
        {
            _editorFormatMap.GetProperties("TextView Background")[EditorFormatDefinition.BackgroundColorId] =
                FontStylesTests.ZenbonesDark;

            CreateStyler().Apply();

            Assert.True(Explicit("keyword").Bold);
        }

        [Fact]
        public void RemovesStylesWhenSwitchingToAnotherTheme()
        {
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            var styler = CreateStyler();
            styler.Apply();

            _editorFormatMap.SetBackground(VsDark);
            styler.Apply();

            Assert.True(Explicit("comment").ItalicEmpty);
            Assert.True(Explicit("keyword").BoldEmpty);
            Assert.True(Explicit(ModifierClassification.Name).BoldEmpty);
        }

        [Fact]
        public void KeepsStylesTheUserSet()
        {
            // The user made keywords bold themselves in Fonts and Colors.
            _formatMap.SetTextProperties(_registry["keyword"], Explicit("keyword").SetBold(true));
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            var styler = CreateStyler();
            styler.Apply();

            _editorFormatMap.SetBackground(VsDark);
            styler.Apply();

            Assert.True(Explicit("keyword").Bold);
        }

        [Fact]
        public void DoesNotClearAStyleThatReplacedOurs()
        {
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            var styler = CreateStyler();
            styler.Apply();
            Assert.True(Explicit("comment").Italic);

            // A rebuild (e.g. a Fonts and Colors edit) replaces our italic with
            // the user's own "not italic".
            _formatMap.SetTextProperties(_registry["comment"], Explicit("comment").ClearItalic().SetItalic(false));
            _editorFormatMap.SetBackground(VsDark);
            styler.Apply();

            Assert.False(Explicit("comment").ItalicEmpty);
            Assert.False(Explicit("comment").Italic);
        }

        [Fact]
        public void ForgetsStylesARebuildDropped()
        {
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            var styler = CreateStyler();
            styler.Apply();

            // The map is rebuilt without our bold, then the theme changes.
            _formatMap.SetTextProperties(_registry["keyword"], Explicit("keyword").ClearBold());
            _editorFormatMap.SetBackground(VsDark);
            var writes = _formatMap.Writes;
            styler.Apply();

            Assert.True(Explicit("keyword").BoldEmpty);
            // Nothing left to clear for keyword; only comment and the modifier
            // still carry our styles.
            Assert.Equal(writes + 2, _formatMap.Writes);
        }

        [Fact]
        public void DoesNotCancelBoldTheUserSetOnModifiers()
        {
            _formatMap.SetTextProperties(_registry[ModifierClassification.Name], Explicit(ModifierClassification.Name).SetBold(true));
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);

            CreateStyler().Apply();

            Assert.True(Explicit(ModifierClassification.Name).Bold);
        }

        [Fact]
        public void HonorsDisabledBold()
        {
            _enabled = Options.EnabledStyles(italic: true, bold: false);
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);

            CreateStyler().Apply();

            Assert.True(Explicit("comment").Italic);
            Assert.True(Explicit("keyword").BoldEmpty);
            Assert.True(Explicit(ModifierClassification.Name).BoldEmpty);
        }

        [Fact]
        public void TurningAStyleOffRemovesIt()
        {
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            var styler = CreateStyler();
            styler.Apply();

            _enabled = Options.EnabledStyles(italic: false, bold: true);
            styler.Apply();

            Assert.True(Explicit("comment").ItalicEmpty);
            Assert.True(Explicit("keyword").Bold);
        }

        [Fact]
        public void ApplyingTwiceChangesNothing()
        {
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            var styler = CreateStyler();
            styler.Apply();
            var writes = _formatMap.Writes;

            styler.Apply();

            Assert.Equal(writes, _formatMap.Writes);
        }

        [Fact]
        public void WritesInOneBatch()
        {
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            var styler = CreateStyler();
            styler.Apply();
            Assert.Equal(1, _formatMap.Batches);

            // Switching away removes everything in one batch too.
            _editorFormatMap.SetBackground(VsDark);
            styler.Apply();
            Assert.Equal(2, _formatMap.Batches);
            Assert.False(_formatMap.IsInBatchUpdate);
        }

        [Fact]
        public void NoOpApplyDoesNotOpenABatch()
        {
            // Ending a batch re-formats every view using the map, so an apply
            // that changes nothing mustn't start one.
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            var styler = CreateStyler();
            styler.Apply();
            var batches = _formatMap.Batches;

            styler.Apply();

            Assert.Equal(batches, _formatMap.Batches);
        }

        [Fact]
        public void OtherThemesNeverOpenABatch()
        {
            _editorFormatMap.SetBackground(VsDark);

            CreateStyler().Apply();

            Assert.Equal(0, _formatMap.Batches);
        }

        [Fact]
        public void SkipsClassificationsThatDontExist()
        {
            // Most of the table is for languages that aren't installed.
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);

            CreateStyler().Apply();

            Assert.Equal(3, _formatMap.CurrentPriorityOrder.Count);
        }

        [Fact]
        public void ReappliesAfterFormatChangesCoalesced()
        {
            _editorFormatMap.SetBackground(VsDark);
            CreateStyler().Apply();

            // A theme switch raises a burst of change events.
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            _editorFormatMap.RaiseChanged();
            _formatMap.RaiseChanged();
            _editorFormatMap.RaiseChanged();

            Assert.Single(_scheduled);
            RunScheduled();
            Assert.True(Explicit("keyword").Bold);

            // Once applied, the next change schedules again.
            _formatMap.RaiseChanged();
            Assert.Single(_scheduled);
        }

        [Fact]
        public void ItsOwnChangesDontScheduleAnotherApply()
        {
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            var styler = CreateStyler();
            _formatMap.RaiseOnWrite = true;

            styler.Apply();

            Assert.Empty(_scheduled);
        }

        [Fact]
        public void ListenerCreatesOneStylerPerFormatMap()
        {
            var mapService = new Mock<IClassificationFormatMapService>();
            mapService.Setup(s => s.GetClassificationFormatMap(It.IsAny<ITextView>())).Returns(_formatMap);
            var editorMapService = new Mock<IEditorFormatMapService>();
            editorMapService.Setup(s => s.GetEditorFormatMap(It.IsAny<ITextView>())).Returns(_editorFormatMap);
            _editorFormatMap.SetBackground(FontStylesTests.ZenbonesDark);
            var listener = new FontStyleListener
            {
                ClassificationFormatMapService = mapService.Object,
                EditorFormatMapService = editorMapService.Object,
                ClassificationTypeRegistry = _registry,
                EnabledStyles = () => _enabled,
                Schedule = _scheduled.Add,
            };

            listener.TextViewCreated(Mock.Of<IWpfTextView>());
            listener.TextViewCreated(Mock.Of<IWpfTextView>());

            Assert.True(Explicit("keyword").Bold);
            Assert.Equal(1, _formatMap.Subscribers);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace ZenbonesTheme.Tests
{
    public class ModifierTaggerTests
    {
        private readonly FakeClassificationTypeRegistry _registry = new FakeClassificationTypeRegistry(
            "keyword", "keyword - control", "class name", "local name", "string", ModifierClassification.Name);

        private readonly Mock<ITextBuffer> _buffer = new Mock<ITextBuffer>();
        private readonly Mock<ITextView> _view = new Mock<ITextView>();
        private readonly Mock<ITagAggregator<IClassificationTag>> _aggregator = new Mock<ITagAggregator<IClassificationTag>>();
        private readonly Mock<IViewTagAggregatorFactoryService> _aggregatorFactory = new Mock<IViewTagAggregatorFactoryService>();
        private ITextSnapshot _snapshot;

        // What Roslyn reports for the text, as (text, classification) pairs.
        private List<(string Text, string Classification)> _roslyn = new List<(string, string)>();

        public ModifierTaggerTests()
        {
            _view.SetupGet(v => v.TextBuffer).Returns(_buffer.Object);
            _view.SetupGet(v => v.Properties).Returns(new PropertyCollection());
            _aggregatorFactory.Setup(f => f.CreateTagAggregator<IClassificationTag>(_view.Object)).Returns(_aggregator.Object);
            _aggregator.Setup(a => a.GetTags(It.IsAny<NormalizedSnapshotSpanCollection>()))
                .Returns(() => RoslynTags());
        }

        private ModifierTaggerProvider Provider() => new ModifierTaggerProvider
        {
            TagAggregatorFactory = _aggregatorFactory.Object,
            ClassificationTypeRegistry = _registry,
        };

        private ITagger<IClassificationTag> CreateTagger() =>
            Provider().CreateTagger<IClassificationTag>(_view.Object, _buffer.Object);

        private void SetText(string text)
        {
            var snapshot = new Mock<ITextSnapshot>();
            snapshot.SetupGet(s => s.Length).Returns(text.Length);
            snapshot.SetupGet(s => s.TextBuffer).Returns(_buffer.Object);
            snapshot.Setup(s => s.GetText(It.IsAny<Span>())).Returns((Span span) => text.Substring(span.Start, span.Length));
            snapshot.Setup(s => s.GetText()).Returns(text);
            _snapshot = snapshot.Object;
        }

        /// <summary>Classifies <paramref name="parts"/>, which are separated by single spaces.</summary>
        private void Classify(params (string Text, string Classification)[] parts)
        {
            _roslyn = parts.ToList();
            SetText(string.Join(" ", parts.Select(p => p.Text)));
        }

        private IEnumerable<IMappingTagSpan<IClassificationTag>> RoslynTags()
        {
            var start = 0;
            foreach (var (text, classification) in _roslyn)
            {
                var span = new SnapshotSpan(_snapshot, start, text.Length);
                start += text.Length + 1;
                if (classification == null)
                {
                    continue;
                }
                var mapping = new Mock<IMappingSpan>();
                mapping.Setup(m => m.GetSpans(It.IsAny<ITextSnapshot>())).Returns(new NormalizedSnapshotSpanCollection(span));
                var tagSpan = new Mock<IMappingTagSpan<IClassificationTag>>();
                tagSpan.SetupGet(t => t.Span).Returns(mapping.Object);
                tagSpan.SetupGet(t => t.Tag).Returns(new ClassificationTag(_registry[classification]));
                yield return tagSpan.Object;
            }
        }

        private List<string> TaggedText(ITagger<IClassificationTag> tagger)
        {
            var all = new NormalizedSnapshotSpanCollection(new SnapshotSpan(_snapshot, 0, _snapshot.Length));
            var tags = tagger.GetTags(all).ToList();
            Assert.All(tags, t => Assert.Equal(ModifierClassification.Name, t.Tag.ClassificationType.Classification));
            return tags.Select(t => t.Span.GetText()).ToList();
        }

        [Fact]
        public void TagsModifiersAndDeclarationKeywords()
        {
            Classify(
                ("public", "keyword"),
                ("static", "keyword"),
                ("async", "keyword"),
                ("class", "keyword"),
                ("Program", "class name"),
                ("namespace", "keyword"),
                ("readonly", "keyword"),
                ("get", "keyword"));

            Assert.Equal(
                new[] { "public", "static", "async", "class", "namespace", "readonly", "get" },
                TaggedText(CreateTagger()));
        }

        [Fact]
        public void LeavesOtherKeywordsAsKeywords()
        {
            // zenbones colors these as Statement/PreProc, matching "keyword".
            Classify(
                ("using", "keyword"),
                ("int", "keyword"),
                ("string", "keyword"),
                ("var", "keyword"),
                ("new", "keyword"),
                ("return", "keyword - control"),
                ("if", "keyword - control"));

            Assert.Empty(TaggedText(CreateTagger()));
        }

        [Fact]
        public void OnlyTagsSpansRoslynCallsKeywords()
        {
            // A local named "set", a string containing "public", and plain text.
            Classify(("set", "local name"), ("public", "string"), ("static", null));

            Assert.Empty(TaggedText(CreateTagger()));
        }

        [Fact]
        public void MatchingIsCaseSensitive()
        {
            // C# keywords are lowercase; "Public" can only be an identifier.
            Classify(("Public", "keyword"));

            Assert.Empty(TaggedText(CreateTagger()));
        }

        [Fact]
        public void IgnoresItselfInTheAggregator()
        {
            // The view's aggregator includes this tagger, so it gets asked again
            // from inside its own GetTags.
            Classify(("public", "keyword"));
            ITagger<IClassificationTag> tagger = null;
            var nested = new List<ITagSpan<IClassificationTag>>();
            _aggregator.Setup(a => a.GetTags(It.IsAny<NormalizedSnapshotSpanCollection>()))
                .Returns((NormalizedSnapshotSpanCollection spans) =>
                {
                    nested.AddRange(tagger.GetTags(spans));
                    return RoslynTags();
                });
            tagger = CreateTagger();

            Assert.Equal(new[] { "public" }, TaggedText(tagger));
            Assert.Empty(nested);
        }

        [Fact]
        public void EmptyRequestReturnsNothing()
        {
            Classify(("public", "keyword"));

            Assert.Empty(CreateTagger().GetTags(new NormalizedSnapshotSpanCollection()));
        }

        [Fact]
        public void ForwardsRoslynChangesOnce()
        {
            Classify(("public", "keyword"));
            var tagger = CreateTagger();
            TaggedText(tagger); // creates the aggregator
            var changed = new Mock<IMappingSpan>();
            changed.Setup(m => m.GetSpans(_buffer.Object))
                .Returns(new NormalizedSnapshotSpanCollection(new SnapshotSpan(_snapshot, 0, 6)));
            var raised = new List<SnapshotSpan>();
            tagger.TagsChanged += (s, e) =>
            {
                raised.Add(e.Span);
                // The aggregator passes our own change straight back.
                _aggregator.Raise(a => a.TagsChanged += null, new TagsChangedEventArgs(changed.Object));
            };

            _aggregator.Raise(a => a.TagsChanged += null, new TagsChangedEventArgs(changed.Object));

            Assert.Equal(new[] { new SnapshotSpan(_snapshot, 0, 6) }, raised);
        }

        [Fact]
        public void AggregatorIsCreatedLazilyAndDisposedOnClose()
        {
            Classify(("public", "keyword"));
            var tagger = CreateTagger();
            _aggregatorFactory.Verify(f => f.CreateTagAggregator<IClassificationTag>(It.IsAny<ITextView>()), Times.Never);

            TaggedText(tagger);
            _view.Raise(v => v.Closed += null, EventArgs.Empty);

            _aggregator.Verify(a => a.Dispose(), Times.Once);
        }

        [Fact]
        public void ProviderReturnsOneTaggerPerView()
        {
            var provider = Provider();

            Assert.Same(
                provider.CreateTagger<IClassificationTag>(_view.Object, _buffer.Object),
                provider.CreateTagger<IClassificationTag>(_view.Object, _buffer.Object));
        }

        [Fact]
        public void ProviderIgnoresProjectedBuffers()
        {
            // e.g. the C# inside a Razor view.
            Assert.Null(Provider().CreateTagger<IClassificationTag>(_view.Object, Mock.Of<ITextBuffer>()));
        }
    }
}

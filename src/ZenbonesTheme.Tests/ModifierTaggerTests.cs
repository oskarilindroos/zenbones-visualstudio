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
            "keyword", "keyword - control", "class name", "local name", "identifier", "string", ModifierClassification.Name);

        private readonly Mock<ITextBuffer> _buffer = new Mock<ITextBuffer>();
        private readonly Mock<ITextView> _view = new Mock<ITextView>();
        private readonly Mock<ITagAggregator<IClassificationTag>> _aggregator = new Mock<ITagAggregator<IClassificationTag>>();
        private readonly Mock<IViewTagAggregatorFactoryService> _aggregatorFactory = new Mock<IViewTagAggregatorFactoryService>();
        private readonly FakeModifierSetting _setting = new FakeModifierSetting(enabled: true);
        private ITextSnapshot _snapshot;

        // What Roslyn reports for the text, as (text, classification) pairs.
        private List<(string Text, string Classification)> _roslyn = new List<(string, string)>();

        public ModifierTaggerTests()
        {
            _view.SetupGet(v => v.TextBuffer).Returns(_buffer.Object);
            _view.SetupGet(v => v.Properties).Returns(new PropertyCollection());
            _buffer.SetupGet(b => b.CurrentSnapshot).Returns(() => _snapshot);
            _aggregatorFactory.Setup(f => f.CreateTagAggregator<IClassificationTag>(_view.Object)).Returns(_aggregator.Object);
            _aggregator.Setup(a => a.GetTags(It.IsAny<NormalizedSnapshotSpanCollection>()))
                .Returns(() => RoslynTags());
        }

        private ModifierTaggerProvider Provider() => new ModifierTaggerProvider
        {
            TagAggregatorFactory = _aggregatorFactory.Object,
            ClassificationTypeRegistry = _registry,
            Setting = _setting,
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
            snapshot.Setup(s => s[It.IsAny<int>()]).Returns((int position) => text[position]);
            snapshot.Setup(s => s.CopyTo(It.IsAny<int>(), It.IsAny<char[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback((int start, char[] destination, int index, int count) => text.CopyTo(start, destination, index, count));
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
        public void DoesNotEchoRoslynChanges()
        {
            // Roslyn's own TagsChanged already makes the view re-query this
            // tagger; forwarding it would classify the same lines twice.
            Classify(("public", "keyword"));
            var tagger = CreateTagger();
            TaggedText(tagger); // creates the aggregator

            _aggregator.VerifyAdd(a => a.TagsChanged += It.IsAny<EventHandler<TagsChangedEventArgs>>(), Times.Never);
            _aggregator.VerifyAdd(a => a.BatchedTagsChanged += It.IsAny<EventHandler<BatchedTagsChangedEventArgs>>(), Times.Never);
        }

        [Fact]
        public void TagsKeywordsOfEveryLength()
        {
            Classify(ModifierKeywords.All.Select(k => (k, "keyword")).ToArray());

            Assert.Equal(ModifierKeywords.All, TaggedText(CreateTagger()));
        }

        [Fact]
        public void IsOffUntilTheSettingIsOn()
        {
            _setting.Enabled = false;
            Classify(("public", "keyword"));

            Assert.Empty(TaggedText(CreateTagger()));
            _aggregatorFactory.Verify(f => f.CreateTagAggregator<IClassificationTag>(It.IsAny<ITextView>()), Times.Never);
        }

        [Fact]
        public void TurningTheSettingOnRefreshesOpenViews()
        {
            _setting.Enabled = false;
            Classify(("public", "keyword"));
            var tagger = CreateTagger();
            var raised = new List<SnapshotSpan>();
            tagger.TagsChanged += (s, e) => raised.Add(e.Span);

            _setting.Enabled = true;

            Assert.Equal(new[] { new SnapshotSpan(_snapshot, 0, _snapshot.Length) }, raised);
            Assert.Equal(new[] { "public" }, TaggedText(tagger));
        }

        [Fact]
        public void TurningTheSettingOffReleasesTheAggregator()
        {
            Classify(("public", "keyword"));
            var tagger = CreateTagger();
            TaggedText(tagger); // creates the aggregator
            var raised = 0;
            tagger.TagsChanged += (s, e) => raised++;

            _setting.Enabled = false;

            _aggregator.Verify(a => a.Dispose(), Times.Once);
            Assert.Equal(1, raised);
            Assert.Empty(TaggedText(tagger));
        }

        [Fact]
        public void OtherSettingChangesDoNothing()
        {
            Classify(("public", "keyword"));
            var tagger = CreateTagger();
            var raised = 0;
            tagger.TagsChanged += (s, e) => raised++;

            _setting.Enabled = true; // unchanged, e.g. the user only toggled italics

            Assert.Equal(0, raised);
        }

        [Fact]
        public void StopsListeningToTheSettingOnClose()
        {
            // The real setting's event is static: a leftover handler would keep
            // the closed view alive.
            CreateTagger();
            Assert.Equal(1, _setting.Subscribers);

            _view.Raise(v => v.Closed += null, EventArgs.Empty);

            Assert.Equal(0, _setting.Subscribers);
        }

        [Fact]
        public void LinesWithoutCandidatesNeverReachTheAggregator()
        {
            Classify(("return", "keyword - control"), ("value", "identifier"), ("+", null), ("publicity", "identifier"));

            Assert.Empty(TaggedText(CreateTagger()));
            _aggregatorFactory.Verify(f => f.CreateTagAggregator<IClassificationTag>(It.IsAny<ITextView>()), Times.Never);
        }

        [Fact]
        public void OnlyCandidateWordsAreSentToTheAggregator()
        {
            Classify(("return", "keyword - control"), ("static", "keyword"), ("x", null));
            NormalizedSnapshotSpanCollection asked = null;
            _aggregator.Setup(a => a.GetTags(It.IsAny<NormalizedSnapshotSpanCollection>()))
                .Returns((NormalizedSnapshotSpanCollection spans) =>
                {
                    asked = spans;
                    return RoslynTags();
                });

            Assert.Equal(new[] { "static" }, TaggedText(CreateTagger()));
            Assert.Equal(new[] { new SnapshotSpan(_snapshot, 7, 6) }, asked);
        }

        [Fact]
        public void FindsAKeywordAcrossTheEdgesOfTheRequest()
        {
            Classify(("public", "keyword"));

            var tags = CreateTagger().GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(_snapshot, 2, 2)));

            Assert.Equal(new[] { "public" }, tags.Select(t => t.Span.GetText()));
        }

        [Fact]
        public void IgnoresVerbatimIdentifiers()
        {
            // "@class" is an identifier, whatever Roslyn says about "class".
            Classify(("@class", "identifier"));

            Assert.Empty(TaggedText(CreateTagger()));
            var tags = CreateTagger().GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(_snapshot, 1, 5)));
            Assert.Empty(tags);
        }

        [Fact]
        public void LongSpansSkipThePreScan()
        {
            var filler = new string('x', ModifierTagger.MaxScan);
            Classify((filler, "identifier"), ("sealed", "keyword"));

            Assert.Equal(new[] { "sealed" }, TaggedText(CreateTagger()));
        }


        [Fact]
        public void IgnoresKeywordsThatOnlyShareAPrefixOrLength()
        {
            // Same length as "public"/"static"/"class", or a prefix of one.
            Classify(("return", "keyword"), ("switch", "keyword"), ("while", "keyword"), ("pub", "keyword"), ("o", "keyword"));

            Assert.Empty(TaggedText(CreateTagger()));
        }

        [Fact]
        public void ReturnsNothingAfterTheViewCloses()
        {
            Classify(("public", "keyword"));
            var tagger = CreateTagger();
            TaggedText(tagger);

            _view.Raise(v => v.Closed += null, EventArgs.Empty);

            Assert.Empty(tagger.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(_snapshot, 0, _snapshot.Length))));
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

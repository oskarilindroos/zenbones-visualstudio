using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace ZenbonesTheme
{
    /// <summary>
    /// Roslyn classifies every C# keyword as "keyword", but zenbones (like the
    /// TextMate scopes storage.modifier / storage.type) styles modifiers and
    /// declaration keywords as types. This re-tags those keyword spans with a
    /// higher-priority classification the theme colors separately.
    /// </summary>
    internal static class ModifierClassification
    {
        public const string Name = "Zenbones Modifier Keyword";

        [Export]
        [Name(Name)]
        internal static ClassificationTypeDefinition Type { get; set; }

        // No colors of its own: without a zenbones theme the underlying
        // keyword formatting shows through unchanged.
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = Name)]
        [Name(Name)]
        [UserVisible(true)]
        [Order(After = Priority.High)]
        internal sealed class Format : ClassificationFormatDefinition
        {
            public Format()
            {
                DisplayName = "Keyword - Modifier (Zenbones)";
            }
        }
    }

    // A view tagger rather than a buffer classifier: Roslyn's classification
    // taggers are view taggers, so a buffer-level aggregator never sees them.
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("CSharp")]
    [TagType(typeof(IClassificationTag))]
    internal sealed class ModifierTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactory { get; set; }

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // Only tag the view's own C# buffer, not buffers projected into it.
            if (textView.TextBuffer != buffer)
            {
                return null;
            }
            return textView.Properties.GetOrCreateSingletonProperty(() => new ModifierTagger(
                textView,
                TagAggregatorFactory,
                ClassificationTypeRegistry.GetClassificationType(ModifierClassification.Name))) as ITagger<T>;
        }
    }

    internal sealed class ModifierTagger : ITagger<IClassificationTag>
    {
        private static readonly HashSet<string> s_keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            // storage.modifier
            "public", "private", "protected", "internal", "static", "readonly", "const",
            "volatile", "abstract", "sealed", "virtual", "override", "extern", "unsafe",
            "async", "partial", "required", "ref", "out", "params",
            // storage.type
            "class", "struct", "interface", "enum", "record", "namespace", "delegate", "event",
            "get", "set", "init", "add", "remove",
        };

        // The aggregator includes this tagger too, so calls back into it from
        // inside GetTags return nothing.
        [ThreadStatic]
        private static bool t_tagging;

        private readonly ITextView _view;
        private readonly IViewTagAggregatorFactoryService _aggregatorFactory;
        private readonly ClassificationTag _modifier;
        private ITagAggregator<IClassificationTag> _aggregator;
        private bool _raising;

        public ModifierTagger(ITextView view, IViewTagAggregatorFactoryService aggregatorFactory, IClassificationType modifier)
        {
            _view = view;
            _aggregatorFactory = aggregatorFactory;
            _modifier = new ClassificationTag(modifier);
            _view.Closed += OnClosed;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var result = new List<ITagSpan<IClassificationTag>>();
            if (t_tagging || spans.Count == 0)
            {
                return result;
            }

            var snapshot = spans[0].Snapshot;
            t_tagging = true;
            try
            {
                foreach (var tagSpan in Aggregator().GetTags(spans))
                {
                    if (!string.Equals(tagSpan.Tag.ClassificationType.Classification, "keyword", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    foreach (var span in tagSpan.Span.GetSpans(snapshot))
                    {
                        if (s_keywords.Contains(span.GetText()))
                        {
                            result.Add(new TagSpan<IClassificationTag>(span, _modifier));
                        }
                    }
                }
            }
            finally
            {
                t_tagging = false;
            }
            return result;
        }

        private ITagAggregator<IClassificationTag> Aggregator()
        {
            if (_aggregator == null)
            {
                // Created lazily: creating it while our provider is still
                // constructing this tagger would recurse into the provider.
                _aggregator = _aggregatorFactory.CreateTagAggregator<IClassificationTag>(_view);
                _aggregator.TagsChanged += OnAggregatorChanged;
            }
            return _aggregator;
        }

        private void OnAggregatorChanged(object sender, TagsChangedEventArgs e)
        {
            // Our own event comes back through the aggregator; don't echo it.
            if (_raising)
            {
                return;
            }
            _raising = true;
            try
            {
                var handler = TagsChanged;
                if (handler != null)
                {
                    foreach (var span in e.Span.GetSpans(_view.TextBuffer))
                    {
                        handler(this, new SnapshotSpanEventArgs(span));
                    }
                }
            }
            finally
            {
                _raising = false;
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.Closed -= OnClosed;
            if (_aggregator != null)
            {
                _aggregator.TagsChanged -= OnAggregatorChanged;
                _aggregator.Dispose();
            }
        }
    }
}

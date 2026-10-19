using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
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

        // Replaced in tests, which don't have a VS shell.
        internal IModifierSetting Setting { get; set; } = ModifierSetting.Instance;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // Only tag the view's own C# buffer, not buffers projected into it.
            if (textView.TextBuffer != buffer)
            {
                return null;
            }
            // Created even while the setting is off, so turning it on applies to
            // open documents. Off, it holds no aggregator and does no work.
            return textView.Properties.GetOrCreateSingletonProperty(() => new ModifierTagger(
                textView,
                TagAggregatorFactory,
                ClassificationTypeRegistry.GetClassificationType(ModifierClassification.Name),
                Setting)) as ITagger<T>;
        }
    }

    internal sealed class ModifierTagger : ITagger<IClassificationTag>
    {
        private static readonly ITagSpan<IClassificationTag>[] s_none = Array.Empty<ITagSpan<IClassificationTag>>();

        // Requested spans are normally single lines. Longer ones skip the
        // pre-scan and go straight to the aggregator, which caps the buffer.
        internal const int MaxScan = 4096;

        // The aggregator includes this tagger too, so calls back into it from
        // inside GetTags return nothing.
        [ThreadStatic]
        private static bool t_tagging;

        // Reused per thread so tagging a line allocates nothing unless it finds
        // a modifier.
        [ThreadStatic]
        private static char[] t_text;

        [ThreadStatic]
        private static List<SnapshotSpan> t_candidates;

        private readonly ITextView _view;
        private readonly IViewTagAggregatorFactoryService _aggregatorFactory;
        private readonly ClassificationTag _modifier;
        private readonly IModifierSetting _setting;
        private ITagAggregator<IClassificationTag> _aggregator;
        private bool _enabled;
        private bool _closed;

        public ModifierTagger(
            ITextView view,
            IViewTagAggregatorFactoryService aggregatorFactory,
            IClassificationType modifier,
            IModifierSetting setting)
        {
            _view = view;
            _aggregatorFactory = aggregatorFactory;
            _modifier = new ClassificationTag(modifier);
            _setting = setting;
            _enabled = setting.Enabled;
            _setting.Changed += OnSettingChanged;
            _view.Closed += OnClosed;
        }

        // Raised only when the setting is toggled. Edits need no event of our
        // own: Roslyn's TagsChanged already makes the view re-query every
        // classification tagger, this one included, for the affected span.
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (!_enabled || _closed || t_tagging || spans.Count == 0)
            {
                return s_none;
            }

            var snapshot = spans[0].Snapshot;
            var candidates = t_candidates ?? (t_candidates = new List<SnapshotSpan>());
            candidates.Clear();
            foreach (var span in spans)
            {
                FindCandidates(span, candidates);
            }
            if (candidates.Count == 0)
            {
                // Most lines: no word that could be a modifier, so Roslyn's
                // taggers are never asked.
                return s_none;
            }

            var text = ScratchBuffer(ModifierKeywords.MaxLength);
            List<ITagSpan<IClassificationTag>> result = null;
            t_tagging = true;
            try
            {
                foreach (var tagSpan in Aggregator().GetTags(new NormalizedSnapshotSpanCollection(candidates)))
                {
                    // Equals rejects on length first, so this is cheap for
                    // every classification but "keyword" itself.
                    if (!string.Equals(tagSpan.Tag.ClassificationType.Classification, "keyword", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    foreach (var span in tagSpan.Span.GetSpans(snapshot))
                    {
                        var length = span.Length;
                        if (length < ModifierKeywords.MinLength || length > ModifierKeywords.MaxLength)
                        {
                            continue;
                        }
                        snapshot.CopyTo(span.Start.Position, text, 0, length);
                        if (ModifierKeywords.IsMatch(text, 0, length))
                        {
                            (result ?? (result = new List<ITagSpan<IClassificationTag>>())).Add(
                                new TagSpan<IClassificationTag>(span, _modifier));
                        }
                    }
                }
            }
            finally
            {
                t_tagging = false;
                candidates.Clear();
            }
            return (IEnumerable<ITagSpan<IClassificationTag>>)result ?? s_none;
        }

        /// <summary>
        /// Adds the whole words in or overlapping <paramref name="span"/> that
        /// spell a modifier. Roslyn still decides whether each is a keyword.
        /// </summary>
        private static void FindCandidates(SnapshotSpan span, List<SnapshotSpan> candidates)
        {
            var snapshot = span.Snapshot;
            if (span.Length > MaxScan)
            {
                candidates.Add(span);
                return;
            }

            // Widen to whole words, so a span starting or ending mid-word
            // neither misses a keyword nor mistakes part of a word for one.
            var start = span.Start.Position;
            var end = span.End.Position;
            for (var n = 0; start > 0 && n <= ModifierKeywords.MaxLength && IsWordChar(snapshot[start - 1]); n++)
            {
                start--;
            }
            for (var n = 0; end < snapshot.Length && n <= ModifierKeywords.MaxLength && IsWordChar(snapshot[end]); n++)
            {
                end++;
            }
            // A word that runs past the widened edges is longer than any keyword.
            var cutAtStart = start > 0 && IsWordChar(snapshot[start - 1]);
            var cutAtEnd = end < snapshot.Length && IsWordChar(snapshot[end]);
            // One more char before, to see a verbatim identifier's "@".
            var verbatimAtStart = start > 0 && snapshot[start - 1] == '@';

            var length = end - start;
            var text = ScratchBuffer(length);
            snapshot.CopyTo(start, text, 0, length);

            var i = 0;
            while (i < length)
            {
                if (!IsWordChar(text[i]))
                {
                    i++;
                    continue;
                }
                var wordStart = i;
                while (i < length && IsWordChar(text[i]))
                {
                    i++;
                }
                if (wordStart == 0 && (cutAtStart || verbatimAtStart))
                {
                    continue;
                }
                if (i == length && cutAtEnd)
                {
                    continue;
                }
                if (wordStart > 0 && text[wordStart - 1] == '@')
                {
                    continue; // @class is an identifier
                }
                if (ModifierKeywords.IsMatch(text, wordStart, i - wordStart))
                {
                    candidates.Add(new SnapshotSpan(snapshot, start + wordStart, i - wordStart));
                }
            }
        }

        private static bool IsWordChar(char c) => c == '_' || char.IsLetterOrDigit(c);

        private static char[] ScratchBuffer(int length)
        {
            var text = t_text;
            if (text == null || text.Length < length)
            {
                // Grows to the longest line tagged, at most MaxScan plus the
                // widened edges.
                t_text = text = new char[Math.Max(length, 256)];
            }
            return text;
        }

        private ITagAggregator<IClassificationTag> Aggregator()
        {
            // Created lazily: creating it while our provider is still
            // constructing this tagger would recurse into the provider. It's
            // also only needed once a line has a candidate modifier.
            return _aggregator ?? (_aggregator = _aggregatorFactory.CreateTagAggregator<IClassificationTag>(_view));
        }

        private void OnSettingChanged(object sender, EventArgs e)
        {
            var enabled = _setting.Enabled;
            if (_closed || enabled == _enabled)
            {
                return;
            }
            _enabled = enabled;
            if (!enabled)
            {
                // Off means off: give back the aggregator and what it holds.
                _aggregator?.Dispose();
                _aggregator = null;
            }
            var snapshot = _view.TextBuffer.CurrentSnapshot;
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.Closed -= OnClosed;
            // The setting's event is static, so staying subscribed would keep
            // every closed C# view alive.
            _setting.Changed -= OnSettingChanged;
            _closed = true;
            _aggregator?.Dispose();
            _aggregator = null;
        }
    }

    /// <summary>
    /// The C# keywords zenbones styles as types: storage.modifier and
    /// storage.type. Matched against a char buffer, without allocating.
    /// </summary>
    internal static class ModifierKeywords
    {
        private static readonly string[] s_all =
        {
            // storage.modifier
            "public", "private", "protected", "internal", "static", "readonly", "const",
            "volatile", "abstract", "sealed", "virtual", "override", "extern", "unsafe",
            "async", "partial", "required", "ref", "out", "params",
            // storage.type
            "class", "struct", "interface", "enum", "record", "namespace", "delegate", "event",
            "get", "set", "init", "add", "remove",
        };

        public static IReadOnlyList<string> All => s_all;

        public static readonly int MinLength = s_all.Min(k => k.Length);
        public static readonly int MaxLength = s_all.Max(k => k.Length);

        // Keywords grouped by length, so a candidate is compared against a
        // handful of words at most.
        private static readonly string[][] s_byLength = Enumerable.Range(0, MaxLength + 1)
            .Select(n => s_all.Where(k => k.Length == n).ToArray())
            .ToArray();

        /// <summary>Whether <paramref name="length"/> chars of <paramref name="text"/> from <paramref name="start"/> are a keyword.</summary>
        public static bool IsMatch(char[] text, int start, int length)
        {
            if (length < MinLength || length > MaxLength)
            {
                return false;
            }
            foreach (var keyword in s_byLength[length])
            {
                var i = 0;
                while (i < length && text[start + i] == keyword[i])
                {
                    i++;
                }
                if (i == length)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

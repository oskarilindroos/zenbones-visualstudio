using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
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

    [Export(typeof(IClassifierProvider))]
    [ContentType("CSharp")]
    internal sealed class ModifierClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassifierAggregatorService ClassifierAggregatorService { get; set; }

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer) =>
            textBuffer.Properties.GetOrCreateSingletonProperty(() => new ModifierClassifier(
                textBuffer,
                ClassifierAggregatorService,
                ClassificationTypeRegistry.GetClassificationType(ModifierClassification.Name)));
    }

    internal sealed class ModifierClassifier : IClassifier
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

        // The aggregator includes this classifier too, so calls back into it
        // from inside GetClassificationSpans return nothing.
        [ThreadStatic]
        private static bool t_classifying;

        private readonly ITextBuffer _buffer;
        private readonly IClassifierAggregatorService _aggregatorService;
        private readonly IClassificationType _modifier;
        private IClassifier _aggregator;
        private bool _raising;

        public ModifierClassifier(ITextBuffer buffer, IClassifierAggregatorService aggregatorService, IClassificationType modifier)
        {
            _buffer = buffer;
            _aggregatorService = aggregatorService;
            _modifier = modifier;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var result = new List<ClassificationSpan>();
            if (t_classifying)
            {
                return result;
            }

            t_classifying = true;
            try
            {
                foreach (var classified in Aggregator().GetClassificationSpans(span))
                {
                    if (string.Equals(classified.ClassificationType.Classification, "keyword", StringComparison.OrdinalIgnoreCase)
                        && s_keywords.Contains(classified.Span.GetText()))
                    {
                        result.Add(new ClassificationSpan(classified.Span, _modifier));
                    }
                }
            }
            finally
            {
                t_classifying = false;
            }
            return result;
        }

        private IClassifier Aggregator()
        {
            if (_aggregator == null)
            {
                // Created lazily: creating it in the constructor would recurse
                // into our own provider before the singleton is stored.
                _aggregator = _aggregatorService.GetClassifier(_buffer);
                _aggregator.ClassificationChanged += OnAggregatorChanged;
            }
            return _aggregator;
        }

        private void OnAggregatorChanged(object sender, ClassificationChangedEventArgs e)
        {
            // Our own event comes back through the aggregator; don't echo it.
            if (_raising)
            {
                return;
            }
            _raising = true;
            try
            {
                ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(e.ChangeSpan));
            }
            finally
            {
                _raising = false;
            }
        }
    }
}

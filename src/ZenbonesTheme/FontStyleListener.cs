using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace ZenbonesTheme
{
    /// <summary>
    /// Gives each editor appearance category (one format map shared by all
    /// views of that kind) a <see cref="FontStyler"/> the first time a view
    /// using it opens.
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("any")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class FontStyleListener : IWpfTextViewCreationListener
    {
        private readonly Dictionary<IClassificationFormatMap, FontStyler> _stylers =
            new Dictionary<IClassificationFormatMap, FontStyler>();

        [Import]
        internal IClassificationFormatMapService ClassificationFormatMapService { get; set; }

        [Import]
        internal IEditorFormatMapService EditorFormatMapService { get; set; }

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry { get; set; }

        // Replaced in tests, which don't have a VS shell.
        internal Func<Style> EnabledStyles { get; set; } = Options.EnabledStylesOnUIThread;
        internal Action<Action> Schedule { get; set; } = FontStyler.ScheduleOnUIThread;

        public void TextViewCreated(IWpfTextView textView)
        {
            var formatMap = ClassificationFormatMapService.GetClassificationFormatMap(textView);
            if (_stylers.ContainsKey(formatMap))
            {
                return;
            }
            var styler = new FontStyler(
                formatMap,
                EditorFormatMapService.GetEditorFormatMap(textView),
                ClassificationTypeRegistry,
                EnabledStyles,
                Schedule);
            _stylers.Add(formatMap, styler);
            styler.Apply();
        }
    }

    /// <summary>
    /// Adds zenbones' bold/italic to one classification format map while a
    /// zenbones theme is active, and takes them away again when it isn't.
    /// </summary>
    internal sealed class FontStyler
    {
        private readonly IClassificationFormatMap _formatMap;
        private readonly IEditorFormatMap _editorFormatMap;
        private readonly IClassificationTypeRegistryService _registry;
        private readonly Func<Style> _enabledStyles;
        private readonly Action<Action> _schedule;

        // Styles this class added (as opposed to ones the user set themselves).
        private readonly Dictionary<IClassificationType, Style> _added =
            new Dictionary<IClassificationType, Style>();

        private bool _applying;
        private bool _pending;

        public FontStyler(
            IClassificationFormatMap formatMap,
            IEditorFormatMap editorFormatMap,
            IClassificationTypeRegistryService registry,
            Func<Style> enabledStyles,
            Action<Action> schedule)
        {
            _formatMap = formatMap;
            _editorFormatMap = editorFormatMap;
            _registry = registry;
            _enabledStyles = enabledStyles;
            _schedule = schedule;

            // Theme switches and Fonts and Colors edits rebuild the format map,
            // dropping our styles, so reapply whenever it changes.
            _formatMap.ClassificationFormatMappingChanged += OnChanged;
            _editorFormatMap.FormatMappingChanged += OnChanged;
            Options.Changed += OnChanged;
        }

        private void OnChanged(object sender, EventArgs e)
        {
            if (_applying || _pending)
            {
                return;
            }
            // Coalesce bursts of change events (a theme switch raises many).
            _pending = true;
            _schedule(() =>
            {
                _pending = false;
                Apply();
            });
        }

        /// <summary>Runs <paramref name="action"/> on the UI thread once the current work yields.</summary>
        public static void ScheduleOnUIThread(Action action)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true);
                action();
            }).FileAndForget("zenbones/fontstyles/apply");
        }

        public void Apply()
        {
            var enabled = _enabledStyles();
            var wanted = enabled == Style.None ? null : FontStyles.ForBackground(Background());

            _applying = true;
            _formatMap.BeginBatchUpdate();
            try
            {
                foreach (var pair in _added.ToList())
                {
                    var owned = StillOwned(pair.Key, pair.Value);
                    if (owned == Style.None)
                    {
                        continue;
                    }
                    var keep = Style.None;
                    if (wanted != null && wanted.TryGetValue(pair.Key.Classification, out var style))
                    {
                        keep = style & enabled;
                    }
                    Remove(pair.Key, owned & ~keep);
                }

                if (wanted != null)
                {
                    foreach (var pair in wanted)
                    {
                        var type = _registry.GetClassificationType(pair.Key);
                        if (type != null)
                        {
                            Add(type, pair.Value & enabled);
                        }
                    }
                }
            }
            finally
            {
                _formatMap.EndBatchUpdate();
                _applying = false;
            }
        }

        /// <summary>
        /// Narrows what we recorded adding to <paramref name="type"/> to what
        /// its explicit properties still show. A theme switch or a Fonts and
        /// Colors edit can rebuild the map without our styles, and whatever is
        /// there now isn't ours to remove.
        /// </summary>
        private Style StillOwned(IClassificationType type, Style recorded)
        {
            var properties = _formatMap.GetExplicitTextProperties(type);
            var present = Style.None;
            if (!properties.BoldEmpty) present |= properties.Bold ? Style.Bold : Style.NoBold;
            if (!properties.ItalicEmpty) present |= properties.Italic ? Style.Italic : Style.NoItalic;

            var owned = recorded & present;
            if (owned == Style.None)
            {
                _added.Remove(type);
            }
            else
            {
                _added[type] = owned;
            }
            return owned;
        }

        private void Add(IClassificationType type, Style style)
        {
            var current = _formatMap.GetTextProperties(type);
            var properties = _formatMap.GetExplicitTextProperties(type);
            var missing = style;
            if (current.Bold) missing &= ~Style.Bold;
            if (current.Italic) missing &= ~Style.Italic;
            // Only cancel styles nobody has set explicitly.
            if (!properties.BoldEmpty) missing &= ~Style.NoBold;
            if (!properties.ItalicEmpty) missing &= ~Style.NoItalic;
            if (missing == Style.None)
            {
                return;
            }

            if ((missing & Style.Bold) != 0) properties = properties.SetBold(true);
            if ((missing & Style.Italic) != 0) properties = properties.SetItalic(true);
            if ((missing & Style.NoBold) != 0) properties = properties.SetBold(false);
            if ((missing & Style.NoItalic) != 0) properties = properties.SetItalic(false);
            _formatMap.SetTextProperties(type, properties);

            _added.TryGetValue(type, out var added);
            _added[type] = added | missing;
        }

        private void Remove(IClassificationType type, Style style)
        {
            if (style == Style.None)
            {
                return;
            }

            var properties = _formatMap.GetExplicitTextProperties(type);
            if ((style & (Style.Bold | Style.NoBold)) != 0) properties = properties.ClearBold();
            if ((style & (Style.Italic | Style.NoItalic)) != 0) properties = properties.ClearItalic();
            _formatMap.SetTextProperties(type, properties);

            var remaining = _added[type] & ~style;
            if (remaining == Style.None)
            {
                _added.Remove(type);
            }
            else
            {
                _added[type] = remaining;
            }
        }

        private Color Background()
        {
            var properties = _editorFormatMap.GetProperties("TextView Background");
            if (properties[EditorFormatDefinition.BackgroundBrushId] is SolidColorBrush brush)
            {
                return brush.Color;
            }
            if (properties[EditorFormatDefinition.BackgroundColorId] is Color color)
            {
                return color;
            }
            return _formatMap.DefaultTextProperties.BackgroundBrush is SolidColorBrush fallback
                ? fallback.Color
                : Colors.Transparent;
        }
    }
}

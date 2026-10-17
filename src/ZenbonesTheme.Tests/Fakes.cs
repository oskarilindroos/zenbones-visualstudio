using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace ZenbonesTheme.Tests
{
    internal sealed class FakeClassificationType : IClassificationType
    {
        public FakeClassificationType(string classification) => Classification = classification;

        public string Classification { get; }

        public IEnumerable<IClassificationType> BaseTypes => Enumerable.Empty<IClassificationType>();

        public bool IsOfType(string type) => string.Equals(type, Classification, StringComparison.OrdinalIgnoreCase);
    }

    internal sealed class FakeClassificationTypeRegistry : IClassificationTypeRegistryService
    {
        private readonly Dictionary<string, IClassificationType> _types =
            new Dictionary<string, IClassificationType>(StringComparer.OrdinalIgnoreCase);

        public FakeClassificationTypeRegistry(params string[] names)
        {
            foreach (var name in names)
            {
                _types[name] = new FakeClassificationType(name);
            }
        }

        public IClassificationType this[string name] => _types[name];

        public IClassificationType GetClassificationType(string type) =>
            _types.TryGetValue(type, out var result) ? result : null;

        public ILayeredClassificationType GetClassificationType(ClassificationLayer layer, string type) =>
            throw new NotSupportedException();

        public IClassificationType CreateClassificationType(string type, IEnumerable<IClassificationType> baseTypes) =>
            throw new NotSupportedException();

        public ILayeredClassificationType CreateClassificationType(
            ClassificationLayer layer,
            string type,
            IEnumerable<IClassificationType> baseTypes) =>
            throw new NotSupportedException();

        public IClassificationType CreateTransientClassificationType(IEnumerable<IClassificationType> baseTypes) =>
            throw new NotSupportedException();

        public IClassificationType CreateTransientClassificationType(params IClassificationType[] baseTypes) =>
            throw new NotSupportedException();
    }

    /// <summary>Stores explicit properties per type, like the real map minus priorities.</summary>
    internal sealed class FakeClassificationFormatMap : IClassificationFormatMap
    {
        private readonly Dictionary<IClassificationType, TextFormattingRunProperties> _explicit =
            new Dictionary<IClassificationType, TextFormattingRunProperties>();

        private EventHandler<EventArgs> _changed;

        public int Subscribers => _changed?.GetInvocationList().Length ?? 0;

        public int Writes { get; private set; }

        // The real map raises ClassificationFormatMappingChanged on every write
        // outside a batch update, and once at the end of one.
        public bool RaiseOnWrite { get; set; }

        public event EventHandler<EventArgs> ClassificationFormatMappingChanged
        {
            add => _changed += value;
            remove => _changed -= value;
        }

        public void RaiseChanged() => _changed?.Invoke(this, EventArgs.Empty);

        public TextFormattingRunProperties DefaultTextProperties { get; set; } =
            TextFormattingRunProperties.CreateTextFormattingRunProperties();

        public ReadOnlyCollection<IClassificationType> CurrentPriorityOrder =>
            new ReadOnlyCollection<IClassificationType>(_explicit.Keys.ToList());

        public bool IsInBatchUpdate { get; private set; }

        public void BeginBatchUpdate() => IsInBatchUpdate = true;

        public void EndBatchUpdate()
        {
            IsInBatchUpdate = false;
            if (RaiseOnWrite)
            {
                RaiseChanged();
            }
        }

        public TextFormattingRunProperties GetExplicitTextProperties(IClassificationType classificationType) =>
            _explicit.TryGetValue(classificationType, out var properties)
                ? properties
                : TextFormattingRunProperties.CreateTextFormattingRunProperties();

        public TextFormattingRunProperties GetTextProperties(IClassificationType classificationType) =>
            GetExplicitTextProperties(classificationType);

        public void SetTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties)
        {
            Writes++;
            _explicit[classificationType] = properties;
            if (RaiseOnWrite && !IsInBatchUpdate)
            {
                RaiseChanged();
            }
        }

        public void SetExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties) =>
            SetTextProperties(classificationType, properties);

        public void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties) =>
            SetTextProperties(classificationType, properties);

        public void AddExplicitTextProperties(
            IClassificationType classificationType,
            TextFormattingRunProperties properties,
            IClassificationType priority) =>
            SetTextProperties(classificationType, properties);

        public string GetEditorFormatMapKey(IClassificationType classificationType) => classificationType.Classification;

        public void SwapPriorities(IClassificationType firstType, IClassificationType secondType) =>
            throw new NotSupportedException();
    }

    internal sealed class FakeEditorFormatMap : IEditorFormatMap
    {
        private readonly Dictionary<string, ResourceDictionary> _properties = new Dictionary<string, ResourceDictionary>();

        public event EventHandler<FormatItemsEventArgs> FormatMappingChanged;

        public bool IsInBatchUpdate { get; private set; }

        public void SetBackground(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            GetProperties("TextView Background")[EditorFormatDefinition.BackgroundBrushId] = brush;
        }

        public void RaiseChanged() =>
            FormatMappingChanged?.Invoke(this, new FormatItemsEventArgs(new ReadOnlyCollection<string>(new List<string>())));

        public ResourceDictionary GetProperties(string key)
        {
            if (!_properties.TryGetValue(key, out var properties))
            {
                _properties[key] = properties = new ResourceDictionary();
            }
            return properties;
        }

        public void AddProperties(string key, ResourceDictionary properties) => _properties[key] = properties;

        public void SetProperties(string key, ResourceDictionary properties) => _properties[key] = properties;

        public void BeginBatchUpdate() => IsInBatchUpdate = true;

        public void EndBatchUpdate() => IsInBatchUpdate = false;
    }
}

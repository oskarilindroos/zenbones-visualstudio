using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace ZenbonesTheme
{
    /// <summary>
    /// User settings, kept in the VS settings store so the editor component can
    /// read them without loading the package (which only loads for the
    /// Tools > Options page).
    /// </summary>
    internal static class Options
    {
        internal const string Collection = "Zenbones";

        private static bool s_loaded;
        private static bool s_italic = true;
        private static bool s_bold = true;
        private static bool s_modifierColors;

        public static event EventHandler Changed;

        public static bool Italic
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                EnsureLoaded();
                return s_italic;
            }
        }

        public static bool Bold
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                EnsureLoaded();
                return s_bold;
            }
        }

        /// <summary>
        /// Whether C# modifiers get their own color. Off by default: it's the
        /// only part of the extension that does work for every line drawn.
        /// </summary>
        public static bool ModifierColors
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                EnsureLoaded();
                return s_modifierColors;
            }
        }

        /// <summary>The styles <see cref="FontStyler"/> may add or cancel.</summary>
        public static Style EnabledStylesOnUIThread()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return EnabledStyles(Italic, Bold);
        }

        internal static Style EnabledStyles(bool italic, bool bold) =>
            (italic ? Style.Italic | Style.NoItalic : Style.None)
            | (bold ? Style.Bold | Style.NoBold : Style.None);

        public static void Save(bool italic, bool bold, bool modifierColors)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Write(
                new ShellSettingsManager(ServiceProvider.GlobalProvider).GetWritableSettingsStore(SettingsScope.UserSettings),
                italic,
                bold,
                modifierColors);

            s_italic = italic;
            s_bold = bold;
            s_modifierColors = modifierColors;
            s_loaded = true;
            Changed?.Invoke(null, EventArgs.Empty);
        }

        private static void EnsureLoaded()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (s_loaded)
            {
                return;
            }
            s_loaded = true;
            (s_italic, s_bold, s_modifierColors) = Read(
                new ShellSettingsManager(ServiceProvider.GlobalProvider).GetReadOnlySettingsStore(SettingsScope.UserSettings));
        }

        internal static (bool Italic, bool Bold, bool ModifierColors) Read(SettingsStore store) =>
            (store.GetBoolean(Collection, nameof(Italic), true),
             store.GetBoolean(Collection, nameof(Bold), true),
             store.GetBoolean(Collection, nameof(ModifierColors), false));

        internal static void Write(WritableSettingsStore store, bool italic, bool bold, bool modifierColors)
        {
            if (!store.CollectionExists(Collection))
            {
                store.CreateCollection(Collection);
            }
            store.SetBoolean(Collection, nameof(Italic), italic);
            store.SetBoolean(Collection, nameof(Bold), bold);
            store.SetBoolean(Collection, nameof(ModifierColors), modifierColors);
        }
    }

    [ComVisible(true)]
    [Guid("6a0f2c9e-3d51-4b7a-8e24-91c5d7f0b3a2")]
    public sealed class OptionsPage : DialogPage
    {
        [Category("Font styles")]
        [DisplayName("Italic")]
        [Description("Use italics where zenbones does, e.g. comments, strings and constants. Only applies while a zenbones theme is active.")]
        [DefaultValue(true)]
        public bool Italic { get; set; } = true;

        [Category("Font styles")]
        [DisplayName("Bold")]
        [Description("Use bold where zenbones does, e.g. keywords and preprocessor directives. Only applies while a zenbones theme is active.")]
        [DefaultValue(true)]
        public bool Bold { get; set; } = true;

        [Category("Syntax")]
        [DisplayName("Color C# modifiers")]
        [Description("Color modifiers and declaration keywords (public, static, class...) as types, like zenbones does in Neovim. Re-checks every line the editor draws, so it's off by default; leave it off on slower or low-memory machines.")]
        [DefaultValue(false)]
        public bool ModifierColors { get; set; }

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Italic = Options.Italic;
            Bold = Options.Bold;
            ModifierColors = Options.ModifierColors;
        }

        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Options.Save(Italic, Bold, ModifierColors);
        }
    }

    /// <summary>The modifier color setting, as the tagger sees it.</summary>
    internal interface IModifierSetting
    {
        bool Enabled { get; }

        event EventHandler Changed;
    }

    internal sealed class ModifierSetting : IModifierSetting
    {
        public static readonly ModifierSetting Instance = new ModifierSetting();

        // Taggers are created on the UI thread; anywhere else, fail safe to off
        // rather than touch the settings store.
        public bool Enabled => ThreadHelper.CheckAccess() && Options.ModifierColors;

        public event EventHandler Changed
        {
            add => Options.Changed += value;
            remove => Options.Changed -= value;
        }
    }
}

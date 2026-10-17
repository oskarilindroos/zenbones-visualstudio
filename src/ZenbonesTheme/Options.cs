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
        private const string Collection = "Zenbones";

        private static bool s_loaded;
        private static bool s_italic = true;
        private static bool s_bold = true;

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

        public static void Save(bool italic, bool bold)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var store = new ShellSettingsManager(ServiceProvider.GlobalProvider)
                .GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!store.CollectionExists(Collection))
            {
                store.CreateCollection(Collection);
            }
            store.SetBoolean(Collection, nameof(Italic), italic);
            store.SetBoolean(Collection, nameof(Bold), bold);

            s_italic = italic;
            s_bold = bold;
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
            var store = new ShellSettingsManager(ServiceProvider.GlobalProvider)
                .GetReadOnlySettingsStore(SettingsScope.UserSettings);
            s_italic = store.GetBoolean(Collection, nameof(Italic), true);
            s_bold = store.GetBoolean(Collection, nameof(Bold), true);
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

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Italic = Options.Italic;
            Bold = Options.Bold;
        }

        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Options.Save(Italic, Bold);
        }
    }
}

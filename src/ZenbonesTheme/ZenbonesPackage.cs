using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace ZenbonesTheme
{
    /// <summary>
    /// Exists only to register the Tools > Options page; font styles are applied
    /// by <see cref="FontStyleListener"/>, which doesn't need the package loaded.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("c3e8a4f1-5b2d-4c69-a0f7-2d8e6b9c1e45")]
    [ProvideOptionPage(typeof(OptionsPage), "Zenbones", "General", 0, 0, true)]
    public sealed class ZenbonesPackage : AsyncPackage
    {
    }
}

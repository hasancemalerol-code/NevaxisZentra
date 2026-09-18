using System.IO;
using System.Reflection;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

public sealed class PackagedRuntimeBundleTests
{
    [Fact]
    public void TryResolveFromDesktopExecutableReturnsNullWithoutManifest()
    {
        var result = PackagedRuntimeBundle.TryResolveFromDesktopExecutable(
            Path.Combine(Path.GetTempPath(), "NevaxisZentra-packaged-test"));

        Assert.Null(result);
    }

    [Fact]
    public void CreateBundleUsesExpectedRelativeLayout()
    {
        var bundleRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var bundle = PackagedRuntimeBundle.Create(bundleRoot);

        Assert.Equal(DesktopRuntimeMode.Packaged, bundle.Mode);
        Assert.Equal(bundleRoot, bundle.BundleRoot);
        Assert.Equal(Path.Combine(bundleRoot, "desktop"), bundle.DesktopDirectory);
        Assert.Equal(Path.Combine(bundleRoot, "api"), bundle.ApiDirectory);
        Assert.Equal(Path.Combine(bundleRoot, "web"), bundle.WebDirectory);
        Assert.Equal(Path.Combine(bundleRoot, "manifests", "runtime-manifest.json"), bundle.ManifestPath);
    }

    [Fact]
    public void TryResolveFromActualRc1BundleDesktopDirectory()
    {
        var desktopDirectory = @"C:\Projects\NevaxisZentra\artifacts\desktop-runtime\NevaxisZentra-RC1\desktop";
        if (!Directory.Exists(desktopDirectory))
        {
            return;
        }

        var result = PackagedRuntimeBundle.TryResolveFromDesktopExecutable(desktopDirectory);

        Assert.NotNull(result);
        Assert.EndsWith("NevaxisZentra-RC1", result!.BundleRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryResolveFromPublishedRc1Dll()
    {
        var publishedDll = @"C:\Projects\NevaxisZentra\artifacts\desktop-runtime\NevaxisZentra-RC1\desktop\NevaxisZentra.dll";
        if (!File.Exists(publishedDll))
        {
            return;
        }

        var desktopDirectory = Path.GetDirectoryName(publishedDll)!;
        var result = PackagedRuntimeBundle.TryResolveFromDesktopExecutable(desktopDirectory);

        Assert.NotNull(result);
        Assert.EndsWith("NevaxisZentra-RC1", result!.BundleRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublishedBundleDllResolvesPackagedRuntime()
    {
        var publishedDll = @"C:\Projects\NevaxisZentra\artifacts\desktop-runtime\NevaxisZentra-RC1\desktop\NevaxisZentra.dll";
        if (!File.Exists(publishedDll))
        {
            return;
        }

        var desktopDirectory = Path.GetDirectoryName(publishedDll)!;
        var assembly = Assembly.LoadFrom(publishedDll);
        var bundleType = assembly.GetType("NevaxisZentra.Desktop.Runtime.PackagedRuntimeBundle", throwOnError: true)!;
        var tryResolve = bundleType.GetMethod(
            "TryResolveFromDesktopExecutable",
            [typeof(string)])!;
        var resolved = tryResolve.Invoke(null, [desktopDirectory]);

        Assert.NotNull(resolved);
        var bundleRoot = bundleType.GetProperty("BundleRoot")!.GetValue(resolved) as string;
        Assert.EndsWith("NevaxisZentra-RC1", bundleRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublishedBundleDllResolvesWithNullDesktopDirectoryArgument()
    {
        var publishedDll = @"C:\Projects\NevaxisZentra\artifacts\desktop-runtime\NevaxisZentra-RC1\desktop\NevaxisZentra.dll";
        if (!File.Exists(publishedDll))
        {
            return;
        }

        var assembly = Assembly.LoadFrom(publishedDll);
        var bundleType = assembly.GetType("NevaxisZentra.Desktop.Runtime.PackagedRuntimeBundle", throwOnError: true)!;
        var tryResolve = bundleType.GetMethod(
            "TryResolveFromDesktopExecutable",
            [typeof(string)])!;
        var resolved = tryResolve.Invoke(null, [null]);

        // Test host AppContext.BaseDirectory is not the packaged desktop folder.
        Assert.Null(resolved);
    }

    [Fact]
    public void TryResolveFromDesktopDirectoryWithTrailingSeparator()
    {
        var desktopDirectory = @"C:\Projects\NevaxisZentra\artifacts\desktop-runtime\NevaxisZentra-RC1\desktop\";
        if (!Directory.Exists(desktopDirectory.TrimEnd('\\')))
        {
            return;
        }

        var result = PackagedRuntimeBundle.TryResolveFromDesktopExecutable(desktopDirectory);

        Assert.NotNull(result);
        Assert.EndsWith("NevaxisZentra-RC1", result!.BundleRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveRuntimeModeDefaultsToDevelopmentWithoutManifest()
    {
        Assert.Equal(DesktopRuntimeMode.Development, RuntimePaths.ResolveRuntimeMode());
    }
}

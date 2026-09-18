using System.IO;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

public sealed class StartupLifecycleTests
{
    [Fact]
    public void ResolveRepositoryRootFindsSolutionFromPublishedArtifactDirectory()
    {
        var repositoryRoot = FindRepositoryRoot();
        var artifactDirectory = Path.Combine(repositoryRoot, "artifacts", "desktop", "crevafactos");
        Directory.CreateDirectory(artifactDirectory);

        var previous = Environment.GetEnvironmentVariable(RuntimePaths.RuntimeRootEnvironmentVariable);
        Environment.SetEnvironmentVariable(RuntimePaths.RuntimeRootEnvironmentVariable, null);

        try
        {
            var resolved = RuntimePaths.TryResolveFromDirectory(artifactDirectory);
            Assert.Equal(repositoryRoot, resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable(RuntimePaths.RuntimeRootEnvironmentVariable, previous);
        }
    }

    [Fact]
    public void ResolveRepositoryRootRejectsConfiguredPathWithoutMarkers()
    {
        var invalidRoot = Path.Combine(Path.GetTempPath(), $"missing-repo-markers-{Guid.NewGuid():N}");
        Directory.CreateDirectory(invalidRoot);
        var previous = Environment.GetEnvironmentVariable(RuntimePaths.RuntimeRootEnvironmentVariable);
        Environment.SetEnvironmentVariable(RuntimePaths.RuntimeRootEnvironmentVariable, invalidRoot);

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => RuntimePaths.ResolveRepositoryRoot());
            Assert.Contains("geçerli bir NevaxisZentra kökü değil", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(RuntimePaths.RuntimeRootEnvironmentVariable, previous);
            Directory.Delete(invalidRoot, recursive: true);
        }
    }

    [Fact]
    public void ResolveRepositoryRootIgnoresKnownVerifierFixtureOverride()
    {
        var repositoryRoot = FindRepositoryRoot();
        var fixtureRoot = Path.Combine(Path.GetTempPath(), "NevaxisZentra-web-readiness-invalid-root");
        Directory.CreateDirectory(fixtureRoot);
        var previous = Environment.GetEnvironmentVariable(RuntimePaths.RuntimeRootEnvironmentVariable);
        Environment.SetEnvironmentVariable(RuntimePaths.RuntimeRootEnvironmentVariable, fixtureRoot);

        try
        {
            Assert.True(RuntimePaths.IsKnownVerifierFixturePath(fixtureRoot));
            var resolved = RuntimePaths.ResolveRepositoryRoot();
            Assert.Equal(repositoryRoot, resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable(RuntimePaths.RuntimeRootEnvironmentVariable, previous);
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }

    [Fact]
    public void IsKnownVerifierFixturePathDetectsWebReadinessNegativeCase()
    {
        var fixture = Path.Combine(Path.GetTempPath(), "NevaxisZentra-web-readiness-invalid-root");
        Assert.True(RuntimePaths.IsKnownVerifierFixturePath(fixture));
        Assert.False(RuntimePaths.IsKnownVerifierFixturePath(FindRepositoryRoot()));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NevaxisZentra.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}

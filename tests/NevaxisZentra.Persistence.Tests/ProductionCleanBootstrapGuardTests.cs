using System.Text.RegularExpressions;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>
/// Source-level guards proving a normal Production host cannot create pilot/demo/test/simulator business records:
/// every dev/simulator seed endpoint refuses outside Development, and the pilot bootstrap + fake transfer adapter are
/// only registered under Development.
/// </summary>
public sealed class ProductionCleanBootstrapGuardTests
{
    private static readonly Regex DevMapPost = new(
        "MapPost\\(\"(?<path>/dev/[^\"]*|/dev-simulator/[^\"]*)\"\\s*,\\s*(?<handler>\\w+)\\)",
        RegexOptions.Compiled);

    [Fact]
    public void EveryDevSeedOrSimulatorEndpointRefusesOutsideDevelopment()
    {
        var apiRoot = Path.Combine(FindRepoRoot(), "apps", "api");
        var checkedHandlers = 0;

        foreach (var file in Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                 && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var source = File.ReadAllText(file);
            foreach (Match match in DevMapPost.Matches(source))
            {
                var handler = match.Groups["handler"].Value;
                var body = ExtractMethodBody(source, handler);
                Assert.True(body is not null, $"Handler {handler} for {match.Groups["path"].Value} not found in {file}.");
                Assert.True(
                    Regex.IsMatch(body!, "!\\s*\\w+\\.IsDevelopment\\(\\)"),
                    $"{match.Groups["path"].Value} ({handler}) must return before doing any work when not IsDevelopment().");
                checkedHandlers++;
            }
        }

        Assert.True(checkedHandlers >= 10, $"Expected to inspect the dev endpoints, inspected {checkedHandlers}.");
    }

    [Fact]
    public void PilotBootstrapAndFakeTransferAdapterAreRegisteredOnlyUnderDevelopment()
    {
        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "apps", "api", "Program.cs"));

        Assert.Contains("ValidateDevelopmentPilotProductionGuard", program, StringComparison.Ordinal);

        var developmentBlock = Regex.Match(
            program,
            "if \\(builder\\.Environment\\.IsDevelopment\\(\\)\\)\\s*\\{(?<body>.*?)\\n\\}",
            RegexOptions.Singleline);
        Assert.True(developmentBlock.Success, "Development-only registration block missing.");
        var body = developmentBlock.Groups["body"].Value;

        Assert.Contains("AddNevaxisZentraDevelopmentPilot(", body, StringComparison.Ordinal);
        Assert.Contains("AddNevaxisZentraDevelopmentPilotHostedBootstrap(", body, StringComparison.Ordinal);
        Assert.Contains("FakeProductionPlanTransferAdapter", body, StringComparison.Ordinal);

        var outside = program.Replace(developmentBlock.Value, string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("AddNevaxisZentraDevelopmentPilot", outside, StringComparison.Ordinal);
        Assert.DoesNotContain("FakeProductionPlanTransferAdapter", outside, StringComparison.Ordinal);
        Assert.Contains("allowSyntheticProviders: builder.Environment.IsDevelopment()", outside, StringComparison.Ordinal);
    }

    [Fact]
    public void HostedPilotBootstrapDoesNothingOutsideDevelopment()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "apps", "api", "Development", "DevelopmentPilotHostedBootstrapService.cs"));
        Assert.Matches("if \\(!environment\\.IsDevelopment\\(\\) \\|\\| !options\\.Value\\.Enabled\\)", source);
    }

    private static string? ExtractMethodBody(string source, string methodName)
    {
        var signature = Regex.Match(source, $"\\b{Regex.Escape(methodName)}\\s*\\(");
        while (signature.Success)
        {
            var open = source.IndexOf('{', signature.Index);
            var arrow = source.IndexOf("=>", signature.Index, StringComparison.Ordinal);
            var semicolon = source.IndexOf(';', signature.Index);
            // Skip the registration call site (ends with ';' before any '{').
            if (open >= 0 && (semicolon < 0 || open < semicolon) && (arrow < 0 || open < arrow))
            {
                var depth = 0;
                for (var i = open; i < source.Length; i++)
                {
                    if (source[i] == '{') depth++;
                    else if (source[i] == '}' && --depth == 0)
                    {
                        return source[open..(i + 1)];
                    }
                }
            }

            signature = signature.NextMatch();
        }

        return null;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NevaxisZentra.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}

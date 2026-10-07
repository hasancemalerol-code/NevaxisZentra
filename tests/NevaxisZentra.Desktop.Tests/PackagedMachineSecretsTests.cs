using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

public sealed class PackagedMachineSecretsTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"zentra-secrets-{Guid.NewGuid():N}");

    public PackagedMachineSecretsTests() => Directory.CreateDirectory(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private string NewPath() => Path.Combine(root, Guid.NewGuid().ToString("N"), "runtime.env.local");

    [Fact]
    public void InitialGenerationCreatesUniqueStrongCredentialsPerMachine()
    {
        var first = NewPath();
        var second = NewPath();

        Assert.True(PackagedMachineSecrets.EnsureCreated(first, null, isDemoChannel: false, []));
        Assert.True(PackagedMachineSecrets.EnsureCreated(second, null, isDemoChannel: false, []));

        var a = PackagedMachineSecrets.Load(first);
        var b = PackagedMachineSecrets.Load(second);
        foreach (var key in new[] { PackagedMachineSecrets.RuntimePasswordKey, PackagedMachineSecrets.MigrationPasswordKey })
        {
            Assert.True(a[key].Length >= 40, "secret must carry at least 240 bits of entropy");
            Assert.NotEqual(a[key], b[key]);
            Assert.Matches("^[A-Za-z0-9_-]+$", a[key]);
        }

        Assert.NotEqual(a[PackagedMachineSecrets.RuntimePasswordKey], a[PackagedMachineSecrets.MigrationPasswordKey]);
        Assert.Equal("false", a["DevelopmentSaasIdentity__Enabled"]);
    }

    [Fact]
    public void ExistingSecretsAreNeverRotatedOnLaterStartups()
    {
        var path = NewPath();
        PackagedMachineSecrets.EnsureCreated(path, null, isDemoChannel: false, []);
        var before = File.ReadAllBytes(path);

        var created = PackagedMachineSecrets.EnsureCreated(path, null, isDemoChannel: false, []);

        Assert.False(created);
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void SecretsFileIsProtectedWithOwnerOnlyAcl()
    {
        var path = NewPath();
        PackagedMachineSecrets.EnsureCreated(path, null, isDemoChannel: false, []);

        var security = new FileInfo(path).GetAccessControl();
        Assert.True(security.AreAccessRulesProtected, "inheritance from the parent folder must be disabled");

        var rules = security.GetAccessRules(true, false, typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>()
            .Select(rule => rule.IdentityReference)
            .ToArray();
        var me = WindowsIdentity.GetCurrent().User!;
        Assert.Contains(me, rules);
        foreach (var broad in new[]
                 {
                     WellKnownSidType.WorldSid,
                     WellKnownSidType.BuiltinUsersSid,
                     WellKnownSidType.AuthenticatedUserSid,
                 })
        {
            Assert.DoesNotContain(new SecurityIdentifier(broad, null), rules);
        }
    }

    [Fact]
    public void DemoChannelTakesOnlyIdentityFromTemplateAndNeverDatabaseCredentials()
    {
        var template = Path.Combine(root, "release-demo-secrets.env");
        File.WriteAllLines(template,
        [
            "NEVAXIS_FACTOS__PERSISTENCE__PASSWORD=TEMPLATE-SHARED-RUNTIME-PASSWORD",
            "NEVAXIS_FACTOS__MIGRATIONS__PASSWORD=TEMPLATE-SHARED-MIGRATION-PASSWORD",
            "DevelopmentSaasIdentity__Enabled=true",
            "DevelopmentSaasIdentity__TenantOwnerPassword=demo-owner",
        ]);
        var path = NewPath();

        PackagedMachineSecrets.EnsureCreated(path, template, isDemoChannel: true, []);

        var content = File.ReadAllText(path);
        Assert.DoesNotContain("TEMPLATE-SHARED", content, StringComparison.Ordinal);
        Assert.Contains("DevelopmentSaasIdentity__TenantOwnerPassword=demo-owner", content, StringComparison.Ordinal);
        Assert.Equal(43, PackagedMachineSecrets.Load(path)[PackagedMachineSecrets.RuntimePasswordKey].Length);
    }

    [Fact]
    public void DemoMfaSecretIsAlwaysValidBase32EvenWhenTheTemplateSecretIsNot()
    {
        var template = Path.Combine(root, "demo-template.env");
        File.WriteAllLines(template,
        [
            "DevelopmentSaasIdentity__TenantOwnerPassword=demo-owner",
            "DevelopmentSaasIdentity__MfaSharedSecret=not+valid/base32==",
        ]);
        var fromTemplate = NewPath();
        var fallback = NewPath();

        PackagedMachineSecrets.EnsureCreated(fromTemplate, template, isDemoChannel: true, []);
        PackagedMachineSecrets.EnsureCreated(fallback, null, isDemoChannel: true, []);

        foreach (var path in new[] { fromTemplate, fallback })
        {
            var secret = PackagedMachineSecrets.Load(path)["DevelopmentSaasIdentity__MfaSharedSecret"];
            Assert.Matches("^[A-Z2-7]{32}$", secret);
            Assert.True(PackagedMachineSecrets.IsValidBase32Secret(secret));
        }
    }

    [Fact]
    public void ValidTemplateMfaSecretIsPreserved()
    {
        var template = Path.Combine(root, "demo-template-valid.env");
        File.WriteAllLines(template, ["DevelopmentSaasIdentity__MfaSharedSecret=JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP"]);
        var path = NewPath();

        PackagedMachineSecrets.EnsureCreated(path, template, isDemoChannel: true, []);

        Assert.Equal("JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP", PackagedMachineSecrets.Load(path)["DevelopmentSaasIdentity__MfaSharedSecret"]);
    }

    [Fact]
    public void Base32ValidationRejectsNonBase32Text()
    {
        Assert.False(PackagedMachineSecrets.IsValidBase32Secret(null));
        Assert.False(PackagedMachineSecrets.IsValidBase32Secret("short"));
        Assert.False(PackagedMachineSecrets.IsValidBase32Secret("abcdefghijklmnopqrstuvwxyz234567"));
        Assert.False(PackagedMachineSecrets.IsValidBase32Secret("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA89"));
    }

    [Fact]
    public void ImportIntoProcessOverridesInheritedValues()
    {
        const string key = "ZENTRA_TEST_IMPORT_KEY";
        Environment.SetEnvironmentVariable(key, "inherited");
        try
        {
            PackagedMachineSecrets.ImportIntoProcess(new Dictionary<string, string> { [key] = "machine" });
            Assert.Equal("machine", Environment.GetEnvironmentVariable(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Fact]
    public void RoleBootstrapStatementEscapesQuotesAndRejectsControlCharacters()
    {
        Assert.Equal(
            "ALTER ROLE \"r\" WITH LOGIN PASSWORD 'a''b'",
            SingleUserPostgresRoleBootstrapper.BuildAlterRole("r", "a'b"));
        Assert.Throws<ArgumentException>(() => SingleUserPostgresRoleBootstrapper.BuildAlterRole("r", "a\nb"));
    }
}

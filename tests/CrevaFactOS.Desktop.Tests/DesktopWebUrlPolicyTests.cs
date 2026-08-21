using CrevaFactOS.Desktop.Security;

namespace CrevaFactOS.Desktop.Tests;

public sealed class DesktopWebUrlPolicyTests
{
    [Fact]
    public void DevelopmentUsesLocalhostDefaultWhenUnset()
    {
        var resolution = DesktopWebUrlPolicy.Resolve(isDevelopment: true, configuredValue: null);

        Assert.Equal(WebUrlValidationResult.Valid, resolution.Result);
        Assert.NotNull(resolution.Url);
        Assert.Equal("http://127.0.0.1:3001/", resolution.Url.AbsoluteUri);
    }

    [Fact]
    public void ProductionRequiresConfiguredHttpsUrl()
    {
        var resolution = DesktopWebUrlPolicy.Resolve(isDevelopment: false, configuredValue: null);

        Assert.Equal(WebUrlValidationResult.MissingInProduction, resolution.Result);
        Assert.Null(resolution.Url);
    }

    [Theory]
    [InlineData("file:///C:/secret")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,test")]
    [InlineData("vbscript:msgbox")]
    [InlineData("custom:payload")]
    public void RejectsUnsafeSchemes(string candidate)
    {
        var resolution = DesktopWebUrlPolicy.Resolve(isDevelopment: true, configuredValue: candidate);

        Assert.Equal(WebUrlValidationResult.UnsafeScheme, resolution.Result);
        Assert.Null(resolution.Url);
    }

    [Fact]
    public void ProductionRejectsLocalhostHttps()
    {
        var resolution = DesktopWebUrlPolicy.Resolve(
            isDevelopment: false,
            configuredValue: "https://127.0.0.1:3001");

        Assert.Equal(WebUrlValidationResult.LocalhostNotAllowedInProduction, resolution.Result);
    }

    [Fact]
    public void ProductionAcceptsConfiguredOrigin()
    {
        var resolution = DesktopWebUrlPolicy.Resolve(
            isDevelopment: false,
            configuredValue: "https://app.crevafactos.example/giris");

        Assert.Equal(WebUrlValidationResult.Valid, resolution.Result);
        Assert.Equal("https://app.crevafactos.example/", resolution.Url!.AbsoluteUri);
    }
}

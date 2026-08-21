using CrevaFactOS.Desktop.Security;

namespace CrevaFactOS.Desktop.Tests;

public sealed class NavigationPolicyTests
{
    private readonly NavigationPolicy _policy = new(new Uri("http://127.0.0.1:3001"));

    [Theory]
    [InlineData("http://127.0.0.1:3001/giris")]
    [InlineData("http://127.0.0.1:3001/dashboard?tab=1#panel")]
    public void AllowsSameOriginNavigation(string target)
    {
        var decision = _policy.Evaluate(target, isUserInitiated: false);

        Assert.Equal(NavigationDecision.AllowInWebView, decision);
    }

    [Fact]
    public void BlocksCrossOriginAutomaticNavigation()
    {
        var decision = _policy.Evaluate("https://example.com/docs", isUserInitiated: false);

        Assert.Equal(NavigationDecision.Block, decision);
    }

    [Fact]
    public void OpensExternalHttpsLinksInSystemBrowserWhenUserInitiated()
    {
        var decision = _policy.Evaluate("https://example.com/docs", isUserInitiated: true);

        Assert.Equal(NavigationDecision.OpenInSystemBrowser, decision);
    }

    [Theory]
    [InlineData("file:///C:/secret")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,test")]
    [InlineData("vbscript:msgbox")]
    [InlineData("custom:payload")]
    public void BlocksUnsafeSchemes(string target)
    {
        var decision = _policy.Evaluate(target, isUserInitiated: true);

        Assert.Equal(NavigationDecision.Block, decision);
    }
}

namespace NevaxisZentra.Desktop.Security;

public enum NavigationDecision
{
    AllowInWebView,
    OpenInSystemBrowser,
    Block,
}

public sealed class NavigationPolicy
{
    private static readonly HashSet<string> BlockedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "file",
        "javascript",
        "data",
        "vbscript",
    };

    private readonly Uri _applicationOrigin;

    public NavigationPolicy(Uri applicationOrigin)
    {
        ArgumentNullException.ThrowIfNull(applicationOrigin);
        _applicationOrigin = DesktopWebUrlPolicy.GetApplicationOrigin(applicationOrigin);
    }

    public Uri ApplicationOrigin => _applicationOrigin;

    public NavigationDecision Evaluate(string? targetUri, bool isUserInitiated)
    {
        if (string.IsNullOrWhiteSpace(targetUri))
        {
            return NavigationDecision.Block;
        }

        if (!Uri.TryCreate(targetUri, UriKind.Absolute, out var uri))
        {
            return NavigationDecision.Block;
        }

        if (BlockedSchemes.Contains(uri.Scheme))
        {
            return NavigationDecision.Block;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return NavigationDecision.Block;
        }

        if (IsSameOrigin(uri, _applicationOrigin))
        {
            return NavigationDecision.AllowInWebView;
        }

        if (isUserInitiated
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return NavigationDecision.OpenInSystemBrowser;
        }

        return NavigationDecision.Block;
    }

    public static bool IsSameOrigin(Uri left, Uri right) =>
        string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.Authority, right.Authority, StringComparison.OrdinalIgnoreCase);
}

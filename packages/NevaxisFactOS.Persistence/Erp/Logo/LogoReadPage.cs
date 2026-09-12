namespace NevaxisFactOS.Persistence.Erp.Logo;

public sealed record LogoReadPageRequest(
    int PageSize,
    string? ContinuationToken = null);

public sealed record LogoReadPageResult<T>(
    IReadOnlyList<T> Items,
    string? ContinuationToken);

public static class LogoReadTransportDefaults
{
    public const int DefaultPageSize = 100;
    public const int MaxPageSize = 500;
}

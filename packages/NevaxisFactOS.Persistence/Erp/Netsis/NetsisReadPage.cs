namespace NevaxisFactOS.Persistence.Erp.Netsis;

public sealed record NetsisReadPageRequest(
    int PageSize,
    string? ContinuationToken = null);

public sealed record NetsisReadPageResult<T>(
    IReadOnlyList<T> Items,
    string? ContinuationToken);

public static class NetsisReadTransportDefaults
{
    public const int DefaultPageSize = 100;
    public const int MaxPageSize = 500;
}

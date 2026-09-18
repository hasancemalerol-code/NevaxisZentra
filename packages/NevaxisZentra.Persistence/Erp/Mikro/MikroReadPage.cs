namespace NevaxisZentra.Persistence.Erp.Mikro;

public sealed record MikroReadPageRequest(
    int PageSize,
    string? ContinuationToken = null);

public sealed record MikroReadPageResult<T>(
    IReadOnlyList<T> Items,
    string? ContinuationToken);

public static class MikroReadTransportDefaults
{
    public const int DefaultPageSize = 100;
    public const int MaxPageSize = 500;
}

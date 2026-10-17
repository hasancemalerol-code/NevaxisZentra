namespace NevaxisZentra.Desktop.Runtime;

/// <summary>How the supervisor reaches PostgreSQL for its own readiness queries.</summary>
internal enum PostgresQueryRoute
{
    /// <summary>The psql.exe shipped inside the installed bundle (customer machines).</summary>
    BundledClient,

    /// <summary>docker exec into the local compose container (repository development only).</summary>
    DevelopmentDockerContainer,
}

/// <summary>
/// Single place that decides the query route. A packaged installation must never depend on Docker, so
/// Packaged mode either uses the bundled client or fails with a clear message, and never selects Docker.
/// </summary>
internal static class PostgresQueryRoutePolicy
{
    public static PostgresQueryRoute Select(DesktopRuntimeMode mode, bool bundledClientAvailable)
    {
        if (bundledClientAvailable)
        {
            return PostgresQueryRoute.BundledClient;
        }

        if (mode == DesktopRuntimeMode.Packaged)
        {
            throw new InvalidOperationException(
                "Paketlenmiş çalışma zamanı için PostgreSQL istemcisi bulunamadı. Kurulum dosyaları eksik görünüyor.");
        }

        return PostgresQueryRoute.DevelopmentDockerContainer;
    }
}

namespace NevaxisFactOS.Persistence.Production;

public static class ProductionExecutionScrapReasons
{
    public const string ShortShot = "short-shot";
    public const string Flash = "flash";
    public const string SinkMark = "sink-mark";
    public const string BurnMark = "burn-mark";
    public const string Warpage = "warpage";
    public const string Discoloration = "discoloration";
    public const string Dimensional = "dimensional";
    public const string Contamination = "contamination";
    public const string StartupScrap = "startup-scrap";
    public const string ProcessInstability = "process-instability";
    public const string Material = "material";
    public const string Mold = "mold";
    public const string Machine = "machine";
    public const string Operator = "operator";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ShortShot,
        Flash,
        SinkMark,
        BurnMark,
        Warpage,
        Discoloration,
        Dimensional,
        Contamination,
        StartupScrap,
        ProcessInstability,
        Material,
        Mold,
        Machine,
        Operator,
        Other,
    };

    public static bool Requires(string reasonCode) => reasonCode == Other;
}

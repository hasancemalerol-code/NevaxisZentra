namespace CrevaFactOS.Persistence.Export;

public static class ExportTransportMode
{
    public const string Road = "road";
    public const string Air = "air";
    public const string Sea = "sea";
    public const string Rail = "rail";
    public const string Courier = "courier";
    public const string Multimodal = "multimodal";
    public const string Other = "other";

    private static readonly HashSet<string> KnownModes =
        new(StringComparer.Ordinal)
        {
            Road,
            Air,
            Sea,
            Rail,
            Courier,
            Multimodal,
            Other,
        };

    public static bool IsKnown(string? value) =>
        value is not null && KnownModes.Contains(value);
}

namespace CrevaFactOS.Persistence.Export;

public static class ExportPackingUnitType
{
    public const string Pallet = "pallet";
    public const string Carton = "carton";
    public const string Crate = "crate";
    public const string ContainerPackage = "container-package";
    public const string Other = "other";

    private static readonly HashSet<string> KnownTypes =
        new(StringComparer.Ordinal)
        {
            Pallet,
            Carton,
            Crate,
            ContainerPackage,
            Other,
        };

    public static bool IsKnown(string? value) =>
        value is not null && KnownTypes.Contains(value);
}

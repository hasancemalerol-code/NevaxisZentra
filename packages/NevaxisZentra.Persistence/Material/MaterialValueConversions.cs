namespace NevaxisZentra.Persistence.Material;

public static class MaterialValueConversions
{
    public static string ToStorage(MaterialCategory value) => value switch
    {
        MaterialCategory.VirginPolymer => "virgin-polymer",
        MaterialCategory.Compound => "compound",
        MaterialCategory.Masterbatch => "masterbatch",
        MaterialCategory.Additive => "additive",
        MaterialCategory.Filler => "filler",
        MaterialCategory.Regrind => "regrind",
        _ => "other",
    };

    public static MaterialCategory CategoryFromStorage(string value) => value switch
    {
        "virgin-polymer" => MaterialCategory.VirginPolymer,
        "compound" => MaterialCategory.Compound,
        "masterbatch" => MaterialCategory.Masterbatch,
        "additive" => MaterialCategory.Additive,
        "filler" => MaterialCategory.Filler,
        "regrind" => MaterialCategory.Regrind,
        _ => MaterialCategory.Other,
    };

    public static string ToStorage(PolymerFamily value) => value switch
    {
        PolymerFamily.Pp => "PP",
        PolymerFamily.Pe => "PE",
        PolymerFamily.Hdpe => "HDPE",
        PolymerFamily.Ldpe => "LDPE",
        PolymerFamily.Abs => "ABS",
        PolymerFamily.Pa => "PA",
        PolymerFamily.Pom => "POM",
        PolymerFamily.Pc => "PC",
        PolymerFamily.Pmma => "PMMA",
        PolymerFamily.Pet => "PET",
        PolymerFamily.Pbt => "PBT",
        PolymerFamily.Ps => "PS",
        PolymerFamily.Tpe => "TPE",
        PolymerFamily.Tpu => "TPU",
        PolymerFamily.Pvc => "PVC",
        PolymerFamily.Other => "other",
        _ => "unknown",
    };

    public static PolymerFamily PolymerFromStorage(string value) => value switch
    {
        "PP" => PolymerFamily.Pp,
        "PE" => PolymerFamily.Pe,
        "HDPE" => PolymerFamily.Hdpe,
        "LDPE" => PolymerFamily.Ldpe,
        "ABS" => PolymerFamily.Abs,
        "PA" => PolymerFamily.Pa,
        "POM" => PolymerFamily.Pom,
        "PC" => PolymerFamily.Pc,
        "PMMA" => PolymerFamily.Pmma,
        "PET" => PolymerFamily.Pet,
        "PBT" => PolymerFamily.Pbt,
        "PS" => PolymerFamily.Ps,
        "TPE" => PolymerFamily.Tpe,
        "TPU" => PolymerFamily.Tpu,
        "PVC" => PolymerFamily.Pvc,
        "other" => PolymerFamily.Other,
        _ => PolymerFamily.Unknown,
    };

    public static string ToStorage(PhysicalForm value) => value switch
    {
        PhysicalForm.Granule => "granule",
        PhysicalForm.Powder => "powder",
        PhysicalForm.Liquid => "liquid",
        PhysicalForm.Flake => "flake",
        PhysicalForm.Pellet => "pellet",
        _ => "other",
    };

    public static PhysicalForm PhysicalFormFromStorage(string value) => value switch
    {
        "granule" => PhysicalForm.Granule,
        "powder" => PhysicalForm.Powder,
        "liquid" => PhysicalForm.Liquid,
        "flake" => PhysicalForm.Flake,
        "pellet" => PhysicalForm.Pellet,
        _ => PhysicalForm.Other,
    };

    public static string ToStorage(MaterialLifecycleStatus value) => value switch
    {
        MaterialLifecycleStatus.Draft => "draft",
        MaterialLifecycleStatus.Active => "active",
        MaterialLifecycleStatus.Blocked => "blocked",
        MaterialLifecycleStatus.Obsolete => "obsolete",
        _ => "draft",
    };

    public static MaterialLifecycleStatus LifecycleFromStorage(string value) => value switch
    {
        "draft" => MaterialLifecycleStatus.Draft,
        "active" => MaterialLifecycleStatus.Active,
        "blocked" => MaterialLifecycleStatus.Blocked,
        "obsolete" => MaterialLifecycleStatus.Obsolete,
        _ => MaterialLifecycleStatus.Draft,
    };

    public static string ToStorage(MaterialSourceType value) => value switch
    {
        MaterialSourceType.Manual => "manual",
        MaterialSourceType.Imported => "imported",
        MaterialSourceType.Erp => "erp",
        MaterialSourceType.DevelopmentPilot => "development-pilot",
        _ => "unknown",
    };

    public static MaterialSourceType SourceFromStorage(string value) => value switch
    {
        "manual" => MaterialSourceType.Manual,
        "imported" => MaterialSourceType.Imported,
        "erp" => MaterialSourceType.Erp,
        "development-pilot" => MaterialSourceType.DevelopmentPilot,
        _ => MaterialSourceType.Unknown,
    };

    public static string ToStorage(MaterialSpecificationSource value) => value switch
    {
        MaterialSpecificationSource.Manual => "manual",
        MaterialSpecificationSource.Imported => "imported",
        MaterialSpecificationSource.DevelopmentPilot => "development-pilot",
        _ => "unknown",
    };

    public static MaterialSpecificationSource SpecificationSourceFromStorage(string value) => value switch
    {
        "manual" => MaterialSpecificationSource.Manual,
        "imported" => MaterialSpecificationSource.Imported,
        "development-pilot" => MaterialSpecificationSource.DevelopmentPilot,
        _ => MaterialSpecificationSource.Unknown,
    };

    public static int LifecycleSortPriority(MaterialLifecycleStatus status) => status switch
    {
        MaterialLifecycleStatus.Active => 0,
        MaterialLifecycleStatus.Draft => 1,
        MaterialLifecycleStatus.Blocked => 2,
        MaterialLifecycleStatus.Obsolete => 3,
        _ => 4,
    };
}

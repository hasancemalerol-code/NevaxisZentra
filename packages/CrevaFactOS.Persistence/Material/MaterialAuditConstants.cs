namespace CrevaFactOS.Persistence.Material;

public static class MaterialAuditActions
{
    public const string Created = "material.created";
    public const string Updated = "material.updated";
    public const string TechnicalSpecificationUpdated = "material.technical.updated";
    public const string LifecycleTransitioned = "material.lifecycle.transitioned";
}

public static class MaterialAuditEventTypes
{
    public const string Created = "material.created";
    public const string Updated = "material.updated";
    public const string TechnicalSpecificationUpdated = "material.technical.updated";
    public const string LifecycleTransitioned = "material.lifecycle.transitioned";
}

public static class MaterialActorDisplayNames
{
    public static string FromSubjectId(string subjectId) =>
        string.IsNullOrWhiteSpace(subjectId) ? "Bilinmeyen" : subjectId;
}

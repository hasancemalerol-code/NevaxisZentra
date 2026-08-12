namespace CrevaFactOS.Persistence.Material;

public static class MaterialAuditActions
{
    public const string Created = "material.created";
    public const string Updated = "material.updated";
    public const string TechnicalSpecificationUpdated = "material.technical.updated";
    public const string LifecycleTransitioned = "material.lifecycle.transitioned";
    public const string LotCreated = "material.lot.created";
    public const string LotReceiptObserved = "material.lot.receipt.observed";
    public const string LotTransferred = "material.lot.transferred";
    public const string LotStatusChanged = "material.lot.status.changed";
    public const string LotQualityRecorded = "material.lot.quality.recorded";
    public const string LotCertificateLinked = "material.lot.certificate.linked";
}

public static class MaterialAuditEventTypes
{
    public const string Created = "material.created";
    public const string Updated = "material.updated";
    public const string TechnicalSpecificationUpdated = "material.technical.updated";
    public const string LifecycleTransitioned = "material.lifecycle.transitioned";
    public const string LotCreated = "material.lot.created";
    public const string LotReceiptObserved = "material.lot.receipt.observed";
    public const string LotTransferred = "material.lot.transferred";
    public const string LotStatusChanged = "material.lot.status.changed";
    public const string LotQualityRecorded = "material.lot.quality.recorded";
    public const string LotCertificateLinked = "material.lot.certificate.linked";
}

public static class MaterialActorDisplayNames
{
    public static string FromSubjectId(string subjectId) =>
        string.IsNullOrWhiteSpace(subjectId) ? "Bilinmeyen" : subjectId;
}

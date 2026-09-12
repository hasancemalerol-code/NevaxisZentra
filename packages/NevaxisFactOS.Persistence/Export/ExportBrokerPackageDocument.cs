namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportBrokerPackageDocument
{
    public Guid Id { get; set; }
    public Guid PackageVersionId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentStatusSnapshot { get; set; } = string.Empty;
    public string DisplayNameSnapshot { get; set; } = string.Empty;
    public bool Included { get; set; }
}

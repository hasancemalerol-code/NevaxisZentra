namespace NevaxisZentra.Persistence.Export;

public static class ExportDocumentType
{
    public const string CommercialInvoice = "commercial-invoice";
    public const string PackingList = "packing-list";
    public const string CustomerPurchaseOrder = "customer-purchase-order";
    public const string CertificateOfOrigin = "certificate-of-origin";
    public const string CirculationDocument = "circulation-document";
    public const string TransportDocument = "transport-document";
    public const string MaterialCertificate = "material-certificate";
    public const string QualityCertificate = "quality-certificate";
    public const string CustomsDocument = "customs-document";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        CommercialInvoice,
        PackingList,
        CustomerPurchaseOrder,
        CertificateOfOrigin,
        CirculationDocument,
        TransportDocument,
        MaterialCertificate,
        QualityCertificate,
        CustomsDocument,
        Other,
    };

    public static bool IsKnown(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value.Trim());
}

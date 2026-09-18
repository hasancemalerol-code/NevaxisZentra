using System.Globalization;
using NevaxisZentra.Persistence.Erp;

namespace NevaxisZentra.Persistence.Procurement;

public static class ProcurementDeliveryRiskCalculator
{
    private static readonly HashSet<string> TerminalOrderStatuses = new(StringComparer.Ordinal)
    {
        ErpPurchaseOrderStatuses.Cancelled,
        ErpPurchaseOrderStatuses.Closed,
        ErpPurchaseOrderStatuses.Received,
    };

    public static ProcurementDeliveryRiskAssessmentResult AssessLine(
        CanonicalErpPurchaseOrder purchaseOrder,
        CanonicalErpPurchaseOrderLine line,
        DateTimeOffset evaluationTimeUtc)
    {
        var drivers = new List<ProcurementDeliveryRiskDriverReadModel>();
        var evidence = new List<ProcurementDeliveryRiskEvidenceReadModel>();

        evidence.Add(new ProcurementDeliveryRiskEvidenceReadModel(
            "purchaseOrderNumber",
            purchaseOrder.PurchaseOrderNumber,
            "erp-purchase-order",
            purchaseOrder.SourceUpdatedAtUtc,
            string.IsNullOrWhiteSpace(purchaseOrder.PurchaseOrderNumber)));

        evidence.Add(new ProcurementDeliveryRiskEvidenceReadModel(
            "orderStatus",
            purchaseOrder.Status,
            "erp-purchase-order",
            purchaseOrder.SourceUpdatedAtUtc,
            string.IsNullOrWhiteSpace(purchaseOrder.Status)));

        evidence.Add(new ProcurementDeliveryRiskEvidenceReadModel(
            "lineNumber",
            line.LineNumber.ToString(CultureInfo.InvariantCulture),
            "erp-purchase-order-line",
            purchaseOrder.SourceUpdatedAtUtc,
            false));

        var headerCommitment = purchaseOrder.ExpectedDeliveryDateUtc;
        var lineCommitment = line.ExpectedDeliveryDateUtc;

        if (headerCommitment.HasValue && lineCommitment.HasValue
            && headerCommitment.Value != lineCommitment.Value)
        {
            drivers.Add(new ProcurementDeliveryRiskDriverReadModel(
                ProcurementDeliveryRiskDriverCodes.CommitmentDateConflict,
                "Satın alma başlığı ve satır teslimat taahhüt tarihleri çakışıyor",
                $"{headerCommitment.Value:O}|{lineCommitment.Value:O}",
                "fail"));
            evidence.Add(new ProcurementDeliveryRiskEvidenceReadModel(
                "headerCommitmentDateUtc",
                headerCommitment.Value.ToString("O"),
                "erp-purchase-order",
                purchaseOrder.SourceUpdatedAtUtc,
                false));
            evidence.Add(new ProcurementDeliveryRiskEvidenceReadModel(
                "lineCommitmentDateUtc",
                lineCommitment.Value.ToString("O"),
                "erp-purchase-order-line",
                purchaseOrder.SourceUpdatedAtUtc,
                false));
            return BuildResult(
                ProcurementDeliveryRiskStatuses.SourceConflict,
                lineCommitment,
                line.OrderedQuantity,
                line.ReceivedQuantity,
                line.OpenQuantity,
                line.Unit,
                purchaseOrder.SourceUpdatedAtUtc,
                ResolveFreshness(purchaseOrder.SourceUpdatedAtUtc, evaluationTimeUtc),
                drivers,
                evidence);
        }

        var commitmentDate = lineCommitment ?? headerCommitment;
        evidence.Add(new ProcurementDeliveryRiskEvidenceReadModel(
            "commitmentDateUtc",
            commitmentDate?.ToString("O"),
            lineCommitment.HasValue ? "erp-purchase-order-line" : "erp-purchase-order",
            purchaseOrder.SourceUpdatedAtUtc,
            !commitmentDate.HasValue));

        evidence.Add(new ProcurementDeliveryRiskEvidenceReadModel(
            "openQuantity",
            line.OpenQuantity?.ToString(CultureInfo.InvariantCulture),
            "erp-purchase-order-line",
            purchaseOrder.SourceUpdatedAtUtc,
            !line.OpenQuantity.HasValue));

        evidence.Add(new ProcurementDeliveryRiskEvidenceReadModel(
            "receivedQuantity",
            line.ReceivedQuantity?.ToString(CultureInfo.InvariantCulture),
            "erp-purchase-order-line",
            purchaseOrder.SourceUpdatedAtUtc,
            !line.ReceivedQuantity.HasValue));

        var freshness = ResolveFreshness(purchaseOrder.SourceUpdatedAtUtc, evaluationTimeUtc);
        evidence.Add(new ProcurementDeliveryRiskEvidenceReadModel(
            "sourceFreshness",
            freshness,
            "erp-purchase-order",
            purchaseOrder.SourceUpdatedAtUtc,
            freshness == ErpSourceFreshness.Unknown));

        if (TerminalOrderStatuses.Contains(purchaseOrder.Status))
        {
            drivers.Add(new ProcurementDeliveryRiskDriverReadModel(
                ProcurementDeliveryRiskDriverCodes.OrderClosedOrCancelled,
                "Satın alma siparişi kapalı, iptal veya tam teslim durumunda",
                purchaseOrder.Status,
                "unknown"));
            return BuildResult(
                ProcurementDeliveryRiskStatuses.InsufficientData,
                commitmentDate,
                line.OrderedQuantity,
                line.ReceivedQuantity,
                line.OpenQuantity,
                line.Unit,
                purchaseOrder.SourceUpdatedAtUtc,
                freshness,
                drivers,
                evidence);
        }

        if (!commitmentDate.HasValue)
        {
            drivers.Add(new ProcurementDeliveryRiskDriverReadModel(
                ProcurementDeliveryRiskDriverCodes.CommitmentDateMissing,
                "Yetkili teslimat taahhüt tarihi bulunamadı",
                purchaseOrder.PurchaseOrderNumber,
                "unknown"));
            return BuildResult(
                ProcurementDeliveryRiskStatuses.InsufficientData,
                null,
                line.OrderedQuantity,
                line.ReceivedQuantity,
                line.OpenQuantity,
                line.Unit,
                purchaseOrder.SourceUpdatedAtUtc,
                freshness,
                drivers,
                evidence);
        }

        if (!line.OpenQuantity.HasValue)
        {
            drivers.Add(new ProcurementDeliveryRiskDriverReadModel(
                ProcurementDeliveryRiskDriverCodes.OpenQuantityUnknown,
                "Açık miktar bilinmiyor",
                null,
                "unknown"));
            return BuildResult(
                ProcurementDeliveryRiskStatuses.InsufficientData,
                commitmentDate,
                line.OrderedQuantity,
                line.ReceivedQuantity,
                null,
                line.Unit,
                purchaseOrder.SourceUpdatedAtUtc,
                freshness,
                drivers,
                evidence);
        }

        if (freshness == ErpSourceFreshness.Unknown)
        {
            drivers.Add(new ProcurementDeliveryRiskDriverReadModel(
                ProcurementDeliveryRiskDriverCodes.ErpSourceUnknown,
                "ERP kaynak güncelleme zamanı bilinmiyor",
                null,
                "unknown"));
            return BuildResult(
                ProcurementDeliveryRiskStatuses.InsufficientData,
                commitmentDate,
                line.OrderedQuantity,
                line.ReceivedQuantity,
                line.OpenQuantity,
                line.Unit,
                purchaseOrder.SourceUpdatedAtUtc,
                freshness,
                drivers,
                evidence);
        }

        if (freshness == ErpSourceFreshness.Stale)
        {
            drivers.Add(new ProcurementDeliveryRiskDriverReadModel(
                ProcurementDeliveryRiskDriverCodes.ErpSourceStale,
                "ERP satın alma kaynağı güncel değil",
                purchaseOrder.SourceUpdatedAtUtc?.ToString("O"),
                "unknown"));
            return BuildResult(
                ProcurementDeliveryRiskStatuses.InsufficientData,
                commitmentDate,
                line.OrderedQuantity,
                line.ReceivedQuantity,
                line.OpenQuantity,
                line.Unit,
                purchaseOrder.SourceUpdatedAtUtc,
                freshness,
                drivers,
                evidence);
        }

        if (line.OpenQuantity.Value <= 0m)
        {
            drivers.Add(new ProcurementDeliveryRiskDriverReadModel(
                ProcurementDeliveryRiskDriverCodes.OnTrackOpenCommitment,
                "Açık miktar kalmadı; teslimat taahhüdü karşılandı",
                line.OpenQuantity.Value.ToString(CultureInfo.InvariantCulture),
                "pass"));
            return BuildResult(
                ProcurementDeliveryRiskStatuses.OnTrack,
                commitmentDate,
                line.OrderedQuantity,
                line.ReceivedQuantity,
                line.OpenQuantity,
                line.Unit,
                purchaseOrder.SourceUpdatedAtUtc,
                freshness,
                drivers,
                evidence);
        }

        if (commitmentDate.Value < evaluationTimeUtc)
        {
            drivers.Add(new ProcurementDeliveryRiskDriverReadModel(
                ProcurementDeliveryRiskDriverCodes.OverdueOpenQuantity,
                "Açık miktar var ve yetkili taahhüt tarihi geçmiş",
                commitmentDate.Value.ToString("O"),
                "fail"));
            return BuildResult(
                ProcurementDeliveryRiskStatuses.Overdue,
                commitmentDate,
                line.OrderedQuantity,
                line.ReceivedQuantity,
                line.OpenQuantity,
                line.Unit,
                purchaseOrder.SourceUpdatedAtUtc,
                freshness,
                drivers,
                evidence);
        }

        if (string.Equals(purchaseOrder.Status, ErpPurchaseOrderStatuses.PartiallyReceived, StringComparison.Ordinal))
        {
            drivers.Add(new ProcurementDeliveryRiskDriverReadModel(
                ProcurementDeliveryRiskDriverCodes.PartialReceiptOpenQuantity,
                "Yetkili kısmi teslim durumu ve açık miktar mevcut",
                purchaseOrder.Status,
                "warn"));
            return BuildResult(
                ProcurementDeliveryRiskStatuses.DeliveryRisk,
                commitmentDate,
                line.OrderedQuantity,
                line.ReceivedQuantity,
                line.OpenQuantity,
                line.Unit,
                purchaseOrder.SourceUpdatedAtUtc,
                freshness,
                drivers,
                evidence);
        }

        drivers.Add(new ProcurementDeliveryRiskDriverReadModel(
            ProcurementDeliveryRiskDriverCodes.OnTrackOpenCommitment,
            "Kaynak veriler tamam; teslimat taahhüdü içinde açık miktar var",
            commitmentDate.Value.ToString("O"),
            "pass"));
        return BuildResult(
            ProcurementDeliveryRiskStatuses.OnTrack,
            commitmentDate,
            line.OrderedQuantity,
            line.ReceivedQuantity,
            line.OpenQuantity,
            line.Unit,
            purchaseOrder.SourceUpdatedAtUtc,
            freshness,
            drivers,
            evidence);
    }

    private static string ResolveFreshness(DateTimeOffset? sourceUpdatedAtUtc, DateTimeOffset now) =>
        ErpSyncRepository.ResolveFreshness(sourceUpdatedAtUtc, now);

    private static ProcurementDeliveryRiskAssessmentResult BuildResult(
        string result,
        DateTimeOffset? commitmentDateUtc,
        decimal? orderedQuantity,
        decimal? receivedQuantity,
        decimal? openQuantity,
        string? unit,
        DateTimeOffset? sourceUpdatedAtUtc,
        string? sourceFreshness,
        IReadOnlyList<ProcurementDeliveryRiskDriverReadModel> drivers,
        IReadOnlyList<ProcurementDeliveryRiskEvidenceReadModel> evidence) =>
        new(
            result,
            commitmentDateUtc,
            orderedQuantity,
            receivedQuantity,
            openQuantity,
            unit,
            sourceUpdatedAtUtc,
            sourceFreshness,
            drivers,
            evidence);
}

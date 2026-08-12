using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrevaFactOS.Persistence.Material;

internal static class MaterialLotIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static string ComputeCreateFingerprint(
        MaterialOperationContext context,
        CreateMaterialLotCommand command,
        string normalizedLotNumber)
    {
        var payload = new
        {
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            materialId = command.MaterialId,
            lotNumberNormalized = normalizedLotNumber,
            supplierLotNumber = NormalizeOptionalString(command.SupplierLotNumber),
            manufacturerLotNumber = NormalizeOptionalString(command.ManufacturerLotNumber),
            supplierReference = NormalizeOptionalString(command.SupplierReference),
            purchaseOrderReference = NormalizeOptionalString(command.PurchaseOrderReference),
            goodsReceiptReference = NormalizeOptionalString(command.GoodsReceiptReference),
            externalSystem = NormalizeOptionalString(command.ExternalSystem),
            externalLotReference = NormalizeOptionalString(command.ExternalLotReference),
            manufactureDate = FormatDate(command.ManufactureDate),
            expiryDate = FormatDate(command.ExpiryDate),
            retestDate = FormatDate(command.RetestDate),
            initialQuantity = command.InitialQuantity,
            unitOfMeasure = NormalizeOptionalString(command.UnitOfMeasure),
            qualityStatus = MaterialLotValueConversions.ToStorage(
                MaterialLotValueConversions.LotQualityStatusFromStorage(command.QualityStatus)),
            storageLocationId = command.StorageLocationId,
            sourceType = MaterialValueConversions.ToStorage(
                MaterialValueConversions.SourceFromStorage(command.SourceType)),
            sourceReference = NormalizeOptionalString(command.SourceReference),
            dataRecordedAtUtc = FormatDateTime(command.DataRecordedAtUtc),
            notes = NormalizeOptionalString(command.Notes),
        };

        return HashPayload(payload);
    }

    internal static string ComputeReceiptFingerprint(
        ObserveMaterialLotReceiptCommand command) =>
        HashPayload(new
        {
            lotId = command.LotId,
            receivedAtUtc = FormatDateTime(command.ReceivedAtUtc),
            observedQuantity = command.ObservedQuantity,
            goodsReceiptReference = NormalizeOptionalString(command.GoodsReceiptReference),
            sourceReference = NormalizeOptionalString(command.SourceReference),
        });

    internal static string ComputeTransferFingerprint(
        TransferMaterialLotCommand command) =>
        HashPayload(new
        {
            lotId = command.LotId,
            targetStorageLocationId = command.TargetStorageLocationId,
            reason = NormalizeOptionalString(command.Reason),
            sourceReference = NormalizeOptionalString(command.SourceReference),
        });

    internal static string ComputeStatusFingerprint(
        TransitionMaterialLotStatusCommand command) =>
        HashPayload(new
        {
            lotId = command.LotId,
            targetStatus = MaterialLotValueConversions.ToStorage(
                MaterialLotValueConversions.LotStatusFromStorage(command.TargetStatus)),
            reason = NormalizeOptionalString(command.Reason),
            sourceReference = NormalizeOptionalString(command.SourceReference),
        });

    internal static string ComputeQualityFingerprint(
        RecordMaterialLotQualityStatusCommand command) =>
        HashPayload(new
        {
            lotId = command.LotId,
            qualityStatus = MaterialLotValueConversions.ToStorage(
                MaterialLotValueConversions.LotQualityStatusFromStorage(command.QualityStatus)),
            reason = NormalizeOptionalString(command.Reason),
            sourceReference = NormalizeOptionalString(command.SourceReference),
        });

    internal static string ComputeCertificateFingerprint(
        LinkMaterialLotCertificateCommand command) =>
        HashPayload(new
        {
            lotId = command.LotId,
            certificateType = MaterialLotValueConversions.ToStorage(
                MaterialLotValueConversions.LotCertificateTypeFromStorage(command.CertificateType)),
            documentReference = command.DocumentReference.Trim(),
            documentNumber = NormalizeOptionalString(command.DocumentNumber),
            issuer = NormalizeOptionalString(command.Issuer),
            issuedDate = FormatDate(command.IssuedDate),
            expiryDate = FormatDate(command.ExpiryDate),
            sourceType = MaterialValueConversions.ToStorage(
                MaterialValueConversions.SourceFromStorage(command.SourceType)),
            sourceReference = NormalizeOptionalString(command.SourceReference),
        });

    internal static bool ReceiptPayloadMatches(
        MaterialLotTraceabilityEvent existing,
        ObserveMaterialLotReceiptCommand command,
        string fingerprint) =>
        existing.PayloadFingerprint == fingerprint
        && existing.Quantity == command.ObservedQuantity
        && existing.EventAtUtc == command.ReceivedAtUtc
        && NullableStringsEqual(existing.SourceReference, command.SourceReference);

    internal static bool TransferPayloadMatches(
        MaterialLotTraceabilityEvent existing,
        TransferMaterialLotCommand command,
        string fingerprint,
        MaterialStorageLocation targetLocation) =>
        existing.PayloadFingerprint == fingerprint
        && existing.ToWarehouseReference == targetLocation.WarehouseCode
        && existing.ToLocationReference == targetLocation.LocationCode
        && NullableStringsEqual(existing.Reason, command.Reason);

    internal static bool StatusPayloadMatches(
        MaterialLotTraceabilityEvent existing,
        TransitionMaterialLotStatusCommand command) =>
        existing.StatusAfter == MaterialLotValueConversions.LotStatusFromStorage(command.TargetStatus)
        && StringsEqual(existing.Reason ?? string.Empty, command.Reason)
        && NullableStringsEqual(existing.SourceReference, command.SourceReference);

    internal static bool QualityPayloadMatches(
        MaterialLotTraceabilityEvent existing,
        RecordMaterialLotQualityStatusCommand command) =>
        existing.QualityStatusAfter == MaterialLotValueConversions.LotQualityStatusFromStorage(command.QualityStatus)
        && NullableStringsEqual(existing.Reason, command.Reason)
        && NullableStringsEqual(existing.SourceReference, command.SourceReference);

    internal static bool CertificatePayloadMatches(
        MaterialLotCertificateReference existing,
        LinkMaterialLotCertificateCommand command,
        string fingerprint) =>
        existing.PayloadFingerprint == fingerprint
        && existing.CertificateType == MaterialLotValueConversions.LotCertificateTypeFromStorage(command.CertificateType)
        && existing.DocumentReference == command.DocumentReference.Trim();

    private static string HashPayload(object payload) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, SerializerOptions))))
            .ToLowerInvariant();

    private static string? NormalizeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FormatDateTime(DateTimeOffset? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture);

    private static string? FormatDate(DateOnly? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture);

    private static bool StringsEqual(string stored, string incoming) =>
        string.Equals(stored.Trim(), incoming.Trim(), StringComparison.Ordinal);

    private static bool NullableStringsEqual(string? stored, string? incoming) =>
        string.Equals(
            NormalizeOptionalString(stored),
            NormalizeOptionalString(incoming),
            StringComparison.Ordinal);
}

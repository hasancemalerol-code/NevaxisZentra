namespace NevaxisFactOS.Persistence.Material;

public static class MaterialLotValidator
{
    public static bool TryNormalizeLotNumber(
        string? lotNumber,
        out string displayLotNumber,
        out string normalizedLotNumber,
        out string? failureReason)
    {
        displayLotNumber = string.Empty;
        normalizedLotNumber = string.Empty;
        failureReason = null;

        if (string.IsNullOrWhiteSpace(lotNumber))
        {
            failureReason = "Lot number is required.";
            return false;
        }

        displayLotNumber = lotNumber.Trim();
        normalizedLotNumber = displayLotNumber.ToUpperInvariant();
        return true;
    }

    public static string? ValidateQuantity(decimal? quantity, string fieldName = "Quantity")
    {
        if (quantity is null)
        {
            return null;
        }

        if (quantity < 0)
        {
            return $"{fieldName} must be non-negative.";
        }

        return null;
    }

    public static string? ValidateUnitOfMeasure(string? unitOfMeasure, string materialUnitOfMeasure)
    {
        if (string.IsNullOrWhiteSpace(unitOfMeasure))
        {
            return "Unit of measure is required.";
        }

        if (!string.Equals(unitOfMeasure.Trim(), materialUnitOfMeasure.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return "Lot unit of measure must match the material unit of measure.";
        }

        return null;
    }

    public static string? ValidateDates(
        DateOnly? manufactureDate,
        DateTimeOffset? receivedAtUtc,
        DateOnly? expiryDate,
        DateOnly? retestDate)
    {
        if (manufactureDate is not null && receivedAtUtc is not null)
        {
            var receivedDate = DateOnly.FromDateTime(receivedAtUtc.Value.UtcDateTime);
            if (manufactureDate > receivedDate)
            {
                return "Manufacture date cannot be after receipt date.";
            }
        }

        if (receivedAtUtc is not null && expiryDate is not null)
        {
            var receivedDate = DateOnly.FromDateTime(receivedAtUtc.Value.UtcDateTime);
            if (receivedDate > expiryDate)
            {
                return "Receipt date cannot be after expiry date.";
            }
        }

        if (retestDate is not null && expiryDate is not null && retestDate > expiryDate)
        {
            return "Retest date cannot be after expiry date.";
        }

        return null;
    }

    public static string? ValidateStorageLocation(
        MaterialStorageLocation? location,
        Guid facilityId)
    {
        if (location is null)
        {
            return "Storage location not found.";
        }

        if (location.FacilityId != facilityId)
        {
            return "Cross-facility location references are not allowed.";
        }

        return null;
    }

    public static string? ValidateTransferTarget(MaterialStorageLocation? targetLocation, Guid facilityId)
    {
        var locationFailure = ValidateStorageLocation(targetLocation, facilityId);
        if (locationFailure is not null)
        {
            return locationFailure;
        }

        if (targetLocation!.IsActive == false)
        {
            return "Inactive storage locations cannot accept transfers.";
        }

        return null;
    }

    public static string? ValidateTransferEndpoints(
        MaterialStorageLocation? sourceLocation,
        MaterialStorageLocation? targetLocation,
        Guid facilityId)
    {
        var sourceFailure = ValidateStorageLocation(sourceLocation, facilityId);
        if (sourceFailure is not null)
        {
            return sourceFailure;
        }

        var targetFailure = ValidateTransferTarget(targetLocation, facilityId);
        if (targetFailure is not null)
        {
            return targetFailure;
        }

        if (sourceLocation!.Id == targetLocation!.Id)
        {
            return "Transfer source and destination cannot be identical.";
        }

        return null;
    }

    public static bool IsExpiryApproaching(DateOnly? expiryDate, DateOnly? retestDate, DateOnly today, int daysThreshold = 30)
    {
        var relevantDate = retestDate ?? expiryDate;
        if (relevantDate is null)
        {
            return false;
        }

        return relevantDate <= today.AddDays(daysThreshold);
    }
}

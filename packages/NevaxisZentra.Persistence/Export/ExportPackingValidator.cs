namespace NevaxisZentra.Persistence.Export;

/// <summary>Pure static packing validation — no I/O.</summary>
public static class ExportPackingValidator
{
    public sealed record ValidationError(string Code, string Message);

    public static IReadOnlyList<ValidationError> ValidateForComplete(
        IReadOnlyList<ExportPackingUnit> units,
        IReadOnlyList<ExportPackingItem> items)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(items);

        var errors = new List<ValidationError>();

        if (units.Count == 0)
        {
            errors.Add(new ValidationError(
                "no-packing-units",
                "At least one packing unit is required before completing packing."));
            return errors;
        }

        var unitCodesSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var unit in units)
        {
            if (!unitCodesSeen.Add(unit.UnitCode))
            {
                errors.Add(new ValidationError(
                    "duplicate-unit-code",
                    $"Packing unit code '{unit.UnitCode}' appears more than once."));
            }

            var unitItems = items.Where(i => i.PackingUnitId == unit.Id).ToList();
            if (unitItems.Count == 0)
            {
                errors.Add(new ValidationError(
                    "unit-without-items",
                    $"Packing unit '{unit.UnitCode}' has no items."));
            }

            foreach (var item in unitItems)
            {
                if (item.Quantity <= 0)
                {
                    errors.Add(new ValidationError(
                        "invalid-quantity",
                        $"Item '{item.ProductCode}' in unit '{unit.UnitCode}' has a quantity ≤ 0."));
                }
            }

            if (unit.NetWeight.HasValue && unit.GrossWeight.HasValue && unit.NetWeight > unit.GrossWeight)
            {
                errors.Add(new ValidationError(
                    "net-exceeds-gross",
                    $"Packing unit '{unit.UnitCode}' has net weight exceeding gross weight."));
            }

            if (unit.GrossWeight.HasValue && unit.GrossWeight < 0)
            {
                errors.Add(new ValidationError(
                    "negative-gross-weight",
                    $"Packing unit '{unit.UnitCode}' has a negative gross weight."));
            }

            if (unit.NetWeight.HasValue && unit.NetWeight < 0)
            {
                errors.Add(new ValidationError(
                    "negative-net-weight",
                    $"Packing unit '{unit.UnitCode}' has a negative net weight."));
            }
        }

        return errors;
    }

    public static bool IsValidUnitType(string? unitType) =>
        ExportPackingUnitType.IsKnown(unitType);
}

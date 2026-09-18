using System.Globalization;

namespace NevaxisZentra.Persistence.Integration.FactoryEventSpine;

public static class FactoryIntegrationUnitNormalizer
{
    private static readonly Dictionary<string, string> KnownUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sec"] = "seconds",
        ["s"] = "seconds",
        ["seconds"] = "seconds",
        ["min"] = "minutes",
        ["minutes"] = "minutes",
        ["kg"] = "kilograms",
        ["kilograms"] = "kilograms",
        ["g"] = "grams",
        ["grams"] = "grams",
        ["c"] = "celsius",
        ["celsius"] = "celsius",
        ["kwh"] = "kilowatt-hours",
    };

    public static FactoryNormalizedMeasurement NormalizeMeasurement(
        string name,
        decimal? numericValue,
        string? textValue,
        string? sourceUnit)
    {
        if (numericValue is null && string.IsNullOrWhiteSpace(textValue))
        {
            return new FactoryNormalizedMeasurement(null, textValue, sourceUnit ?? string.Empty, null, null, true, "missing-value");
        }

        if (string.IsNullOrWhiteSpace(sourceUnit))
        {
            return new FactoryNormalizedMeasurement(numericValue, textValue, string.Empty, null, numericValue, false, null);
        }

        if (!KnownUnits.TryGetValue(sourceUnit.Trim(), out var normalizedUnit))
        {
            return new FactoryNormalizedMeasurement(
                numericValue,
                textValue,
                sourceUnit,
                null,
                null,
                true,
                $"unknown-unit:{sourceUnit}");
        }

        var normalizedValue = numericValue;
        if (name.Contains("cycle", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(normalizedUnit, "seconds", StringComparison.OrdinalIgnoreCase))
        {
            normalizedValue = numericValue;
        }

        return new FactoryNormalizedMeasurement(
            numericValue,
            textValue,
            sourceUnit,
            normalizedUnit,
            normalizedValue,
            false,
            null);
    }

    public static bool TryParseDecimal(string? value, out decimal parsed) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed);
}

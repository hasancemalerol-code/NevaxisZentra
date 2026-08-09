namespace CrevaFactOS.Persistence.Quality;

public static class QualityEvaluationEngine
{
    public static QualityInspectionItemResult EvaluateDefinitionResult(
        QualityInspectionDefinition definition,
        decimal? measuredNumericValue,
        string? measuredTextValue,
        bool? booleanResult)
    {
        return definition.InspectionType switch
        {
            QualityInspectionType.Measurement => EvaluateMeasurement(
                definition.LowerLimit,
                definition.UpperLimit,
                measuredNumericValue),
            QualityInspectionType.Boolean => booleanResult switch
            {
                true => QualityInspectionItemResult.Pass,
                false => QualityInspectionItemResult.Fail,
                _ => QualityInspectionItemResult.Pending,
            },
            QualityInspectionType.Visual or QualityInspectionType.Text =>
                string.IsNullOrWhiteSpace(measuredTextValue)
                    ? QualityInspectionItemResult.Pending
                    : measuredTextValue.Trim().Equals("pass", StringComparison.OrdinalIgnoreCase)
                        || measuredTextValue.Trim().Equals("ok", StringComparison.OrdinalIgnoreCase)
                        ? QualityInspectionItemResult.Pass
                        : measuredTextValue.Trim().Equals("n/a", StringComparison.OrdinalIgnoreCase)
                            || measuredTextValue.Trim().Equals("na", StringComparison.OrdinalIgnoreCase)
                            ? QualityInspectionItemResult.NotApplicable
                            : QualityInspectionItemResult.Fail,
            _ => QualityInspectionItemResult.Pending,
        };
    }

    public static QualityInspectionItemResult EvaluateMeasurement(
        decimal? lowerLimit,
        decimal? upperLimit,
        decimal? measuredValue)
    {
        if (!measuredValue.HasValue)
        {
            return QualityInspectionItemResult.Pending;
        }

        if (lowerLimit.HasValue && measuredValue.Value < lowerLimit.Value)
        {
            return QualityInspectionItemResult.Fail;
        }

        if (upperLimit.HasValue && measuredValue.Value > upperLimit.Value)
        {
            return QualityInspectionItemResult.Fail;
        }

        if (!lowerLimit.HasValue && !upperLimit.HasValue)
        {
            return QualityInspectionItemResult.Pending;
        }

        return QualityInspectionItemResult.Pass;
    }

    public static QualityInspectionOverallResult ComputeOverallResult(
        IEnumerable<(QualityInspectionDefinition Definition, QualityInspectionItemResult Result)> items)
    {
        var materialized = items.ToList();
        var required = materialized.Where(i => i.Definition.Required).ToList();
        if (required.Count == 0)
        {
            return QualityInspectionOverallResult.Pending;
        }

        if (required.Any(i => i.Definition.Critical && i.Result == QualityInspectionItemResult.Fail))
        {
            return QualityInspectionOverallResult.Fail;
        }

        if (required.Any(i => i.Result == QualityInspectionItemResult.Fail))
        {
            return QualityInspectionOverallResult.Fail;
        }

        if (required.Any(i => i.Result == QualityInspectionItemResult.Pending))
        {
            return QualityInspectionOverallResult.Pending;
        }

        return QualityInspectionOverallResult.Pass;
    }

    public static QualityInspectionReleaseStatus ComputeReleaseStatusAfterCompletion(
        QualityInspectionOverallResult overallResult,
        QualityInspectionReleaseStatus current)
    {
        if (current is QualityInspectionReleaseStatus.Released or QualityInspectionReleaseStatus.Rejected)
        {
            return current;
        }

        return overallResult switch
        {
            QualityInspectionOverallResult.Pending => QualityInspectionReleaseStatus.NotReady,
            QualityInspectionOverallResult.Fail => QualityInspectionReleaseStatus.ReadyForReview,
            QualityInspectionOverallResult.Pass => QualityInspectionReleaseStatus.ReadyForReview,
            QualityInspectionOverallResult.Conditional => QualityInspectionReleaseStatus.ReadyForReview,
            _ => QualityInspectionReleaseStatus.NotReady,
        };
    }
}

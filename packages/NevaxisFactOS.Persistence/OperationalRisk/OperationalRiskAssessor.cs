namespace NevaxisFactOS.Persistence.OperationalRisk;

public sealed record OperationalRiskAssessmentResult(
    string AssessmentOutcome,
    int? LikelihoodLevel,
    int? ImpactLevel,
    int? RiskScore,
    string? SeverityBand);

public static class OperationalRiskAssessor
{
    public static OperationalRiskAssessmentResult Assess(
        string evidenceState,
        int? likelihoodLevel,
        int? impactLevel)
    {
        if (string.Equals(evidenceState, OperationalRiskEvidenceStates.SourceConflict, StringComparison.Ordinal))
        {
            return new OperationalRiskAssessmentResult(
                OperationalRiskAssessmentOutcomes.SourceConflict,
                likelihoodLevel,
                impactLevel,
                null,
                null);
        }

        if (string.Equals(evidenceState, OperationalRiskEvidenceStates.InsufficientData, StringComparison.Ordinal)
            || !likelihoodLevel.HasValue
            || !impactLevel.HasValue)
        {
            return new OperationalRiskAssessmentResult(
                OperationalRiskAssessmentOutcomes.InsufficientData,
                likelihoodLevel,
                impactLevel,
                null,
                null);
        }

        var likelihood = likelihoodLevel.Value;
        var impact = impactLevel.Value;

        if (likelihood is < 1 or > 5 || impact is < 1 or > 5)
        {
            return new OperationalRiskAssessmentResult(
                OperationalRiskAssessmentOutcomes.InsufficientData,
                likelihoodLevel,
                impactLevel,
                null,
                null);
        }

        var score = likelihood * impact;
        return new OperationalRiskAssessmentResult(
            OperationalRiskAssessmentOutcomes.Scored,
            likelihood,
            impact,
            score,
            ResolveSeverityBand(score));
    }

    public static string? ResolveSeverityBand(int score) =>
        score switch
        {
            >= 1 and <= 4 => OperationalRiskSeverityBands.Low,
            >= 5 and <= 9 => OperationalRiskSeverityBands.Moderate,
            >= 10 and <= 16 => OperationalRiskSeverityBands.High,
            >= 17 and <= 25 => OperationalRiskSeverityBands.Critical,
            _ => null,
        };
}

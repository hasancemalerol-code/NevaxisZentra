namespace NevaxisFactOS.Persistence.ProductionPlanning;

public static class PlanningLogisticsRiskPolicy
{
    public const int HighSlackThresholdMinutes = 120;
    public const int MediumSlackThresholdMinutes = 480;
    public const int LowSlackThresholdMinutes = 1440;

    public static PlanningLogisticsRiskLevel MapSlackToRisk(int slackMinutes, bool blocking)
    {
        if (blocking && slackMinutes < 0)
        {
            return PlanningLogisticsRiskLevel.Critical;
        }

        if (slackMinutes < 0)
        {
            return PlanningLogisticsRiskLevel.Critical;
        }

        if (slackMinutes <= HighSlackThresholdMinutes)
        {
            return PlanningLogisticsRiskLevel.High;
        }

        if (slackMinutes <= MediumSlackThresholdMinutes)
        {
            return PlanningLogisticsRiskLevel.Medium;
        }

        if (slackMinutes <= LowSlackThresholdMinutes)
        {
            return PlanningLogisticsRiskLevel.Low;
        }

        return PlanningLogisticsRiskLevel.None;
    }

    public static string ToStorage(PlanningLogisticsRiskLevel level) =>
        level switch
        {
            PlanningLogisticsRiskLevel.None => "none",
            PlanningLogisticsRiskLevel.Low => "low",
            PlanningLogisticsRiskLevel.Medium => "medium",
            PlanningLogisticsRiskLevel.High => "high",
            PlanningLogisticsRiskLevel.Critical => "critical",
            _ => throw new InvalidOperationException($"Unknown risk level: {level}"),
        };

    public static PlanningLogisticsRiskLevel FromStorage(string value) =>
        value switch
        {
            "none" => PlanningLogisticsRiskLevel.None,
            "low" => PlanningLogisticsRiskLevel.Low,
            "medium" => PlanningLogisticsRiskLevel.Medium,
            "high" => PlanningLogisticsRiskLevel.High,
            "critical" => PlanningLogisticsRiskLevel.Critical,
            _ => throw new InvalidOperationException($"Unknown risk level storage value: {value}"),
        };
}

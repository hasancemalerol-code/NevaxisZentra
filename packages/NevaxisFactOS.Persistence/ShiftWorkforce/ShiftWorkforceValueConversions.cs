namespace NevaxisFactOS.Persistence.ShiftWorkforce;

public static class ShiftWorkforceValueConversions
{
    public static string ToDisplayAvailabilityState(string state) =>
        state switch
        {
            ShiftWorkforceAvailabilityStates.Available => "Müsait",
            ShiftWorkforceAvailabilityStates.Leave => "İzinli",
            ShiftWorkforceAvailabilityStates.Absent => "Devamsız",
            ShiftWorkforceAvailabilityStates.Unknown => "Bilinmiyor",
            _ => "Bilinmiyor",
        };

    public static string ToDisplayFreshnessState(string state) =>
        state switch
        {
            ShiftWorkforceFreshnessStates.Fresh => "Güncel",
            ShiftWorkforceFreshnessStates.Stale => "Bayat",
            ShiftWorkforceFreshnessStates.Unknown => "Bilinmiyor",
            _ => "Bilinmiyor",
        };

    public static string ToDisplaySourceSystem(string sourceSystem) =>
        sourceSystem switch
        {
            ShiftWorkforceSourceSystems.LocalDevelopmentPilot => "Yerel pilot veri",
            ShiftWorkforceSourceSystems.Logo => "Logo ERP",
            ShiftWorkforceSourceSystems.Mikro => "Mikro ERP",
            ShiftWorkforceSourceSystems.Netsis => "Netsis ERP",
            _ => "Harici kaynak",
        };

    public static string ToDisplayCompetencyLevel(string level) =>
        level switch
        {
            ShiftWorkforceCompetencyLevels.Basic => "Temel",
            ShiftWorkforceCompetencyLevels.Competent => "Yetkin",
            ShiftWorkforceCompetencyLevels.Proficient => "İleri",
            ShiftWorkforceCompetencyLevels.Expert => "Uzman",
            _ => "Bilinmiyor",
        };

    public static string ToDisplayVerificationStatus(string status) =>
        status switch
        {
            ShiftWorkforceVerificationStatuses.Verified => "Doğrulandı",
            ShiftWorkforceVerificationStatuses.PendingVerification => "Doğrulama bekliyor",
            ShiftWorkforceVerificationStatuses.Expired => "Süresi doldu",
            ShiftWorkforceVerificationStatuses.Rejected => "Reddedildi",
            _ => "Bilinmiyor",
        };

    public static bool IsOperationallyAvailable(ShiftWorkforcePerson person) =>
        person.IsActive
        && person.AvailabilityState == ShiftWorkforceAvailabilityStates.Available;

    public static bool RequiresCompetencyAttention(ShiftWorkforceCompetencyAssignment assignment, DateTimeOffset nowUtc)
    {
        if (assignment.VerificationStatus == ShiftWorkforceVerificationStatuses.PendingVerification
            || assignment.VerificationStatus == ShiftWorkforceVerificationStatuses.Expired
            || assignment.VerificationStatus == ShiftWorkforceVerificationStatuses.Rejected)
        {
            return true;
        }

        if (assignment.ValidUntilUtc.HasValue && assignment.ValidUntilUtc.Value <= nowUtc.AddDays(30))
        {
            return true;
        }

        return false;
    }

    public static string BuildCompetencySummary(IReadOnlyList<ShiftWorkforceCompetencyAssignmentReadModel> competencies)
    {
        if (competencies.Count == 0)
        {
            return "Yetkinlik kaydı yok";
        }

        var verifiedCount = competencies.Count(c =>
            c.VerificationStatus == ShiftWorkforceVerificationStatuses.Verified);
        var attentionCount = competencies.Count(c => c.RequiresAttention);

        if (attentionCount > 0)
        {
            return $"{verifiedCount} doğrulandı, {attentionCount} dikkat gerektiriyor";
        }

        return $"{verifiedCount} doğrulandı";
    }
}

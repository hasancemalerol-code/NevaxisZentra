using NevaxisZentra.Persistence.DigitalWorkforce;

namespace NevaxisZentra.Persistence.Tests;

public sealed class WorksExperienceProjectionTests
{
    [Fact]
    public void MapDisplayStateUsesTurkishUserFacingLabels()
    {
        var detected = CreateCase(DigitalWorkCaseStatuses.Detected);
        Assert.Equal(WorksDisplayStates.New, WorksExperienceProjection.MapDisplayState(detected));

        var approval = CreateCase(DigitalWorkCaseStatuses.AwaitingApproval);
        Assert.Equal(WorksDisplayStates.AwaitingApproval, WorksExperienceProjection.MapDisplayState(approval));

        var verifying = CreateCase(DigitalWorkCaseStatuses.Verifying);
        Assert.Equal(WorksDisplayStates.Verifying, WorksExperienceProjection.MapDisplayState(verifying));

        var observing = CreateCase(DigitalWorkCaseStatuses.Executing);
        Assert.Equal(WorksDisplayStates.Observing, WorksExperienceProjection.MapDisplayState(observing, "OBSERVING"));
    }

    [Fact]
    public void BlockedCaseMapsToEngellendi()
    {
        var blocked = CreateCase(DigitalWorkCaseStatuses.Executing);
        blocked.CurrentBlockerCode = "dependency-wait";
        Assert.Equal(WorksDisplayStates.Blocked, WorksExperienceProjection.MapDisplayState(blocked));
    }

    [Fact]
    public void QueueFiltersMatchSpecification()
    {
        var now = DateTimeOffset.UtcNow;
        var blocked = CreateCase(DigitalWorkCaseStatuses.Executing);
        blocked.CurrentBlockerCode = "approval-wait";

        Assert.True(WorksExperienceProjection.MatchesQueue(blocked, WorksExperienceQueues.Blocked, now, false));
        Assert.False(WorksExperienceProjection.MatchesQueue(blocked, WorksExperienceQueues.CompletedToday, now, false));
    }

    [Fact]
    public void VerifiedSuccessOutcomeLabelIsUserFacing()
    {
        var label = WorksExperienceProjection.MapOutcomeLabel("VERIFIED_SUCCESS", DigitalWorkVerificationResults.Passed);
        Assert.Equal("DOĞRULANMIŞ İYİLEŞME", label);
    }

    [Fact]
    public void TimelineIncludesCreationAndClosureWhenPresent()
    {
        var workCase = CreateCase(DigitalWorkCaseStatuses.Closed);
        workCase.CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-2);
        workCase.ClosedAtUtc = DateTimeOffset.UtcNow;
        workCase.Outcome = "VERIFIED_SUCCESS";

        var timeline = WorksExperienceProjection.BuildTimeline(workCase);
        Assert.Contains(timeline, entry => entry.Code == "created");
        Assert.Contains(timeline, entry => entry.Code == "closed");
    }

    [Fact]
    public void CompletedTodayMatchesVerifiedClosureWithoutClosedAt()
    {
        var now = DateTimeOffset.UtcNow;
        var workCase = CreateCase(DigitalWorkCaseStatuses.Closed);
        workCase.VerifiedAtUtc = now;
        workCase.VerificationState = DigitalWorkVerificationResults.Passed;

        Assert.True(WorksExperienceProjection.MatchesQueue(
            workCase,
            WorksExperienceQueues.CompletedToday,
            now,
            false));
    }

    [Fact]
    public void ParseVerificationSummaryUsesPassedStateWhenOutcomeMissing()
    {
        var workCase = CreateCase(DigitalWorkCaseStatuses.Closed);
        workCase.Outcome = string.Empty;
        workCase.VerificationState = DigitalWorkVerificationResults.Passed;
        workCase.VerifiedAtUtc = DateTimeOffset.UtcNow;

        var summary = WorksExperienceProjection.ParseVerificationSummary(workCase);
        Assert.NotNull(summary);
        Assert.Equal("DOĞRULANMIŞ İYİLEŞME", summary!.OutcomeLabel);
    }

    private static DigitalWorkCase CreateCase(string status) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Title = "Assembly bottleneck recovery",
        Description = "Throughput below target",
        Status = status,
        CurrentStage = DigitalWorkCaseStages.Act,
        Severity = "high",
        PrimaryWorkerId = Guid.NewGuid(),
        VerificationState = DigitalWorkVerificationResults.Pending,
        CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
        UpdatedAtUtc = DateTimeOffset.UtcNow,
        CurrentBlockerCode = "none",
        Approvals = [],
        Activities = [],
        Verifications = [],
        Handoffs = [],
        Participants = [],
    };
}

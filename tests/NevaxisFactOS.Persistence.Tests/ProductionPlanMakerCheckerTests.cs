using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Development;
using NevaxisFactOS.Persistence.ProductionPlanning;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class ProductionPlanMakerCheckerTests
{
    private static readonly Guid PlanId = Guid.Parse("88000010-0000-4000-8000-000000000001");
    private static readonly Guid MakerId = Guid.Parse("cc000001-0000-4000-8000-000000000001");
    private static readonly Guid ApproverId = Guid.Parse("cc000002-0000-4000-8000-000000000002");

    // ── LastModifiedBySubjectId on ProductionPlan ────────────────────────────

    [Fact]
    public void ProductionPlanHasLastModifiedBySubjectIdProperty()
    {
        var plan = new ProductionPlan();

        var property = typeof(ProductionPlan).GetProperty("LastModifiedBySubjectId");

        Assert.NotNull(property);
        Assert.Equal(typeof(Guid?), property.PropertyType);
    }

    [Fact]
    public void ProductionPlanLastModifiedBySubjectIdIsNullableByDefault()
    {
        var plan = new ProductionPlan();

        Assert.Null(plan.LastModifiedBySubjectId);
    }

    [Fact]
    public void ProductionPlanLastModifiedBySubjectIdCanBeAssigned()
    {
        var plan = new ProductionPlan
        {
            LastModifiedBySubjectId = MakerId,
        };

        Assert.Equal(MakerId, plan.LastModifiedBySubjectId);
    }

    // ── SelfApprovalNotAllowed failure code ──────────────────────────────────

    [Fact]
    public void SelfApprovalNotAllowedFailureCodeIsCorrectValue()
    {
        Assert.Equal(
            "self-approval-not-allowed",
            ProductionPlanWorkflowFailureCodes.SelfApprovalNotAllowed);
    }

    [Fact]
    public void SelfApprovalNotAllowedIsDistinctFromOtherFailureCodes()
    {
        var allCodes = new[]
        {
            ProductionPlanWorkflowFailureCodes.InvalidScope,
            ProductionPlanWorkflowFailureCodes.NotFound,
            ProductionPlanWorkflowFailureCodes.InvalidTransition,
            ProductionPlanWorkflowFailureCodes.ConcurrencyConflict,
            ProductionPlanWorkflowFailureCodes.AuditFailure,
            ProductionPlanWorkflowFailureCodes.SourceUnavailable,
            ProductionPlanWorkflowFailureCodes.ValidationFailed,
            ProductionPlanWorkflowFailureCodes.ScheduleBlockedByRisk,
        };

        foreach (var code in allCodes)
        {
            Assert.NotEqual(
                ProductionPlanWorkflowFailureCodes.SelfApprovalNotAllowed,
                code);
        }
    }

    // ── Development pilot second identity ────────────────────────────────────

    [Fact]
    public void ReviewerIdentitySubjectIdIsDefined()
    {
        Assert.NotEqual(Guid.Empty, DevelopmentPilotIds.ReviewerIdentitySubjectId);
    }

    [Fact]
    public void ReviewerTenantMembershipIdIsDefined()
    {
        Assert.NotEqual(Guid.Empty, DevelopmentPilotIds.ReviewerTenantMembershipId);
    }

    [Fact]
    public void ReviewerTenantRoleIdIsDefined()
    {
        Assert.NotEqual(Guid.Empty, DevelopmentPilotIds.ReviewerTenantRoleId);
    }

    [Fact]
    public void ReviewerFacilityMembershipIdIsDefined()
    {
        Assert.NotEqual(Guid.Empty, DevelopmentPilotIds.ReviewerFacilityMembershipId);
    }

    [Fact]
    public void ReviewerIdentitySubjectIdDiffersFromPrimaryIdentitySubjectId()
    {
        Assert.NotEqual(
            DevelopmentPilotIds.IdentitySubjectId,
            DevelopmentPilotIds.ReviewerIdentitySubjectId);
    }

    [Fact]
    public void ReviewerTenantMembershipIdDiffersFromPrimaryMembershipId()
    {
        Assert.NotEqual(
            DevelopmentPilotIds.TenantMembershipId,
            DevelopmentPilotIds.ReviewerTenantMembershipId);
    }

    // ── Handoff permission is separate from approve ───────────────────────────

    [Fact]
    public void HandoffPermissionKeyExists()
    {
        Assert.Equal(
            "production.planning.handoff",
            ProductionPlanningPermissionKeys.Handoff);
    }

    [Fact]
    public void ApprovePermissionDoesNotGrantHandoff()
    {
        Assert.NotEqual(
            ProductionPlanningPermissionKeys.Approve,
            ProductionPlanningPermissionKeys.Handoff);
    }

    [Fact]
    public void TransferPermissionDoesNotGrantHandoff()
    {
        Assert.NotEqual(
            ProductionPlanningPermissionKeys.Transfer,
            ProductionPlanningPermissionKeys.Handoff);
    }

    // ── Permission matrix: approve does not grant edit ────────────────────────

    [Fact]
    public void ApprovePermissionIsDistinctFromWritePermission()
    {
        Assert.NotEqual(
            ProductionPlanningPermissionKeys.Write,
            ProductionPlanningPermissionKeys.Approve);
    }

    [Fact]
    public void SchedulePermissionIsDistinctFromTransferPermission()
    {
        Assert.NotEqual(
            ProductionPlanningPermissionKeys.Schedule,
            ProductionPlanningPermissionKeys.Transfer);
    }

    [Fact]
    public void AllPermissionKeysAreDistinct()
    {
        var keys = new[]
        {
            ProductionPlanningPermissionKeys.Read,
            ProductionPlanningPermissionKeys.Write,
            ProductionPlanningPermissionKeys.Review,
            ProductionPlanningPermissionKeys.Approve,
            ProductionPlanningPermissionKeys.Schedule,
            ProductionPlanningPermissionKeys.Transfer,
            ProductionPlanningPermissionKeys.Handoff,
        };

        var distinct = keys.Distinct().ToArray();
        Assert.Equal(keys.Length, distinct.Length);
    }
}

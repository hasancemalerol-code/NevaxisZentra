using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Outbox;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>
/// Verifies that workflow audit actions, event types and outbox message types
/// are registered in the taxonomy sets used by the migration constraint checks.
/// This guards against taxonomy drift between constants and migration SQL.
/// </summary>
public sealed class ProductionPlanningAtomicAuditTaxonomyTests
{
    // ── Audit action taxonomy ─────────────────────────────────────────────────

    [Fact]
    public void AuditActionsAllSetContainsDraftCreateAction()
    {
        Assert.Contains(AuditActions.ProductionPlanDraftCreate, AuditActions.All, StringComparer.Ordinal);
    }

    [Fact]
    public void AuditActionsAllSetContainsDraftUpdateAction()
    {
        Assert.Contains(AuditActions.ProductionPlanDraftUpdate, AuditActions.All, StringComparer.Ordinal);
    }

    [Fact]
    public void AuditActionsAllSetContainsSubmitForReviewAction()
    {
        Assert.Contains(AuditActions.ProductionPlanSubmitForReview, AuditActions.All, StringComparer.Ordinal);
    }

    [Fact]
    public void AuditActionsAllSetContainsRequestRevisionAction()
    {
        Assert.Contains(AuditActions.ProductionPlanRequestRevision, AuditActions.All, StringComparer.Ordinal);
    }

    [Fact]
    public void AuditActionsAllSetContainsApproveAction()
    {
        Assert.Contains(AuditActions.ProductionPlanApprove, AuditActions.All, StringComparer.Ordinal);
    }

    [Fact]
    public void AuditActionsAllSetContainsRejectAction()
    {
        Assert.Contains(AuditActions.ProductionPlanReject, AuditActions.All, StringComparer.Ordinal);
    }

    // ── Audit event type taxonomy ─────────────────────────────────────────────

    [Fact]
    public void AuditEventTypesAllSetContainsDraftCreatedEvent()
    {
        Assert.Contains(AuditEventTypes.ProductionPlanDraftCreated, AuditEventTypes.All, StringComparer.Ordinal);
    }

    [Fact]
    public void AuditEventTypesAllSetContainsDraftUpdatedEvent()
    {
        Assert.Contains(AuditEventTypes.ProductionPlanDraftUpdated, AuditEventTypes.All, StringComparer.Ordinal);
    }

    [Fact]
    public void AuditEventTypesAllSetContainsSubmittedForReviewEvent()
    {
        Assert.Contains(AuditEventTypes.ProductionPlanSubmittedForReview, AuditEventTypes.All, StringComparer.Ordinal);
    }

    [Fact]
    public void AuditEventTypesAllSetContainsRevisionRequestedEvent()
    {
        Assert.Contains(AuditEventTypes.ProductionPlanRevisionRequested, AuditEventTypes.All, StringComparer.Ordinal);
    }

    [Fact]
    public void AuditEventTypesAllSetContainsApprovedEvent()
    {
        Assert.Contains(AuditEventTypes.ProductionPlanApproved, AuditEventTypes.All, StringComparer.Ordinal);
    }

    [Fact]
    public void AuditEventTypesAllSetContainsRejectedEvent()
    {
        Assert.Contains(AuditEventTypes.ProductionPlanRejected, AuditEventTypes.All, StringComparer.Ordinal);
    }

    // ── Outbox message type taxonomy ─────────────────────────────────────────

    [Fact]
    public void OutboxMessageTypesAllSetContainsSubmittedForReview()
    {
        Assert.Contains(OutboxMessageTypes.ProductionPlanSubmittedForReview, OutboxMessageTypes.All, StringComparer.Ordinal);
    }

    [Fact]
    public void OutboxMessageTypesAllSetContainsRevisionRequested()
    {
        Assert.Contains(OutboxMessageTypes.ProductionPlanRevisionRequested, OutboxMessageTypes.All, StringComparer.Ordinal);
    }

    [Fact]
    public void OutboxMessageTypesAllSetContainsApproved()
    {
        Assert.Contains(OutboxMessageTypes.ProductionPlanApproved, OutboxMessageTypes.All, StringComparer.Ordinal);
    }

    [Fact]
    public void OutboxMessageTypesAllSetContainsRejected()
    {
        Assert.Contains(OutboxMessageTypes.ProductionPlanRejected, OutboxMessageTypes.All, StringComparer.Ordinal);
    }

    // ── Taxonomy string value contracts ───────────────────────────────────────

    [Fact]
    public void WorkflowAuditActionStringsMatchMigrationSql()
    {
        Assert.Equal("production.plan.workflow.submit.for.review", AuditActions.ProductionPlanSubmitForReview);
        Assert.Equal("production.plan.workflow.request.revision", AuditActions.ProductionPlanRequestRevision);
        Assert.Equal("production.plan.workflow.approve", AuditActions.ProductionPlanApprove);
        Assert.Equal("production.plan.workflow.reject", AuditActions.ProductionPlanReject);
    }

    [Fact]
    public void WorkflowAuditEventTypeStringsMatchMigrationSql()
    {
        Assert.Equal("production.plan.submitted.for.review", AuditEventTypes.ProductionPlanSubmittedForReview);
        Assert.Equal("production.plan.revision.requested", AuditEventTypes.ProductionPlanRevisionRequested);
        Assert.Equal("production.plan.approved", AuditEventTypes.ProductionPlanApproved);
        Assert.Equal("production.plan.rejected", AuditEventTypes.ProductionPlanRejected);
    }

    [Fact]
    public void WorkflowOutboxMessageTypeStringsMatchMigrationSql()
    {
        Assert.Equal("production-plan-submitted-for-review", OutboxMessageTypes.ProductionPlanSubmittedForReview);
        Assert.Equal("production-plan-revision-requested", OutboxMessageTypes.ProductionPlanRevisionRequested);
        Assert.Equal("production-plan-approved", OutboxMessageTypes.ProductionPlanApproved);
        Assert.Equal("production-plan-rejected", OutboxMessageTypes.ProductionPlanRejected);
    }

    // ── Atomicity design guards ───────────────────────────────────────────────

    [Fact]
    public void WorkflowRepositoryUsesDbContextDirectlyNotSeparateAuditWriter()
    {
        // The repository type exists in the same assembly as the DbContext, not via
        // an IAuditWriter/IOutboxWriter interface that would write out-of-transaction.
        var repositoryType = typeof(ProductionPlanning.ProductionPlanWorkflowRepository);
        var ctorParameters = repositoryType.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType.Name)
            .ToArray();

        Assert.Contains("NevaxisZentraDbContext", ctorParameters);
        Assert.DoesNotContain("IAuditWriter", ctorParameters);
        Assert.DoesNotContain("IOutboxWriter", ctorParameters);
    }

    [Fact]
    public void WorkflowFailureCodesAreDistinct()
    {
        var allCodes = new[]
        {
            ProductionPlanning.ProductionPlanWorkflowFailureCodes.InvalidScope,
            ProductionPlanning.ProductionPlanWorkflowFailureCodes.ValidationFailed,
            ProductionPlanning.ProductionPlanWorkflowFailureCodes.NotFound,
            ProductionPlanning.ProductionPlanWorkflowFailureCodes.InvalidTransition,
            ProductionPlanning.ProductionPlanWorkflowFailureCodes.ConcurrencyConflict,
            ProductionPlanning.ProductionPlanWorkflowFailureCodes.SourceUnavailable,
            ProductionPlanning.ProductionPlanWorkflowFailureCodes.AuditFailure,
            ProductionPlanning.ProductionPlanWorkflowFailureCodes.DataIntegrity,
            ProductionPlanning.ProductionPlanWorkflowFailureCodes.UnexpectedError,
        };

        Assert.Equal(allCodes.Length, allCodes.Distinct(StringComparer.Ordinal).Count());
    }
}

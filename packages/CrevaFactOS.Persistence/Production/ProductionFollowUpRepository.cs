using System.Text.Json;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Production;

public interface IProductionFollowUpRepository
{
    Task<ProductionFollowUpWorkspaceReadModel> GetFollowUpWorkspaceAsync(
        ProductionOrderOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionFollowUpRowReadModel>> ListFollowUpRowsAsync(
        ProductionOrderOperationContext context,
        string? followUpStatus = null,
        string? exceptionType = null,
        CancellationToken cancellationToken = default);

    Task<ProductionFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        ProductionOrderOperationContext context,
        Guid exceptionId,
        CancellationToken cancellationToken = default);

    Task<ProductionFollowUpWriteResult> LinkTaskAsync(
        ProductionOrderOperationContext context,
        LinkProductionFollowUpTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionFollowUpWriteResult> TransitionFollowUpAsync(
        ProductionOrderOperationContext context,
        TransitionProductionFollowUpCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class UnavailableProductionFollowUpRepository : IProductionFollowUpRepository
{
    private static readonly ProductionFollowUpWorkspaceReadModel EmptyWorkspace =
        new(ProductionFollowUpConstants.AutomationDisclaimer, 0, 0, 0, 0, 0, false, false, false);

    private static ProductionFollowUpWriteResult Unavailable() =>
        ProductionFollowUpWriteResult.Fail(
            ProductionOrderFailureCodes.SourceUnavailable,
            "Production follow-up service is unavailable because persistence is disabled.");

    public Task<ProductionFollowUpWorkspaceReadModel> GetFollowUpWorkspaceAsync(
        ProductionOrderOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EmptyWorkspace);

    public Task<IReadOnlyList<ProductionFollowUpRowReadModel>> ListFollowUpRowsAsync(
        ProductionOrderOperationContext context,
        string? followUpStatus = null,
        string? exceptionType = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductionFollowUpRowReadModel>>([]);

    public Task<ProductionFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        ProductionOrderOperationContext context,
        Guid exceptionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProductionFollowUpDetailReadModel?>(null);

    public Task<ProductionFollowUpWriteResult> LinkTaskAsync(
        ProductionOrderOperationContext context,
        LinkProductionFollowUpTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<ProductionFollowUpWriteResult> TransitionFollowUpAsync(
        ProductionOrderOperationContext context,
        TransitionProductionFollowUpCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}

public sealed class ProductionFollowUpRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IProductionFollowUpRepository
{
    public Task<ProductionFollowUpWorkspaceReadModel> GetFollowUpWorkspaceAsync(
        ProductionOrderOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var rows = await LoadEligibleRowsAsync(context, null, null, ct);

            return new ProductionFollowUpWorkspaceReadModel(
                ProductionFollowUpConstants.AutomationDisclaimer,
                rows.Count(r => r.FollowUpStatus == ProductionFollowUpStatuses.RequiresReview),
                rows.Count(r => r.FollowUpStatus == ProductionFollowUpStatuses.Acknowledged),
                rows.Count(r => r.LinkedAiTaskId.HasValue),
                rows.Count(r => r.IsBlockedOrDelayed),
                rows.Count(r => r.HasInsufficientEvidence),
                CanRead: true,
                CanManage: canManage,
                CanCreateTask: canCreateTask);
        }, cancellationToken);

    public Task<IReadOnlyList<ProductionFollowUpRowReadModel>> ListFollowUpRowsAsync(
        ProductionOrderOperationContext context,
        string? followUpStatus = null,
        string? exceptionType = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => LoadEligibleRowsAsync(context, followUpStatus, exceptionType, ct),
            cancellationToken);

    public Task<ProductionFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        ProductionOrderOperationContext context,
        Guid exceptionId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var loaded = await LoadExceptionWithOrderAsync(context, exceptionId, ct);
            if (loaded is null)
            {
                return null;
            }

            var (exception, order) = loaded.Value;
            if (!ProductionFollowUpPresentationBuilder.IsEligibleForFollowUp(exception))
            {
                return null;
            }

            var auditEvents = await dbContext.ProductionFollowUpAuditEvents.AsNoTracking()
                .Where(e => e.TenantId == context.TenantId
                            && e.FacilityId == context.FacilityId
                            && e.ExceptionId == exceptionId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(20)
                .Select(e => new ProductionFollowUpAuditEventReadModel(
                    e.EventType,
                    e.PayloadJson,
                    e.Actor,
                    e.OccurredAtUtc))
                .ToListAsync(ct);

            return MapDetail(exception, order, auditEvents);
        }, cancellationToken);

    public Task<ProductionFollowUpWriteResult> LinkTaskAsync(
        ProductionOrderOperationContext context,
        LinkProductionFollowUpTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var loaded = await LoadExceptionForWriteAsync(context, command.ExceptionId, ct);
            if (loaded is null)
            {
                return ProductionFollowUpWriteResult.Fail(
                    ProductionOrderFailureCodes.ExceptionNotFound,
                    "Üretim istisnası bulunamadı.");
            }

            var (exception, order) = loaded.Value;

            if (exception.LinkedAiTaskId.HasValue)
            {
                if (exception.LinkedAiTaskId.Value == command.AiTaskId)
                {
                    var existing = await GetFollowUpDetailAsync(context, exception.Id, ct);
                    return ProductionFollowUpWriteResult.Success(exception.Id, idempotentReplay: true, detail: existing);
                }

                return ProductionFollowUpWriteResult.Fail(
                    "validation-failed",
                    "Üretim istisnası zaten farklı bir göreve bağlı.");
            }

            if (!ProductionFollowUpPresentationBuilder.CanCreateTaskDraft(exception, order))
            {
                return ProductionFollowUpWriteResult.Fail(
                    "validation-failed",
                    "Bu üretim istisnası için görev taslağı oluşturulamaz.");
            }

            var taskExists = await dbContext.AiTasks.AsNoTracking()
                .AnyAsync(
                    task => task.TenantId == context.TenantId
                            && task.FacilityId == context.FacilityId
                            && task.Id == command.AiTaskId,
                    ct);

            if (!taskExists)
            {
                return ProductionFollowUpWriteResult.Fail(
                    ProductionOrderFailureCodes.SourceUnavailable,
                    "Bağlanacak AI görevi bulunamadı.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            exception.LinkedAiTaskId = command.AiTaskId;
            exception.FollowUpStatus = ProductionFollowUpStatuses.TaskCreated;
            exception.FollowUpUpdatedBy = actor;
            exception.Version += 1;

            await AppendFollowUpAuditEventAsync(
                exception,
                ProductionFollowUpAuditEventTypes.TaskLinked,
                new { aiTaskId = command.AiTaskId },
                actor,
                now,
                ct);

            await dbContext.SaveChangesAsync(ct);

            var detail = await GetFollowUpDetailAsync(context, exception.Id, ct);
            return ProductionFollowUpWriteResult.Success(exception.Id, detail: detail);
        }, cancellationToken);

    public Task<ProductionFollowUpWriteResult> TransitionFollowUpAsync(
        ProductionOrderOperationContext context,
        TransitionProductionFollowUpCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var loaded = await LoadExceptionForWriteAsync(context, command.ExceptionId, ct);
            if (loaded is null)
            {
                return ProductionFollowUpWriteResult.Fail(
                    ProductionOrderFailureCodes.ExceptionNotFound,
                    "Üretim istisnası bulunamadı.");
            }

            var (exception, _) = loaded.Value;

            if (exception.Version != command.ExpectedVersion)
            {
                return ProductionFollowUpWriteResult.Fail(
                    ProductionOrderFailureCodes.ConcurrencyConflict,
                    "Kayıt başka bir kullanıcı tarafından güncellendi.");
            }

            if (command.TargetFollowUpStatus == ProductionFollowUpStatuses.Dismissed
                && string.IsNullOrWhiteSpace(command.Note))
            {
                return ProductionFollowUpWriteResult.Fail(
                    "validation-failed",
                    "Red gerekçesi zorunludur.");
            }

            if (!ProductionFollowUpPresentationBuilder.IsValidFollowUpTransition(
                    exception.FollowUpStatus,
                    command.TargetFollowUpStatus))
            {
                return ProductionFollowUpWriteResult.Fail(
                    ProductionOrderFailureCodes.InvalidTransition,
                    "Geçersiz takip durumu geçişi.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var previous = exception.FollowUpStatus;
            exception.FollowUpStatus = command.TargetFollowUpStatus;
            exception.FollowUpUpdatedBy = actor;
            exception.Version += 1;

            await AppendFollowUpAuditEventAsync(
                exception,
                ProductionFollowUpAuditEventTypes.FollowUpTransitioned,
                new
                {
                    previousStatus = previous,
                    targetStatus = command.TargetFollowUpStatus,
                    note = command.Note,
                },
                actor,
                now,
                ct);

            await dbContext.SaveChangesAsync(ct);

            var detail = await GetFollowUpDetailAsync(context, exception.Id, ct);
            return ProductionFollowUpWriteResult.Success(exception.Id, detail: detail);
        }, cancellationToken);

    private async Task<IReadOnlyList<ProductionFollowUpRowReadModel>> LoadEligibleRowsAsync(
        ProductionOrderOperationContext context,
        string? followUpStatus,
        string? exceptionType,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ProductionExecutionExceptions.AsNoTracking()
            .Where(e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId)
            .Where(e => e.Status == ProductionExceptionStatuses.Open
                        || e.Status == ProductionExceptionStatuses.Acknowledged)
            .Where(e => e.FollowUpStatus != ProductionFollowUpStatuses.Resolved
                        && e.FollowUpStatus != ProductionFollowUpStatuses.Dismissed);

        if (!string.IsNullOrWhiteSpace(followUpStatus))
        {
            query = query.Where(e => e.FollowUpStatus == followUpStatus);
        }

        if (!string.IsNullOrWhiteSpace(exceptionType))
        {
            query = query.Where(e => e.ExceptionType == exceptionType);
        }

        var exceptions = await query.OrderByDescending(e => e.RaisedAtUtc).ToListAsync(cancellationToken);
        if (exceptions.Count == 0)
        {
            return [];
        }

        var orderIds = exceptions.Select(e => e.ProductionOrderId).Distinct().ToList();
        var orders = await dbContext.ProductionOrders.AsNoTracking()
            .Where(o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        return exceptions
            .Select(exception =>
            {
                orders.TryGetValue(exception.ProductionOrderId, out var order);
                return MapRow(exception, order);
            })
            .ToList();
    }

    private async Task<(ProductionExecutionExceptionRecord Exception, ProductionOrder? Order)?> LoadExceptionWithOrderAsync(
        ProductionOrderOperationContext context,
        Guid exceptionId,
        CancellationToken cancellationToken)
    {
        var exception = await dbContext.ProductionExecutionExceptions.AsNoTracking()
            .SingleOrDefaultAsync(
                e => e.TenantId == context.TenantId
                     && e.FacilityId == context.FacilityId
                     && e.Id == exceptionId,
                cancellationToken);

        if (exception is null)
        {
            return null;
        }

        var order = await dbContext.ProductionOrders.AsNoTracking()
            .SingleOrDefaultAsync(
                o => o.TenantId == context.TenantId
                     && o.FacilityId == context.FacilityId
                     && o.Id == exception.ProductionOrderId,
                cancellationToken);

        return (exception, order);
    }

    private async Task<(ProductionExecutionExceptionRecord Exception, ProductionOrder? Order)?> LoadExceptionForWriteAsync(
        ProductionOrderOperationContext context,
        Guid exceptionId,
        CancellationToken cancellationToken)
    {
        var exception = await dbContext.ProductionExecutionExceptions
            .SingleOrDefaultAsync(
                e => e.TenantId == context.TenantId
                     && e.FacilityId == context.FacilityId
                     && e.Id == exceptionId,
                cancellationToken);

        if (exception is null)
        {
            return null;
        }

        var order = await dbContext.ProductionOrders.AsNoTracking()
            .SingleOrDefaultAsync(
                o => o.TenantId == context.TenantId
                     && o.FacilityId == context.FacilityId
                     && o.Id == exception.ProductionOrderId,
                cancellationToken);

        return (exception, order);
    }

    private static ProductionFollowUpRowReadModel MapRow(
        ProductionExecutionExceptionRecord exception,
        ProductionOrder? order) =>
        new(
            exception.Id,
            exception.ProductionOrderId,
            order?.ReferenceCode ?? string.Empty,
            exception.ExceptionType,
            exception.Severity,
            exception.Status,
            exception.Message,
            exception.RaisedAtUtc,
            exception.FollowUpStatus,
            exception.LinkedAiTaskId,
            exception.Version,
            ProductionFollowUpPresentationBuilder.CanCreateTaskDraft(exception, order),
            ProductionFollowUpPresentationBuilder.ResolveTaskPurpose(exception, order),
            ProductionFollowUpPresentationBuilder.HasInsufficientEvidence(exception, order),
            ProductionFollowUpPresentationBuilder.IsBlockedOrDelayedIndicator(exception));

    private static ProductionFollowUpDetailReadModel MapDetail(
        ProductionExecutionExceptionRecord exception,
        ProductionOrder? order,
        IReadOnlyList<ProductionFollowUpAuditEventReadModel> auditEvents) =>
        new(
            exception.Id,
            exception.ProductionOrderId,
            order?.ReferenceCode ?? string.Empty,
            order?.Title ?? string.Empty,
            order is null ? string.Empty : FormatOrderStatus(order.Status),
            order?.MachineReference,
            order?.MoldReference,
            order?.ProductReference,
            exception.ExceptionType,
            exception.Severity,
            exception.Status,
            exception.Message,
            exception.RaisedAtUtc,
            exception.RaisedBySubjectId,
            exception.FollowUpStatus,
            exception.LinkedAiTaskId,
            exception.Version,
            ProductionFollowUpPresentationBuilder.CanCreateTaskDraft(exception, order),
            ProductionFollowUpPresentationBuilder.ResolveTaskPurpose(exception, order),
            ProductionFollowUpPresentationBuilder.HasInsufficientEvidence(exception, order),
            ProductionFollowUpPresentationBuilder.BuildEvidenceSummary(exception, order),
            auditEvents);

    private async Task AppendFollowUpAuditEventAsync(
        ProductionExecutionExceptionRecord exception,
        string eventType,
        object payload,
        string actor,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.ProductionFollowUpAuditEvents.Add(new ProductionFollowUpAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = exception.TenantId,
            FacilityId = exception.FacilityId,
            ExceptionId = exception.Id,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload),
            Actor = actor,
            OccurredAtUtc = occurredAtUtc,
        });

        await Task.CompletedTask;
    }

    private static string FormatOrderStatus(ProductionOrderStatus status) =>
        status switch
        {
            ProductionOrderStatus.Ready => "ready",
            ProductionOrderStatus.InProgress => "in-progress",
            ProductionOrderStatus.Paused => "paused",
            ProductionOrderStatus.Completed => "completed",
            ProductionOrderStatus.Cancelled => "cancelled",
            _ => "ready",
        };
}

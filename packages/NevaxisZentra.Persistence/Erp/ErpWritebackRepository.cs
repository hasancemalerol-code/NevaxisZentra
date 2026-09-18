using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Erp;

public interface IErpWritebackRepository
{
    Task<IReadOnlyList<ErpWritebackSummaryReadModel>> ListWritebacksAsync(
        ErpOperationContext context,
        CancellationToken cancellationToken = default);

    Task<ErpWritebackDetailReadModel?> GetWritebackAsync(
        ErpOperationContext context,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ErpWriteResult> CreateWritebackAsync(
        ErpOperationContext context,
        CreateErpWritebackCommand command,
        CancellationToken cancellationToken = default);

    Task<ErpWriteResult> SubmitWritebackAsync(
        ErpOperationContext context,
        SubmitErpWritebackCommand command,
        CancellationToken cancellationToken = default);

    Task<ErpWriteResult> ApproveWritebackAsync(
        ErpOperationContext context,
        ApproveErpWritebackCommand command,
        CancellationToken cancellationToken = default);

    Task<ErpWriteResult> RejectWritebackAsync(
        ErpOperationContext context,
        RejectErpWritebackCommand command,
        CancellationToken cancellationToken = default);

    Task<ErpWriteResult> ExecuteWritebackAsync(
        ErpOperationContext context,
        ExecuteErpWritebackCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class ErpWritebackRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IErpWritebackAdapterResolver writebackAdapterResolver) : IErpWritebackRepository
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public Task<IReadOnlyList<ErpWritebackSummaryReadModel>> ListWritebacksAsync(
        ErpOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var rows = await dbContext.ErpWritebackRequests.AsNoTracking()
                .Include(r => r.Approvals)
                .Where(r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId)
                .OrderByDescending(r => r.RequestedAtUtc)
                .ThenByDescending(r => r.Id)
                .ToListAsync(ct);

            return (IReadOnlyList<ErpWritebackSummaryReadModel>)rows.Select(MapSummary).ToList();
        }, cancellationToken);

    public Task<ErpWritebackDetailReadModel?> GetWritebackAsync(
        ErpOperationContext context,
        Guid id,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var row = await dbContext.ErpWritebackRequests.AsNoTracking()
                .Include(r => r.Approvals)
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.Id == id,
                    ct);

            return row is null ? null : MapDetail(row);
        }, cancellationToken);

    public Task<ErpWriteResult> CreateWritebackAsync(
        ErpOperationContext context,
        CreateErpWritebackCommand command,
        CancellationToken cancellationToken = default) =>
        ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                if (command.ConnectionProfileId == Guid.Empty)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.ValidationFailed, "Connection profile id is required.");
                }

                if (string.IsNullOrWhiteSpace(command.CommandType))
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.ValidationFailed, "Command type is required.");
                }

                if (string.IsNullOrWhiteSpace(command.PayloadJson))
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.ValidationFailed, "Payload is required.");
                }

                if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.ValidationFailed, "Idempotency key is required.");
                }

                var normalizedCommandType = command.CommandType.Trim().ToLowerInvariant();
                var normalizedIdempotencyKey = command.IdempotencyKey.Trim();

                var existing = await dbContext.ErpWritebackRequests.AsNoTracking()
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                            && r.FacilityId == context.FacilityId
                            && r.ConnectionProfileId == command.ConnectionProfileId
                            && r.CommandType == normalizedCommandType
                            && r.IdempotencyKey == normalizedIdempotencyKey,
                        ct);

                if (existing is not null)
                {
                    return ErpWriteResult.Success(existing.Id, existing.Version);
                }

                var payloadValidation = ValidateCanonicalPayload(normalizedCommandType, command.PayloadJson);
                if (payloadValidation is not null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.ValidationFailed, payloadValidation);
                }

                var profile = await dbContext.ErpConnectionProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId
                            && c.FacilityId == context.FacilityId
                            && c.Id == command.ConnectionProfileId,
                        ct);

                if (profile is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Connection profile not found.");
                }

                var payloadHash = ComputePayloadHash(command.PayloadJson);
                var now = DateTimeOffset.UtcNow;
                var request = new ErpWritebackRequest
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ConnectionProfileId = profile.Id,
                    Provider = profile.Provider,
                    CommandType = normalizedCommandType,
                    TargetEntityType = NormalizeOptional(command.TargetEntityType),
                    TargetExternalId = NormalizeOptional(command.TargetExternalId),
                    PayloadJson = command.PayloadJson,
                    PayloadHash = payloadHash,
                    IdempotencyKey = normalizedIdempotencyKey,
                    Status = ErpWritebackStatuses.Draft,
                    RequestedBySubjectId = context.IdentitySubjectId.ToString(),
                    RequestedAtUtc = now,
                    Version = 1,
                };

                dbContext.ErpWritebackRequests.Add(request);
                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.WritebackCreated,
                    ErpAuditEventTypes.WritebackCreated,
                    "erp.writeback",
                    request.Id,
                    now,
                    new
                    {
                        request.Id,
                        request.ConnectionProfileId,
                        request.Provider,
                        request.CommandType,
                        request.PayloadHash,
                        request.IdempotencyKey,
                        version = request.Version,
                    });

                await dbContext.SaveChangesAsync(ct);
                return ErpWriteResult.Success(request.Id, request.Version);
            },
            cancellationToken);

    public Task<ErpWriteResult> SubmitWritebackAsync(
        ErpOperationContext context,
        SubmitErpWritebackCommand command,
        CancellationToken cancellationToken = default) =>
        ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var request = await LoadTrackedWritebackAsync(context, command.WritebackId, ct);
                if (request is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Writeback request not found.");
                }

                if (request.Version != command.ExpectedVersion)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        $"The writeback request was modified (have {request.Version}, expected {command.ExpectedVersion}).");
                }

                if (request.Status != ErpWritebackStatuses.Draft)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ValidationFailed,
                        $"Writeback request cannot be submitted from status '{request.Status}'.");
                }

                var now = DateTimeOffset.UtcNow;
                request.Status = ErpWritebackStatuses.PendingApproval;
                request.SubmittedAtUtc = now;
                request.Version = command.ExpectedVersion + 1;

                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.WritebackSubmitted,
                    ErpAuditEventTypes.WritebackSubmitted,
                    "erp.writeback",
                    request.Id,
                    now,
                    new
                    {
                        request.Id,
                        request.CommandType,
                        request.PayloadHash,
                        version = request.Version,
                    });
                ErpRepositorySupport.AddOutbox(
                    dbContext,
                    context,
                    "erp-writeback",
                    request.Id,
                    request.Version,
                    ErpOutboxMessageTypes.WritebackSubmitted,
                    now,
                    new
                    {
                        writebackId = request.Id.ToString(),
                        commandType = request.CommandType,
                        connectionProfileId = request.ConnectionProfileId.ToString(),
                        payloadHash = request.PayloadHash,
                    });

                await dbContext.SaveChangesAsync(ct);
                return ErpWriteResult.Success(request.Id, request.Version);
            },
            cancellationToken);

    public Task<ErpWriteResult> ApproveWritebackAsync(
        ErpOperationContext context,
        ApproveErpWritebackCommand command,
        CancellationToken cancellationToken = default) =>
        ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var request = await LoadTrackedWritebackAsync(context, command.WritebackId, ct);
                if (request is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Writeback request not found.");
                }

                if (request.Version != command.ExpectedVersion)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        $"The writeback request was modified (have {request.Version}, expected {command.ExpectedVersion}).");
                }

                if (request.Status != ErpWritebackStatuses.PendingApproval)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ValidationFailed,
                        $"Writeback request cannot be approved from status '{request.Status}'.");
                }

                var now = DateTimeOffset.UtcNow;
                var newVersion = command.ExpectedVersion + 1;
                request.ApprovedPayloadHash = request.PayloadHash;
                request.ApprovedVersion = newVersion;
                request.Status = ErpWritebackStatuses.Approved;
                request.Version = newVersion;

                var approval = new ErpWritebackApproval
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    WritebackRequestId = request.Id,
                    Decision = ErpWritebackDecisions.Approved,
                    DecidedBySubjectId = context.IdentitySubjectId.ToString(),
                    DecidedAtUtc = now,
                    RequestVersion = command.ExpectedVersion,
                    PayloadHash = request.PayloadHash,
                    Comment = NormalizeOptional(command.Comment),
                };

                dbContext.ErpWritebackApprovals.Add(approval);
                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.WritebackApproved,
                    ErpAuditEventTypes.WritebackApproved,
                    "erp.writeback",
                    request.Id,
                    now,
                    new
                    {
                        request.Id,
                        approvalId = approval.Id,
                        request.CommandType,
                        request.PayloadHash,
                        approvedVersion = request.ApprovedVersion,
                        version = request.Version,
                    });
                ErpRepositorySupport.AddOutbox(
                    dbContext,
                    context,
                    "erp-writeback",
                    request.Id,
                    request.Version,
                    ErpOutboxMessageTypes.WritebackApproved,
                    now,
                    new
                    {
                        writebackId = request.Id.ToString(),
                        approvalId = approval.Id.ToString(),
                        commandType = request.CommandType,
                        payloadHash = request.PayloadHash,
                        approvedVersion = request.ApprovedVersion,
                    });

                await dbContext.SaveChangesAsync(ct);
                return ErpWriteResult.Success(request.Id, request.Version);
            },
            cancellationToken);

    public Task<ErpWriteResult> RejectWritebackAsync(
        ErpOperationContext context,
        RejectErpWritebackCommand command,
        CancellationToken cancellationToken = default) =>
        ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var request = await LoadTrackedWritebackAsync(context, command.WritebackId, ct);
                if (request is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Writeback request not found.");
                }

                if (request.Version != command.ExpectedVersion)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        $"The writeback request was modified (have {request.Version}, expected {command.ExpectedVersion}).");
                }

                if (request.Status != ErpWritebackStatuses.PendingApproval)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ValidationFailed,
                        $"Writeback request cannot be rejected from status '{request.Status}'.");
                }

                var now = DateTimeOffset.UtcNow;
                request.Status = ErpWritebackStatuses.Rejected;
                request.ApprovedPayloadHash = null;
                request.ApprovedVersion = null;
                request.Version = command.ExpectedVersion + 1;

                var approval = new ErpWritebackApproval
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    WritebackRequestId = request.Id,
                    Decision = ErpWritebackDecisions.Rejected,
                    DecidedBySubjectId = context.IdentitySubjectId.ToString(),
                    DecidedAtUtc = now,
                    RequestVersion = command.ExpectedVersion,
                    PayloadHash = request.PayloadHash,
                    Comment = NormalizeOptional(command.Comment),
                };

                dbContext.ErpWritebackApprovals.Add(approval);
                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.WritebackRejected,
                    ErpAuditEventTypes.WritebackRejected,
                    "erp.writeback",
                    request.Id,
                    now,
                    new
                    {
                        request.Id,
                        approvalId = approval.Id,
                        request.CommandType,
                        request.PayloadHash,
                        version = request.Version,
                    });
                ErpRepositorySupport.AddOutbox(
                    dbContext,
                    context,
                    "erp-writeback",
                    request.Id,
                    request.Version,
                    ErpOutboxMessageTypes.WritebackRejected,
                    now,
                    new
                    {
                        writebackId = request.Id.ToString(),
                        approvalId = approval.Id.ToString(),
                        commandType = request.CommandType,
                        payloadHash = request.PayloadHash,
                    });

                await dbContext.SaveChangesAsync(ct);
                return ErpWriteResult.Success(request.Id, request.Version);
            },
            cancellationToken);

    public Task<ErpWriteResult> ExecuteWritebackAsync(
        ErpOperationContext context,
        ExecuteErpWritebackCommand command,
        CancellationToken cancellationToken = default) =>
        ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var request = await LoadTrackedWritebackAsync(context, command.WritebackId, ct);
                if (request is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Writeback request not found.");
                }

                if (request.Status == ErpWritebackStatuses.Succeeded)
                {
                    return ErpWriteResult.Success(request.Id, request.Version);
                }

                if (request.Status == ErpWritebackStatuses.Executing)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        "Writeback execution is already in progress.");
                }

                if (request.Version != command.ExpectedVersion)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        $"The writeback request was modified (have {request.Version}, expected {command.ExpectedVersion}).");
                }

                var executionBlock = ResolveExecutionBlock(request.Status);
                if (executionBlock is not null)
                {
                    return executionBlock;
                }

                if (request.ApprovedPayloadHash != request.PayloadHash
                    || request.ApprovedVersion != request.Version)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.WritebackNotApproved,
                        "Writeback approval snapshot is no longer valid for the current request version and payload.");
                }

                var profile = await dbContext.ErpConnectionProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId
                            && c.FacilityId == context.FacilityId
                            && c.Id == request.ConnectionProfileId,
                        ct);

                if (profile is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Connection profile not found.");
                }

                var capability = ResolveWritebackCapability(request.CommandType);
                if (capability is null)
                {
                    return await PersistBlockedWritebackAsync(
                        context,
                        request,
                        ErpFailureCodes.WritebackBlocked,
                        $"Writeback command '{request.CommandType}' is not supported.",
                        ct);
                }

                var adapter = writebackAdapterResolver.Resolve(profile.Provider);
                if (adapter is null || !adapter.WritebackCapabilities.Contains(capability))
                {
                    return await PersistBlockedWritebackAsync(
                        context,
                        request,
                        ErpFailureCodes.WritebackBlocked,
                        $"Provider '{profile.Provider}' does not support writeback capability '{capability}'.",
                        ct);
                }

                var now = DateTimeOffset.UtcNow;
                request.Status = ErpWritebackStatuses.Executing;
                request.LastErrorCode = null;
                request.LastErrorMessage = null;
                request.Version = command.ExpectedVersion + 1;

                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.WritebackExecutionStarted,
                    ErpAuditEventTypes.WritebackExecutionStarted,
                    "erp.writeback",
                    request.Id,
                    now,
                    new
                    {
                        request.Id,
                        request.CommandType,
                        request.Provider,
                        request.PayloadHash,
                        version = request.Version,
                    });

                await dbContext.SaveChangesAsync(ct);

                var adapterContext = ErpRepositorySupport.ToAdapterContext(profile);
                var executionContext = new ErpWritebackExecutionContext(
                    request.Id,
                    request.CommandType,
                    request.PayloadJson,
                    request.PayloadHash,
                    request.IdempotencyKey);

                ErpWritebackValidationResult validation;
                try
                {
                    validation = await adapter.ValidateWritebackAsync(adapterContext, executionContext, ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    return await PersistFailedExecutionAsync(
                        context,
                        request,
                        ErpFailureCodes.SourceUnavailable,
                        SanitizeErrorMessage(ex.Message),
                        ct);
                }

                if (!validation.IsValid)
                {
                    return await PersistFailedExecutionAsync(
                        context,
                        request,
                        validation.ErrorCode ?? ErpFailureCodes.ValidationFailed,
                        validation.ErrorMessage,
                        ct);
                }

                ErpWritebackExecutionResult execution;
                try
                {
                    execution = await adapter.ExecuteWritebackAsync(adapterContext, executionContext, ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    return await PersistFailedExecutionAsync(
                        context,
                        request,
                        ErpFailureCodes.SourceUnavailable,
                        SanitizeErrorMessage(ex.Message),
                        ct);
                }

                if (execution.Succeeded)
                {
                    return await PersistSucceededExecutionAsync(
                        context,
                        request,
                        execution.ExternalExecutionReference,
                        ct);
                }

                return await PersistFailedExecutionAsync(
                    context,
                    request,
                    execution.ErrorCode ?? ErpFailureCodes.WritebackBlocked,
                    execution.ErrorMessage,
                    ct);
            },
            cancellationToken);

    public static string ComputePayloadHash(string payloadJson)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<ErpWritebackRequest?> LoadTrackedWritebackAsync(
        ErpOperationContext context,
        Guid writebackId,
        CancellationToken cancellationToken) =>
        await dbContext.ErpWritebackRequests
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.Id == writebackId,
                cancellationToken);

    private async Task<ErpWriteResult> PersistBlockedWritebackAsync(
        ErpOperationContext context,
        ErpWritebackRequest request,
        string failureCode,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        request.Status = ErpWritebackStatuses.Blocked;
        request.LastErrorCode = failureCode;
        request.LastErrorMessage = SanitizeErrorMessage(failureReason);
        request.Version += 1;

        ErpRepositorySupport.AddAudit(
            dbContext,
            context,
            ErpAuditActions.WritebackExecutionBlocked,
            ErpAuditEventTypes.WritebackExecutionBlocked,
            "erp.writeback",
            request.Id,
            now,
            new
            {
                request.Id,
                request.CommandType,
                request.Provider,
                failureCode = request.LastErrorCode,
                version = request.Version,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        return ErpWriteResult.Fail(failureCode, failureReason);
    }

    private async Task<ErpWriteResult> PersistFailedExecutionAsync(
        ErpOperationContext context,
        ErpWritebackRequest request,
        string? failureCode,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        request.Status = ErpWritebackStatuses.Failed;
        request.LastErrorCode = failureCode ?? ErpFailureCodes.WritebackBlocked;
        request.LastErrorMessage = SanitizeErrorMessage(failureReason);
        request.Version += 1;

        ErpRepositorySupport.AddAudit(
            dbContext,
            context,
            ErpAuditActions.WritebackExecutionFailed,
            ErpAuditEventTypes.WritebackExecutionFailed,
            "erp.writeback",
            request.Id,
            now,
            new
            {
                request.Id,
                request.CommandType,
                request.Provider,
                failureCode = request.LastErrorCode,
                version = request.Version,
            });
        ErpRepositorySupport.AddOutbox(
            dbContext,
            context,
            "erp-writeback",
            request.Id,
            request.Version,
            ErpOutboxMessageTypes.WritebackFailed,
            now,
            new
            {
                writebackId = request.Id.ToString(),
                commandType = request.CommandType,
                failureCode = request.LastErrorCode,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        return ErpWriteResult.Fail(request.LastErrorCode, request.LastErrorMessage);
    }

    private async Task<ErpWriteResult> PersistSucceededExecutionAsync(
        ErpOperationContext context,
        ErpWritebackRequest request,
        string? externalExecutionReference,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        request.Status = ErpWritebackStatuses.Succeeded;
        request.ExecutedAtUtc = now;
        request.ExternalExecutionReference = externalExecutionReference;
        request.LastErrorCode = null;
        request.LastErrorMessage = null;
        request.Version += 1;

        ErpRepositorySupport.AddAudit(
            dbContext,
            context,
            ErpAuditActions.WritebackExecutionSucceeded,
            ErpAuditEventTypes.WritebackExecutionSucceeded,
            "erp.writeback",
            request.Id,
            now,
            new
            {
                request.Id,
                request.CommandType,
                request.Provider,
                externalExecutionReference = request.ExternalExecutionReference,
                version = request.Version,
            });
        ErpRepositorySupport.AddOutbox(
            dbContext,
            context,
            "erp-writeback",
            request.Id,
            request.Version,
            ErpOutboxMessageTypes.WritebackSucceeded,
            now,
            new
            {
                writebackId = request.Id.ToString(),
                commandType = request.CommandType,
                externalExecutionReference = request.ExternalExecutionReference,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        return ErpWriteResult.Success(request.Id, request.Version);
    }

    private static ErpWriteResult? ResolveExecutionBlock(string status) =>
        status switch
        {
            ErpWritebackStatuses.Draft
            or ErpWritebackStatuses.PendingApproval
            or ErpWritebackStatuses.Rejected =>
                ErpWriteResult.Fail(
                    ErpFailureCodes.WritebackNotApproved,
                    "Writeback execution requires an approved request."),
            ErpWritebackStatuses.Blocked =>
                ErpWriteResult.Fail(
                    ErpFailureCodes.WritebackBlocked,
                    "Writeback execution is blocked for this request."),
            ErpWritebackStatuses.Failed =>
                ErpWriteResult.Fail(
                    ErpFailureCodes.WritebackBlocked,
                    "Writeback execution previously failed."),
            ErpWritebackStatuses.Superseded =>
                ErpWriteResult.Fail(
                    ErpFailureCodes.WritebackBlocked,
                    "Writeback request has been superseded."),
            _ => null,
        };

    private static string? ResolveWritebackCapability(string commandType) =>
        commandType switch
        {
            ErpWritebackCommandTypes.SalesOrderCreateDraft => ErpCapabilities.SalesOrdersWriteDraft,
            _ => null,
        };

    private static string? ValidateCanonicalPayload(string commandType, string payloadJson)
    {
        if (!string.Equals(commandType, ErpWritebackCommandTypes.SalesOrderCreateDraft, StringComparison.Ordinal))
        {
            return $"Unsupported writeback command type '{commandType}'.";
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return "Writeback payload must be a JSON object.";
            }

            var root = document.RootElement;
            if (!TryGetNonEmptyString(root, "customerExternalId", out _))
            {
                return "Writeback payload requires customerExternalId.";
            }

            if (!TryGetNonEmptyString(root, "orderReference", out _))
            {
                return "Writeback payload requires orderReference.";
            }

            if (root.TryGetProperty("requestedDeliveryDateUtc", out var deliveryElement)
                && deliveryElement.ValueKind != JsonValueKind.Null
                && deliveryElement.ValueKind != JsonValueKind.String)
            {
                return "Writeback payload requestedDeliveryDateUtc must be a string or null.";
            }

            if (!root.TryGetProperty("lines", out var linesElement)
                || linesElement.ValueKind != JsonValueKind.Array
                || linesElement.GetArrayLength() == 0)
            {
                return "Writeback payload requires at least one line.";
            }

            var lineNumber = 0;
            foreach (var line in linesElement.EnumerateArray())
            {
                lineNumber++;
                if (line.ValueKind != JsonValueKind.Object)
                {
                    return $"Writeback payload line {lineNumber} must be an object.";
                }

                if (!TryGetNonEmptyString(line, "productExternalId", out _))
                {
                    return $"Writeback payload line {lineNumber} requires productExternalId.";
                }

                if (!line.TryGetProperty("quantity", out var quantityElement)
                    || quantityElement.ValueKind != JsonValueKind.Number
                    || !quantityElement.TryGetDecimal(out var quantity)
                    || quantity <= 0)
                {
                    return $"Writeback payload line {lineNumber} requires a positive quantity.";
                }

                if (!TryGetNonEmptyString(line, "unit", out _))
                {
                    return $"Writeback payload line {lineNumber} requires unit.";
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return "Writeback payload must be valid JSON.";
        }
    }

    private static bool TryGetNonEmptyString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static ErpWritebackSummaryReadModel MapSummary(ErpWritebackRequest request)
    {
        var latestApproval = request.Approvals
            .OrderByDescending(a => a.DecidedAtUtc)
            .ThenByDescending(a => a.Id)
            .FirstOrDefault();

        var payloadSummary = BuildPayloadSummary(request.CommandType, request.PayloadJson);

        return new ErpWritebackSummaryReadModel(
            request.Id,
            request.ConnectionProfileId,
            request.Provider,
            request.CommandType,
            request.Status,
            request.RequestedBySubjectId,
            request.RequestedAtUtc,
            request.SubmittedAtUtc,
            latestApproval?.Decision,
            latestApproval?.DecidedBySubjectId,
            latestApproval?.DecidedAtUtc,
            request.ExternalExecutionReference,
            request.LastErrorCode,
            payloadSummary.CustomerExternalId,
            payloadSummary.OrderReference,
            payloadSummary.LineCount,
            request.Version);
    }

    private static ErpWritebackDetailReadModel MapDetail(ErpWritebackRequest request)
    {
        var approvals = request.Approvals
            .OrderBy(a => a.DecidedAtUtc)
            .ThenBy(a => a.Id)
            .Select(a => new ErpWritebackApprovalReadModel(
                a.Id,
                a.Decision,
                a.DecidedBySubjectId,
                a.DecidedAtUtc,
                a.RequestVersion,
                a.PayloadHash,
                a.Comment))
            .ToList();

        return new ErpWritebackDetailReadModel(
            request.Id,
            request.ConnectionProfileId,
            request.Provider,
            request.CommandType,
            request.TargetEntityType,
            request.TargetExternalId,
            request.Status,
            request.PayloadHash,
            request.IdempotencyKey,
            request.RequestedBySubjectId,
            request.RequestedAtUtc,
            request.SubmittedAtUtc,
            request.ApprovedPayloadHash,
            request.ApprovedVersion,
            request.ExecutedAtUtc,
            request.ExternalExecutionReference,
            request.LastErrorCode,
            request.LastErrorMessage,
            request.Version,
            BuildPayloadSummary(request.CommandType, request.PayloadJson),
            approvals);
    }

    private static ErpWritebackPayloadSummaryReadModel BuildPayloadSummary(string commandType, string payloadJson)
    {
        if (!string.Equals(commandType, ErpWritebackCommandTypes.SalesOrderCreateDraft, StringComparison.Ordinal))
        {
            return new ErpWritebackPayloadSummaryReadModel(null, null, null, 0, []);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SalesOrderCreateDraftPayload>(payloadJson, PayloadJsonOptions);
            if (payload is null)
            {
                return new ErpWritebackPayloadSummaryReadModel(null, null, null, 0, []);
            }

            var lines = payload.Lines?
                .Where(line => line is not null)
                .Select(line => new ErpWritebackLineSummaryReadModel(
                    line!.ProductExternalId ?? string.Empty,
                    line.Quantity,
                    line.Unit ?? string.Empty))
                .ToList() ?? [];

            DateTimeOffset? requestedDeliveryDateUtc = null;
            if (!string.IsNullOrWhiteSpace(payload.RequestedDeliveryDateUtc)
                && DateTimeOffset.TryParse(payload.RequestedDeliveryDateUtc, out var parsedDelivery))
            {
                requestedDeliveryDateUtc = parsedDelivery;
            }

            return new ErpWritebackPayloadSummaryReadModel(
                payload.CustomerExternalId,
                payload.OrderReference,
                requestedDeliveryDateUtc,
                lines.Count,
                lines);
        }
        catch (JsonException)
        {
            return new ErpWritebackPayloadSummaryReadModel(null, null, null, 0, []);
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? SanitizeErrorMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var trimmed = message.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    private sealed class SalesOrderCreateDraftPayload
    {
        public string? CustomerExternalId { get; set; }
        public string? OrderReference { get; set; }
        public string? RequestedDeliveryDateUtc { get; set; }
        public List<SalesOrderCreateDraftLinePayload>? Lines { get; set; }
    }

    private sealed class SalesOrderCreateDraftLinePayload
    {
        public string? ProductExternalId { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }
}

public sealed class UnavailableErpWritebackRepository : IErpWritebackRepository
{
    public Task<IReadOnlyList<ErpWritebackSummaryReadModel>> ListWritebacksAsync(
        ErpOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ErpWritebackSummaryReadModel>>([]);

    public Task<ErpWritebackDetailReadModel?> GetWritebackAsync(
        ErpOperationContext context,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ErpWritebackDetailReadModel?>(null);

    public Task<ErpWriteResult> CreateWritebackAsync(
        ErpOperationContext context,
        CreateErpWritebackCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable, "ERP writeback service unavailable."));

    public Task<ErpWriteResult> SubmitWritebackAsync(
        ErpOperationContext context,
        SubmitErpWritebackCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable, "ERP writeback service unavailable."));

    public Task<ErpWriteResult> ApproveWritebackAsync(
        ErpOperationContext context,
        ApproveErpWritebackCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable, "ERP writeback service unavailable."));

    public Task<ErpWriteResult> RejectWritebackAsync(
        ErpOperationContext context,
        RejectErpWritebackCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable, "ERP writeback service unavailable."));

    public Task<ErpWriteResult> ExecuteWritebackAsync(
        ErpOperationContext context,
        ExecuteErpWritebackCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable, "ERP writeback service unavailable."));
}

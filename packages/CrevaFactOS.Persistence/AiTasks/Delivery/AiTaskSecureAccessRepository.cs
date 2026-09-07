using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Persistence.AiTasks.Delivery;

public static class AiTaskDeliveryWorkerState
{
    public static DateTimeOffset? LastHeartbeatUtc { get; set; }
}

public sealed class AiTaskSecureAccessRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    AiTaskSecureAccessTokenService tokenService,
    TenantContextAccessor tenantContextAccessor,
    IOptions<AiTaskDeliveryOptions> deliveryOptions) : IAiTaskSecureAccessRepository
{
    private readonly bool enableClosureTtlPolicies = deliveryOptions.Value.EnableClosureTtlPolicies;
    public Task<AiTaskSecureAccessIssueResult> IssueAsync(
        AiTaskOperationContext context,
        IssueAiTaskSecureAccessCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (!AiTaskSecureAccessScopes.All.Contains(command.Scope))
                {
                    return IssueFail(AiTaskSecureAccessFailureCodes.ValidationFailed, "Invalid scope.");
                }

                if (!AiTaskSecureAccessTtlPolicies.IsValidPolicy(command.TtlPolicy, enableClosureTtlPolicies))
                {
                    return IssueFail(AiTaskSecureAccessFailureCodes.ValidationFailed, "Invalid TTL policy.");
                }

                var task = await dbContext.AiTasks.AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.TenantId == context.TenantId
                             && t.FacilityId == context.FacilityId
                             && t.Id == command.AiTaskId,
                        ct);

                if (task is null)
                {
                    return IssueFail(AiTaskSecureAccessFailureCodes.NotFound, "AI task not found.");
                }

                var now = DateTimeOffset.UtcNow;
                var expiresAt = now.Add(AiTaskSecureAccessTtlPolicies.ResolveDuration(command.TtlPolicy, enableClosureTtlPolicies));
                var grantId = Guid.NewGuid();
                var token = tokenService.IssueToken(context.TenantId, grantId, expiresAt);

                var grant = new AiTaskSecureAccessGrant
                {
                    Id = grantId,
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    AiTaskId = command.AiTaskId,
                    Scope = command.Scope,
                    TokenHash = token.TokenHash,
                    IssuedByUserId = context.IdentitySubjectId,
                    IssuedAtUtc = now,
                    ExpiresAtUtc = expiresAt,
                    AccessCount = 0,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };

                dbContext.AiTaskSecureAccessGrants.Add(grant);
                await AppendAuditAsync(context, task, AiTaskDeliveryAuditEventTypes.SecureAccessIssued, now, ct);
                await dbContext.SaveChangesAsync(ct);

                return new AiTaskSecureAccessIssueResult(
                    true,
                    null,
                    null,
                    MapGrant(grant, true),
                    token.RawToken,
                    "/gorev");
            },
            cancellationToken);

    public Task<IReadOnlyList<AiTaskSecureAccessGrantReadModel>> ListForTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var grants = await dbContext.AiTaskSecureAccessGrants.AsNoTracking()
                    .Where(g => g.TenantId == context.TenantId && g.AiTaskId == taskId)
                    .OrderByDescending(g => g.IssuedAtUtc)
                    .ToListAsync(ct);

                var now = DateTimeOffset.UtcNow;
                return (IReadOnlyList<AiTaskSecureAccessGrantReadModel>)grants
                    .Select(g => MapGrant(g, g.RevokedAtUtc is null && g.ExpiresAtUtc > now))
                    .ToList();
            },
            cancellationToken);

    public Task<AiTaskSecureAccessIssueResult> RevokeAsync(
        AiTaskOperationContext context,
        Guid grantId,
        string? reason,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var grant = await dbContext.AiTaskSecureAccessGrants
                    .FirstOrDefaultAsync(g => g.TenantId == context.TenantId && g.Id == grantId, ct);

                if (grant is null)
                {
                    return IssueFail(AiTaskSecureAccessFailureCodes.NotFound, "Grant not found.");
                }

                if (grant.RevokedAtUtc is not null)
                {
                    return new AiTaskSecureAccessIssueResult(true, null, null, MapGrant(grant, false), null, null);
                }

                var now = DateTimeOffset.UtcNow;
                grant.RevokedAtUtc = now;
                grant.RevokedByUserId = context.IdentitySubjectId;
                grant.RevocationReason = reason?.Trim();
                grant.UpdatedAtUtc = now;

                var task = await dbContext.AiTasks.AsNoTracking()
                    .FirstAsync(t => t.Id == grant.AiTaskId && t.TenantId == context.TenantId, ct);
                await AppendAuditAsync(context, task, AiTaskDeliveryAuditEventTypes.SecureAccessRevoked, now, ct);
                await dbContext.SaveChangesAsync(ct);

                return new AiTaskSecureAccessIssueResult(true, null, null, MapGrant(grant, false), null, null);
            },
            cancellationToken);

    public Task<AiTaskSecureAccessIssueResult> RotateAsync(
        AiTaskOperationContext context,
        Guid grantId,
        string ttlPolicy,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (!AiTaskSecureAccessTtlPolicies.IsValidPolicy(ttlPolicy, enableClosureTtlPolicies))
                {
                    return IssueFail(AiTaskSecureAccessFailureCodes.ValidationFailed, "Invalid TTL policy.");
                }

                var oldGrant = await dbContext.AiTaskSecureAccessGrants
                    .FirstOrDefaultAsync(g => g.TenantId == context.TenantId && g.Id == grantId, ct);

                if (oldGrant is null)
                {
                    return IssueFail(AiTaskSecureAccessFailureCodes.NotFound, "Grant not found.");
                }

                var now = DateTimeOffset.UtcNow;
                if (oldGrant.RevokedAtUtc is null)
                {
                    oldGrant.RevokedAtUtc = now;
                    oldGrant.RevokedByUserId = context.IdentitySubjectId;
                    oldGrant.RevocationReason = "rotated";
                    oldGrant.UpdatedAtUtc = now;
                }

                var expiresAt = now.Add(AiTaskSecureAccessTtlPolicies.ResolveDuration(ttlPolicy, enableClosureTtlPolicies));
                var newGrantId = Guid.NewGuid();
                var token = tokenService.IssueToken(context.TenantId, newGrantId, expiresAt);
                var newGrant = new AiTaskSecureAccessGrant
                {
                    Id = newGrantId,
                    TenantId = context.TenantId,
                    FacilityId = oldGrant.FacilityId,
                    AiTaskId = oldGrant.AiTaskId,
                    Scope = oldGrant.Scope,
                    TokenHash = token.TokenHash,
                    IssuedByUserId = context.IdentitySubjectId,
                    IssuedAtUtc = now,
                    ExpiresAtUtc = expiresAt,
                    AccessCount = 0,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };

                dbContext.AiTaskSecureAccessGrants.Add(newGrant);
                var task = await dbContext.AiTasks.AsNoTracking()
                    .FirstAsync(t => t.Id == oldGrant.AiTaskId && t.TenantId == context.TenantId, ct);
                await AppendAuditAsync(context, task, AiTaskDeliveryAuditEventTypes.SecureAccessRotated, now, ct);
                await dbContext.SaveChangesAsync(ct);

                return new AiTaskSecureAccessIssueResult(
                    true,
                    null,
                    null,
                    MapGrant(newGrant, true),
                    token.RawToken,
                    "/gorev");
            },
            cancellationToken);

    public async Task<AiTaskSecureAccessResolveResult> ResolvePublicAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        if (!tokenService.TryResolve(rawToken, out var payload, out var tokenHash) || payload is null || tokenHash is null)
        {
            return new AiTaskSecureAccessResolveResult(false, AiTaskSecureAccessFailureCodes.InvalidToken, "Invalid token.", null);
        }

        if (!tenantContextAccessor.IsAvailable)
        {
            tenantContextAccessor.Set(new TenantContext(payload.TenantId, "secure-access", Guid.Empty));
        }

        return await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var grant = await dbContext.AiTaskSecureAccessGrants
                    .FirstOrDefaultAsync(g => g.TenantId == payload.TenantId && g.Id == payload.GrantId, ct);

                if (grant is null || !AiTaskSecureAccessTokenService.ConstantTimeEquals(grant.TokenHash, tokenHash))
                {
                    return new AiTaskSecureAccessResolveResult(
                        false,
                        AiTaskSecureAccessFailureCodes.Tampered,
                        "Invalid token.",
                        null);
                }

                var now = DateTimeOffset.UtcNow;
                if (grant.RevokedAtUtc is not null)
                {
                    return new AiTaskSecureAccessResolveResult(
                        false,
                        AiTaskSecureAccessFailureCodes.Revoked,
                        "Link revoked.",
                        null);
                }

                if (grant.ExpiresAtUtc <= now || payload.ExpiresAtUtc <= now)
                {
                    return new AiTaskSecureAccessResolveResult(
                        false,
                        AiTaskSecureAccessFailureCodes.Expired,
                        "Link expired.",
                        null);
                }

                var task = await dbContext.AiTasks.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenantId == grant.TenantId && t.Id == grant.AiTaskId, ct);

                if (task is null)
                {
                    return new AiTaskSecureAccessResolveResult(
                        false,
                        AiTaskSecureAccessFailureCodes.NotFound,
                        "Task unavailable.",
                        null);
                }

                grant.AccessCount += 1;
                grant.LastAccessedAtUtc = now;
                grant.UpdatedAtUtc = now;
                await AppendAuditAsync(
                    new AiTaskOperationContext(
                        grant.TenantId,
                        grant.FacilityId,
                        Guid.Empty,
                        Guid.Empty,
                        AiTaskPermissionKeys.Read,
                        null),
                    task,
                    AiTaskDeliveryAuditEventTypes.SecureAccessAccessed,
                    now,
                    ct);
                await dbContext.SaveChangesAsync(ct);

                return new AiTaskSecureAccessResolveResult(
                    true,
                    null,
                    null,
                    AiTaskSafeProjectionBuilder.Build(task, grant));
            },
            cancellationToken);
    }

    private async Task AppendAuditAsync(
        AiTaskOperationContext context,
        AiTask task,
        string eventType,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.AiTaskAuditEvents.Add(new AiTaskAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            TaskId = task.Id,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            ActorType = context.IdentitySubjectId == Guid.Empty ? AiTaskActorTypes.System : AiTaskActorTypes.User,
            ActorId = context.IdentitySubjectId == Guid.Empty ? null : context.IdentitySubjectId.ToString(),
            ActorLabel = "Secure Access",
            CorrelationId = context.CorrelationId,
        });
        await Task.CompletedTask;
    }

    private static AiTaskSecureAccessIssueResult IssueFail(string code, string message) =>
        new(false, code, message, null, null, null);

    private static AiTaskSecureAccessGrantReadModel MapGrant(AiTaskSecureAccessGrant grant, bool isActive) => new(
        grant.Id,
        grant.AiTaskId,
        grant.Scope,
        grant.IssuedAtUtc,
        grant.ExpiresAtUtc,
        grant.RevokedAtUtc,
        grant.AccessCount,
        grant.LastAccessedAtUtc,
        isActive);
}

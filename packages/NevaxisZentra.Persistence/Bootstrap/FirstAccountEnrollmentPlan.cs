using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.SaasIdentity;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Bootstrap;

public static class FirstAccountEnrollmentEventTypes
{
    public const string Completed = "bootstrap.enrollment.completed";
}

/// <summary>
/// The complete, deterministic set of rows a first-account enrollment creates. Pure (no I/O) so the
/// shape - including "no secret in any audit payload" - is unit-testable.
/// </summary>
public sealed class FirstAccountEnrollmentPlan
{
    public const string OwnerRoleKey = "tenant-owner";

    public required Tenant Tenant { get; init; }

    public required IdentitySubject IdentitySubject { get; init; }

    public required SaasIdentityAccount Account { get; init; }

    public required SaasIdentityPasswordCredential Credential { get; init; }

    public required TenantMembership Membership { get; init; }

    public required TenantRole Role { get; init; }

    public required IReadOnlyList<TenantRolePermissionAssignment> PermissionAssignments { get; init; }

    public required TenantMembershipRole MembershipRole { get; init; }

    public required Facility Facility { get; init; }

    public required FacilityMembership FacilityMembership { get; init; }

    public required SaasIdentitySecurityAuditEvent AuditEvent { get; init; }

    public required PackagedInstallSubscription License { get; init; }

    public static FirstAccountEnrollmentPlan Build(
        FirstAccountEnrollmentInput input,
        IReadOnlyCollection<Guid> activePermissionDefinitionIds,
        DateTimeOffset now,
        PasswordHasher<SaasIdentityAccount> passwordHasher,
        PackagedInstallLicenseOptions? licenseOptions = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(activePermissionDefinitionIds);
        ArgumentNullException.ThrowIfNull(passwordHasher);

        var tenantId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var tenant = new Tenant
        {
            Id = tenantId,
            TenantKey = input.OrganizationCode,
            DisplayName = input.OrganizationName,
            Status = TenantStatus.Active,
            CreatedAtUtc = now,
        };

        var subject = new IdentitySubject
        {
            Id = Guid.NewGuid(),
            ExternalIssuer = SaasIdentityConstants.Issuer,
            ExternalSubject = accountId.ToString(),
            Status = IdentitySubjectStatus.Active,
            CreatedAtUtc = now,
        };

        var account = new SaasIdentityAccount
        {
            Id = accountId,
            AccountReference = SaasIdentityTokenUtilities.GenerateReference("acc"),
            EmailNormalized = input.OwnerEmailNormalized,
            DisplayName = input.OwnerDisplayName,
            LifecycleStatus = SaasIdentityAccountLifecycleStatuses.Active,
            EmailVerifiedAtUtc = now,
            SecurityStamp = Guid.NewGuid(),
            IdentitySubjectId = subject.Id,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };

        var credential = new SaasIdentityPasswordCredential
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PasswordHash = passwordHasher.HashPassword(account, input.Password),
            PasswordHashVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        var membership = new TenantMembership
        {
            Id = membershipId,
            TenantId = tenantId,
            IdentitySubjectId = subject.Id,
            MembershipReference = TenantMembership.CreateReference(membershipId),
            Status = TenantMembershipStatus.Active,
            AuthorizationVersion = 1,
            CreatedAtUtc = now,
        };

        var role = new TenantRole
        {
            Id = roleId,
            TenantId = tenantId,
            Key = OwnerRoleKey,
            DisplayName = "Tenant Owner",
            Description = "First-account owner created by the one-time installation enrollment.",
            IsProtected = true,
            Status = AuthorizationStatus.Active,
            CreatedByActorReference = account.AccountReference,
            CreatedAtUtc = now,
        };

        var assignments = activePermissionDefinitionIds
            .Distinct()
            .Select(permissionId => new TenantRolePermissionAssignment
            {
                TenantId = tenantId,
                TenantRoleId = roleId,
                PermissionDefinitionId = permissionId,
            })
            .ToList();

        var membershipRole = new TenantMembershipRole
        {
            TenantId = tenantId,
            TenantMembershipId = membershipId,
            TenantRoleId = roleId,
            Status = AuthorizationStatus.Active,
            AssignedAtUtc = now,
        };

        var facility = new Facility
        {
            Id = input.FacilityId,
            TenantId = tenantId,
            Code = "MAIN",
            Name = "Ana Tesis",
            Status = FacilityStatus.Active,
            TimeZoneId = "Europe/Istanbul",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };

        var facilityMembership = new FacilityMembership
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facility.Id,
            TenantMembershipId = membershipId,
            Status = FacilityMembershipStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };

        // Audit evidence: identifiers and counts only. Never the token, password, hash or email.
        var audit = new SaasIdentitySecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountId = accountId,
            EventType = FirstAccountEnrollmentEventTypes.Completed,
            PayloadJson = JsonSerializer.Serialize(new
            {
                accountReference = account.AccountReference,
                tenantKey = tenant.TenantKey,
                membershipReference = membership.MembershipReference,
                roleKey = role.Key,
                grantedPermissionCount = assignments.Count,
                tokenIssuedCount = input.TokenIssuedCount,
                channel = "localhost-onboarding",
            }),
            OccurredAtUtc = now,
        };

        return new FirstAccountEnrollmentPlan
        {
            Tenant = tenant,
            IdentitySubject = subject,
            Account = account,
            Credential = credential,
            Membership = membership,
            Role = role,
            PermissionAssignments = assignments,
            MembershipRole = membershipRole,
            Facility = facility,
            FacilityMembership = facilityMembership,
            AuditEvent = audit,
            License = PackagedInstallLicense.BuildSubscription(tenantId, now, licenseOptions),
        };
    }
}

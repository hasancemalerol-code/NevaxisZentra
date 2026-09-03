using CrevaFactOS.Persistence.SaasCommercial;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotSaasCommercialSeed
{
    public const string DevPlanCode = "dev-pilot-local-commercial";

    internal static async Task EnsureSaasCommercialDevelopmentCatalogAsync(
        CrevaFactOSDbContext dbContext,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureEntitlementDefinitionsAsync(dbContext, seedTimestampUtc, cancellationToken);
        await EnsureDevelopmentPlanAsync(dbContext, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsureMonthlyDraftVersionAsync(dbContext, seedTimestampUtc, cancellationToken);
        await EnsureAnnualDraftVersionAsync(dbContext, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsureEntitlementDefinitionsAsync(
        CrevaFactOSDbContext dbContext,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var definitions = new (Guid Id, string Code, string Label, string? Description, string ValueKind)[]
        {
            (DevelopmentPilotIds.SaasCommercialFacilityLimitEntitlementId,
                "facility-limit",
                "Tesis limiti",
                "Plan kapsamındaki dahil tesis sayısı.",
                SaasCommercialEntitlementValueKinds.FacilityLimit),
            (DevelopmentPilotIds.SaasCommercialConnectedMachineLimitEntitlementId,
                "connected-machine-limit",
                "Bağlı makine limiti",
                "Plan kapsamındaki dahil bağlı makine sayısı.",
                SaasCommercialEntitlementValueKinds.ConnectedMachineLimit),
            (DevelopmentPilotIds.SaasCommercialUserLimitEntitlementId,
                "user-limit",
                "Kullanıcı limiti",
                "Opsiyonel kullanıcı limiti.",
                SaasCommercialEntitlementValueKinds.UserLimit),
            (DevelopmentPilotIds.SaasCommercialShiftWorkforceModuleEntitlementId,
                "module.shift-workforce",
                "Vardiya işgücü modülü",
                "Vardiya işgücü modül erişimi.",
                SaasCommercialEntitlementValueKinds.ModuleAccess),
        };

        foreach (var definition in definitions)
        {
            if (await dbContext.SaasCommercialEntitlementDefinitions.AsNoTracking()
                    .AnyAsync(d => d.Id == definition.Id, cancellationToken))
            {
                continue;
            }

            dbContext.SaasCommercialEntitlementDefinitions.Add(new SaasCommercialEntitlementDefinition
            {
                Id = definition.Id,
                Code = definition.Code,
                CodeNormalized = SaasCommercialValueConversions.NormalizeEntitlementCode(definition.Code),
                DisplayNameTr = definition.Label,
                DescriptionTr = definition.Description,
                ValueKind = definition.ValueKind,
                CreatedAtUtc = seedTimestampUtc,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDevelopmentPlanAsync(
        CrevaFactOSDbContext dbContext,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.SaasCommercialPlans.AsNoTracking()
                .AnyAsync(p => p.Id == DevelopmentPilotIds.SaasCommercialDevPlanId, cancellationToken))
        {
            return;
        }

        dbContext.SaasCommercialPlans.Add(new SaasCommercialPlan
        {
            Id = DevelopmentPilotIds.SaasCommercialDevPlanId,
            PlanCode = DevPlanCode,
            PlanCodeNormalized = SaasCommercialValueConversions.NormalizePlanCode(DevPlanCode),
            DisplayNameTr = "Yerel Geliştirme / Pilot — Ticari Teklif Değildir",
            DescriptionTr = "Yalnızca yerel geliştirme ve doğrulama için taslak katalog kaydı. Üretim satışı için kullanılamaz.",
            LifecycleState = SaasCommercialPlanLifecycleStates.Draft,
            AvailabilityState = SaasCommercialAvailabilityStates.Draft,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });

        dbContext.SaasCommercialAuditEvents.Add(new SaasCommercialAuditEvent
        {
            Id = DevelopmentPilotIds.SaasCommercialDevPlanAuditEventId,
            EventType = SaasCommercialAuditEventTypes.PlanCreated,
            EntityType = SaasCommercialAuditEntityTypes.Plan,
            EntityId = DevelopmentPilotIds.SaasCommercialDevPlanId,
            PayloadJson = """{"planCode":"dev-pilot-local-commercial","provenance":"local-development-pilot"}""",
            ActorSubjectId = actorSubjectId,
            OccurredAtUtc = seedTimestampUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureMonthlyDraftVersionAsync(
        CrevaFactOSDbContext dbContext,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.SaasCommercialPlanVersions.AsNoTracking()
                .AnyAsync(v => v.Id == DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId, cancellationToken))
        {
            return;
        }

        dbContext.SaasCommercialPlanVersions.Add(new SaasCommercialPlanVersion
        {
            Id = DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId,
            PlanId = DevelopmentPilotIds.SaasCommercialDevPlanId,
            VersionNumber = 1,
            EffectiveFromUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EffectiveUntilUtc = null,
            BillingCadence = SaasCommercialBillingCadences.Monthly,
            TrialDays = 14,
            CurrencyCode = "TRY",
            Sellable = false,
            PublicationStatus = SaasCommercialPublicationStatuses.Draft,
            CommercialNotes = "Taslak aylık sürüm — onaylı üretim fiyatı yok.",
            Provenance = SaasCommercialCatalogProvenance.NonProductionDraft,
            CreatedAtUtc = seedTimestampUtc,
            Version = 1,
        });

        AddMonthlyVersionChildren(dbContext, seedTimestampUtc);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAnnualDraftVersionAsync(
        CrevaFactOSDbContext dbContext,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.SaasCommercialPlanVersions.AsNoTracking()
                .AnyAsync(v => v.Id == DevelopmentPilotIds.SaasCommercialDevAnnualVersionId, cancellationToken))
        {
            return;
        }

        dbContext.SaasCommercialPlanVersions.Add(new SaasCommercialPlanVersion
        {
            Id = DevelopmentPilotIds.SaasCommercialDevAnnualVersionId,
            PlanId = DevelopmentPilotIds.SaasCommercialDevPlanId,
            VersionNumber = 2,
            EffectiveFromUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EffectiveUntilUtc = null,
            BillingCadence = SaasCommercialBillingCadences.Annual,
            TrialDays = 14,
            CurrencyCode = "TRY",
            Sellable = false,
            PublicationStatus = SaasCommercialPublicationStatuses.Draft,
            CommercialNotes = "Taslak yıllık sürüm — onaylı üretim fiyatı yok.",
            Provenance = SaasCommercialCatalogProvenance.NonProductionDraft,
            CreatedAtUtc = seedTimestampUtc,
            Version = 1,
        });

        AddAnnualVersionChildren(dbContext, seedTimestampUtc);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void AddMonthlyVersionChildren(CrevaFactOSDbContext dbContext, DateTimeOffset seedTimestampUtc)
    {
        AddPriceComponents(
            dbContext,
            DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyTenantBaseComponentId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyIncludedFacilityComponentId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyAdditionalFacilityComponentId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyIncludedMachineComponentId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyAdditionalMachineComponentId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyIncludedUserComponentId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyAdditionalUserComponentId);

        AddEntitlementGrants(
            dbContext,
            DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyFacilityGrantId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyMachineGrantId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyUserGrantId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyModuleGrantId);

        AddPaymentMethodCapabilities(
            dbContext,
            DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyCardCapabilityId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyBankTransferCapabilityId,
            DevelopmentPilotIds.SaasCommercialDevMonthlyEnterpriseInvoiceCapabilityId);

        dbContext.SaasCommercialGracePeriodPolicies.Add(new SaasCommercialGracePeriodPolicy
        {
            Id = DevelopmentPilotIds.SaasCommercialDevMonthlyGracePolicyId,
            PlanVersionId = DevelopmentPilotIds.SaasCommercialDevMonthlyVersionId,
            GracePeriodDays = 14,
            RestrictedBehaviorToken = "read-only-restricted",
            SuspensionEligibilityPolicy = "grace-then-suspend",
            OperatorExplanationTr = "Ödeme gecikmesi durumunda 14 gün kısıtlı erişim sağlanır; bu paket yalnızca politika sözleşmesini tanımlar.",
        });
    }

    private static void AddAnnualVersionChildren(CrevaFactOSDbContext dbContext, DateTimeOffset seedTimestampUtc)
    {
        AddPriceComponents(
            dbContext,
            DevelopmentPilotIds.SaasCommercialDevAnnualVersionId,
            DevelopmentPilotIds.SaasCommercialDevAnnualTenantBaseComponentId,
            DevelopmentPilotIds.SaasCommercialDevAnnualIncludedFacilityComponentId,
            DevelopmentPilotIds.SaasCommercialDevAnnualAdditionalFacilityComponentId,
            DevelopmentPilotIds.SaasCommercialDevAnnualIncludedMachineComponentId,
            DevelopmentPilotIds.SaasCommercialDevAnnualAdditionalMachineComponentId,
            DevelopmentPilotIds.SaasCommercialDevAnnualIncludedUserComponentId,
            DevelopmentPilotIds.SaasCommercialDevAnnualAdditionalUserComponentId);

        AddEntitlementGrants(
            dbContext,
            DevelopmentPilotIds.SaasCommercialDevAnnualVersionId,
            DevelopmentPilotIds.SaasCommercialDevAnnualFacilityGrantId,
            DevelopmentPilotIds.SaasCommercialDevAnnualMachineGrantId,
            DevelopmentPilotIds.SaasCommercialDevAnnualUserGrantId,
            DevelopmentPilotIds.SaasCommercialDevAnnualModuleGrantId);

        AddPaymentMethodCapabilities(
            dbContext,
            DevelopmentPilotIds.SaasCommercialDevAnnualVersionId,
            DevelopmentPilotIds.SaasCommercialDevAnnualCardCapabilityId,
            DevelopmentPilotIds.SaasCommercialDevAnnualBankTransferCapabilityId,
            DevelopmentPilotIds.SaasCommercialDevAnnualEnterpriseInvoiceCapabilityId);

        dbContext.SaasCommercialGracePeriodPolicies.Add(new SaasCommercialGracePeriodPolicy
        {
            Id = DevelopmentPilotIds.SaasCommercialDevAnnualGracePolicyId,
            PlanVersionId = DevelopmentPilotIds.SaasCommercialDevAnnualVersionId,
            GracePeriodDays = 14,
            RestrictedBehaviorToken = "read-only-restricted",
            SuspensionEligibilityPolicy = "grace-then-suspend",
            OperatorExplanationTr = "Ödeme gecikmesi durumunda 14 gün kısıtlı erişim sağlanır; bu paket yalnızca politika sözleşmesini tanımlar.",
        });
    }

    private static void AddPriceComponents(
        CrevaFactOSDbContext dbContext,
        Guid versionId,
        Guid tenantBaseId,
        Guid includedFacilityId,
        Guid additionalFacilityId,
        Guid includedMachineId,
        Guid additionalMachineId,
        Guid includedUserId,
        Guid additionalUserId)
    {
        var components = new (Guid Id, string Type, int Sort)[]
        {
            (tenantBaseId, SaasCommercialPriceComponentTypes.TenantBase, 10),
            (includedFacilityId, SaasCommercialPriceComponentTypes.IncludedFacility, 20),
            (additionalFacilityId, SaasCommercialPriceComponentTypes.AdditionalFacility, 30),
            (includedMachineId, SaasCommercialPriceComponentTypes.IncludedConnectedMachine, 40),
            (additionalMachineId, SaasCommercialPriceComponentTypes.AdditionalConnectedMachine, 50),
            (includedUserId, SaasCommercialPriceComponentTypes.IncludedUser, 60),
            (additionalUserId, SaasCommercialPriceComponentTypes.AdditionalUser, 70),
        };

        foreach (var component in components)
        {
            dbContext.SaasCommercialPriceComponents.Add(new SaasCommercialPriceComponent
            {
                Id = component.Id,
                PlanVersionId = versionId,
                ComponentType = component.Type,
                Amount = null,
                CurrencyCode = "TRY",
                SortOrder = component.Sort,
            });
        }
    }

    private static void AddEntitlementGrants(
        CrevaFactOSDbContext dbContext,
        Guid versionId,
        Guid facilityGrantId,
        Guid machineGrantId,
        Guid userGrantId,
        Guid moduleGrantId)
    {
        dbContext.SaasCommercialPlanVersionEntitlements.AddRange(
            new SaasCommercialPlanVersionEntitlement
            {
                Id = facilityGrantId,
                PlanVersionId = versionId,
                EntitlementDefinitionId = DevelopmentPilotIds.SaasCommercialFacilityLimitEntitlementId,
                Semantics = SaasCommercialEntitlementSemantics.Limited,
                IntegerLimitValue = 2,
            },
            new SaasCommercialPlanVersionEntitlement
            {
                Id = machineGrantId,
                PlanVersionId = versionId,
                EntitlementDefinitionId = DevelopmentPilotIds.SaasCommercialConnectedMachineLimitEntitlementId,
                Semantics = SaasCommercialEntitlementSemantics.Limited,
                IntegerLimitValue = 5,
            },
            new SaasCommercialPlanVersionEntitlement
            {
                Id = userGrantId,
                PlanVersionId = versionId,
                EntitlementDefinitionId = DevelopmentPilotIds.SaasCommercialUserLimitEntitlementId,
                Semantics = SaasCommercialEntitlementSemantics.Limited,
                IntegerLimitValue = 25,
            },
            new SaasCommercialPlanVersionEntitlement
            {
                Id = moduleGrantId,
                PlanVersionId = versionId,
                EntitlementDefinitionId = DevelopmentPilotIds.SaasCommercialShiftWorkforceModuleEntitlementId,
                Semantics = SaasCommercialEntitlementSemantics.Unlimited,
                BooleanValue = true,
            });
    }

    private static void AddPaymentMethodCapabilities(
        CrevaFactOSDbContext dbContext,
        Guid versionId,
        Guid cardId,
        Guid bankTransferId,
        Guid enterpriseInvoiceId)
    {
        dbContext.SaasCommercialPaymentMethodCapabilities.AddRange(
            new SaasCommercialPaymentMethodCapability
            {
                Id = cardId,
                PlanVersionId = versionId,
                MethodType = SaasCommercialPaymentMethodTypes.Card,
                Enabled = true,
                ManualVerificationRequired = false,
                RecurringCapable = true,
                DisplayOrder = 10,
            },
            new SaasCommercialPaymentMethodCapability
            {
                Id = bankTransferId,
                PlanVersionId = versionId,
                MethodType = SaasCommercialPaymentMethodTypes.BankTransfer,
                Enabled = true,
                ManualVerificationRequired = true,
                RecurringCapable = false,
                DisplayOrder = 20,
            },
            new SaasCommercialPaymentMethodCapability
            {
                Id = enterpriseInvoiceId,
                PlanVersionId = versionId,
                MethodType = SaasCommercialPaymentMethodTypes.EnterpriseInvoice,
                Enabled = true,
                ManualVerificationRequired = true,
                RecurringCapable = true,
                DisplayOrder = 30,
            });
    }
}

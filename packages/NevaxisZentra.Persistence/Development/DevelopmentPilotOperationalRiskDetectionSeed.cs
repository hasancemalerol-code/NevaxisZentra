using NevaxisZentra.Persistence.Energy;
using NevaxisZentra.Persistence.IotPlc;
using NevaxisZentra.Persistence.Maintenance;
using NevaxisZentra.Persistence.OperationalRisk;
// Maintenance predictive assessments are read-only source evidence — never seeded here.
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotOperationalRiskDetectionSeed
{
    internal static async Task EnsureOperationalRiskDetectionPilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = OperationalRiskDetectionConstants.DevelopmentPilotProvenance;
        var observedAt = DevelopmentPilotIds.OperationalRiskPilotSeedTimeUtc;
        var iotEvalTime = DevelopmentPilotIds.IotPlc2PilotEvaluationTimeUtc;

        await EnsurePilotConnectivityAssessmentsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            iotEvalTime,
            cancellationToken);

        await EnsurePilotDetectionProfileAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        var terminalDetectionKey = OperationalRiskDetectionKeyBuilder.Build(
            tenantId,
            facilityId,
            OperationalRiskCategories.Energy,
            OperationalRiskScopeTypes.EnergyMeasurementPoint,
            DevelopmentPilotIds.EnergyPilotPointCompressorId.ToString(),
            OperationalRiskSourceModules.Energy,
            DevelopmentPilotIds.Energy2PilotAssessmentPeakDemandRef,
            "energy-attention");

        await EnsureTerminalRecurrenceCaseAsync(
            dbContext,
            tenantId,
            facilityId,
            terminalDetectionKey,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            observedAt,
            cancellationToken);
    }

    private static async Task EnsurePilotConnectivityAssessmentsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        DateTimeOffset evalTime,
        CancellationToken cancellationToken)
    {
        var windowStart = evalTime.AddHours(-1);
        var windowEnd = evalTime;
        var provenance = IotPlcIntelligenceConstants.DevelopmentPilotProvenance;

        await EnsureConnectivityAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.IotPlc2PilotAssessHealthyConnectivityId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId,
            windowStart,
            windowEnd,
            DevelopmentPilotIds.IotPlc2PilotProfileStandardId,
            IotPlcIntelligenceOutcomes.Healthy,
            0.95m,
            120,
            provenance,
            actorSubjectId,
            evalTime,
            seedTimestampUtc,
            cancellationToken);

        await EnsureConnectivityAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.IotPlc2PilotAssessOfflineEndpointId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointMqttId,
            windowStart,
            windowEnd,
            DevelopmentPilotIds.IotPlc2PilotProfileStandardId,
            IotPlcIntelligenceOutcomes.Unavailable,
            0.10m,
            60,
            provenance,
            actorSubjectId,
            evalTime,
            seedTimestampUtc,
            cancellationToken);

        await EnsureConnectivityAssessmentAsync(
            dbContext,
            DevelopmentPilotIds.IotPlc2PilotAssessStaleSignalId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointEdgeId,
            windowStart,
            windowEnd,
            DevelopmentPilotIds.IotPlc2PilotProfileStandardId,
            IotPlcIntelligenceOutcomes.Degraded,
            0.55m,
            7200,
            provenance,
            actorSubjectId,
            evalTime,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureConnectivityAssessmentAsync(
        NevaxisZentraDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid targetId,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        Guid profileId,
        string outcome,
        decimal availabilityRatio,
        int freshnessSeconds,
        string provenance,
        string actorSubjectId,
        DateTimeOffset evaluatedAtUtc,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.IotPlcConnectivityAssessments
            .IgnoreQueryFilters()
            .AnyAsync(a => a.Id == id, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.IotPlcConnectivityAssessments.Add(new IotPlcConnectivityAssessment
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            TargetType = IotPlcIntelligenceTargetTypes.Endpoint,
            TargetId = targetId,
            EvaluationWindowStartUtc = windowStart,
            EvaluationWindowEndUtc = windowEnd,
            QualityProfileId = profileId,
            EvidenceMode = IotPlcIntelligenceEvidenceModes.Simulated,
            Outcome = outcome,
            LeadingDriverCode = outcome,
            HealthObservationAvailabilityRatio = availabilityRatio,
            FreshnessSeconds = freshnessSeconds,
            RuleSetVersion = IotPlcIntelligenceConstants.RuleSetVersion,
            SourceProvenance = provenance,
            EvaluatedAtUtc = evaluatedAtUtc,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsurePilotDetectionProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.OperationalRiskDetectionProfiles
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == DevelopmentPilotIds.OperationalRiskDetectionPilotProfileId, cancellationToken);
        if (exists)
        {
            return;
        }

        var profile = new OperationalRiskDetectionProfile
        {
            Id = DevelopmentPilotIds.OperationalRiskDetectionPilotProfileId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProfileCode = DevelopmentPilotIds.OperationalRiskDetectionPilotProfileCode,
            DisplayName = "Development Cross-Module Detection Profile",
            Status = OperationalRiskDetectionProfileStatuses.Approved,
            RuleSetVersion = OperationalRiskDetectionConstants.RuleSetVersion,
            SourceProvenance = provenance,
            CreateIdempotencyKey = "opr2-pilot-profile-bootstrap",
            CreateIdempotencyPayloadFingerprint = "opr2-pilot-profile-bootstrap",
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        };

        foreach (var rule in new[]
                 {
                     Rule(
                         DevelopmentPilotIds.OperationalRiskDetectionPilotRuleEnergyAttentionId,
                         "energy-attention",
                         OperationalRiskSourceModules.Energy,
                         EnergyIntelligenceOutcomes.Attention,
                         OperationalRiskCategories.Energy,
                         4,
                         4,
                         OperationalRiskDetectionDriverReasonCodes.EnergyRisk,
                         10),
                     Rule(
                         DevelopmentPilotIds.OperationalRiskDetectionPilotRuleEnergyWithinTargetId,
                         "energy-within-target",
                         OperationalRiskSourceModules.Energy,
                         EnergyIntelligenceOutcomes.WithinTarget,
                         OperationalRiskCategories.Energy,
                         1,
                         1,
                         OperationalRiskDetectionDriverReasonCodes.WithinThreshold,
                         20),
                     Rule(
                         DevelopmentPilotIds.OperationalRiskDetectionPilotRuleIotHealthyId,
                         "iot-healthy",
                         OperationalRiskSourceModules.IotPlc,
                         IotPlcIntelligenceOutcomes.Healthy,
                         OperationalRiskCategories.IotConnectivity,
                         1,
                         1,
                         OperationalRiskDetectionDriverReasonCodes.WithinThreshold,
                         30),
                     Rule(
                         DevelopmentPilotIds.OperationalRiskDetectionPilotRuleIotUnavailableId,
                         "iot-unavailable",
                         OperationalRiskSourceModules.IotPlc,
                         IotPlcIntelligenceOutcomes.Unavailable,
                         OperationalRiskCategories.IotConnectivity,
                         4,
                         5,
                         OperationalRiskDetectionDriverReasonCodes.ConnectivityRisk,
                         40),
                     Rule(
                         DevelopmentPilotIds.OperationalRiskDetectionPilotRuleMaintenanceHighId,
                         "maintenance-high",
                         OperationalRiskSourceModules.Maintenance,
                         MaintenancePredictiveRiskBands.High,
                         OperationalRiskCategories.Maintenance,
                         4,
                         4,
                         OperationalRiskDetectionDriverReasonCodes.MaintenanceRisk,
                         50),
                     Rule(
                         DevelopmentPilotIds.OperationalRiskDetectionPilotRuleMaintenanceCriticalId,
                         "maintenance-critical",
                         OperationalRiskSourceModules.Maintenance,
                         MaintenancePredictiveRiskBands.Critical,
                         OperationalRiskCategories.Maintenance,
                         5,
                         5,
                         OperationalRiskDetectionDriverReasonCodes.MaintenanceRisk,
                         60),
                 })
        {
            profile.Rules.Add(rule);
        }

        dbContext.OperationalRiskDetectionProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        OperationalRiskDetectionRule Rule(
            Guid id,
            string code,
            string module,
            string outcome,
            string category,
            int likelihood,
            int impact,
            string driver,
            int priority) =>
            new()
            {
                Id = id,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProfileId = profile.Id,
                RuleCode = code,
                SourceModule = module,
                SourceOutcome = outcome,
                RiskCategory = category,
                DefaultLikelihood = likelihood,
                DefaultImpact = impact,
                DriverReasonCode = driver,
                PriorityOrder = priority,
                IsActive = true,
                SourceProvenance = provenance,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
            };
    }

    private static async Task EnsureTerminalRecurrenceCaseAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string detectionKey,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.OperationalRiskCases
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Id == DevelopmentPilotIds.OperationalRiskDetectionPilotTerminalCaseId, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.OperationalRiskCases.Add(new OperationalRiskCase
        {
            Id = DevelopmentPilotIds.OperationalRiskDetectionPilotTerminalCaseId,
            TenantId = tenantId,
            FacilityId = facilityId,
            CaseReference = DevelopmentPilotIds.OperationalRiskDetectionPilotTerminalCaseReference,
            Category = OperationalRiskCategories.Energy,
            ScopeType = OperationalRiskScopeTypes.EnergyMeasurementPoint,
            ScopeReference = DevelopmentPilotIds.EnergyPilotPointCompressorId.ToString(),
            Title = "Terminal energy detection case for recurrence proof",
            Description = "Resolved terminal case — must not reopen on identical detection key recurrence.",
            EvidenceState = OperationalRiskEvidenceStates.Simulated,
            LikelihoodLevel = 4,
            ImpactLevel = 4,
            RiskScore = 16,
            SeverityBand = OperationalRiskSeverityBands.High,
            AssessmentOutcome = OperationalRiskAssessmentOutcomes.Scored,
            LifecycleStatus = OperationalRiskLifecycleStatuses.Resolved,
            RuleSetVersion = OperationalRiskConstants.RuleSetVersion,
            SourceModule = OperationalRiskSourceModules.Energy,
            SourceReference = DevelopmentPilotIds.Energy2PilotAssessmentPeakDemandRef,
            SourceProvenance = provenance,
            DetectionKey = detectionKey,
            OccurrenceSequence = 1,
            DetectionRuleCode = "energy-attention",
            FirstDetectedAtUtc = observedAt.AddDays(-7),
            LastObservedAtUtc = observedAt.AddDays(-1),
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

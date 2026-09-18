using System.Text;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.BulkDataOnboarding;
using NevaxisFactOS.Persistence.ContextKnowledge;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class BulkDataOnboardingPermissionTests
{
    [Fact]
    public void PermissionKeysFollowBulkOnboardingConvention()
    {
        Assert.Equal("bulk-onboarding.read", BulkDataOnboardingPermissionKeys.Read);
        Assert.Equal("bulk-onboarding.manage", BulkDataOnboardingPermissionKeys.Manage);
        Assert.Equal("bulk-onboarding.mapping.manage", BulkDataOnboardingPermissionKeys.MappingManage);
        Assert.Equal("bulk-onboarding.run", BulkDataOnboardingPermissionKeys.Run);
        Assert.Equal("bulk-onboarding.commit", BulkDataOnboardingPermissionKeys.Commit);
    }
}

public sealed class BulkDataSourceProviderTests
{
    [Fact]
    public void SyntheticProviderExposesExpectedKinds()
    {
        var provider = new SyntheticStructuredSourceProvider();
        Assert.Equal(BulkDataSourceProviderKinds.SyntheticStructured, provider.ProviderKind);
        Assert.Equal(BulkDataSourceKinds.Database, provider.SourceKind);
    }

    [Fact]
    public async Task ResolverReturnsSyntheticProvider()
    {
        var resolver = new BulkDataSourceProviderResolver([new SyntheticStructuredSourceProvider()]);
        var provider = resolver.Resolve(BulkDataSourceProviderKinds.SyntheticStructured);
        Assert.IsType<SyntheticStructuredSourceProvider>(provider);
    }
}

public sealed class BulkDiscoveryTests
{
    [Fact]
    public async Task SyntheticDiscoveryIncludesExpectedTables()
    {
        var discovery = await new SyntheticStructuredSourceProvider().DiscoverAsync();
        Assert.Equal(BulkDataOnboardingSyntheticFixture.SourceSystem, discovery.SourceSystem);
        Assert.True(discovery.Tables.Count >= 10);
        Assert.Contains(discovery.Tables, t => t.SourceTable == "resources");
        Assert.Contains(discovery.Tables, t => t.SourceTable == "events");
    }
}

public sealed class BulkSchemaProfilerTests
{
    [Fact]
    public void ProfilerInfersMachineResourceRole()
    {
        var role = BulkSchemaProfilerEngine.InferRoleHint(
            "machine_resources",
            [new BulkColumnDescriptor("machine_code", "text", false, true, null)]);
        Assert.Equal(BulkSchemaRoleHints.MachinesResources, role);
    }

    [Fact]
    public void ProfilerInfersProductionOrderRole()
    {
        var role = BulkSchemaProfilerEngine.InferRoleHint(
            "production_orders",
            [new BulkColumnDescriptor("order_code", "text", false, true, null)]);
        Assert.Equal(BulkSchemaRoleHints.ProductionOrders, role);
    }
}

public sealed class BulkMappingAndConversionTests
{
    [Fact]
    public void DirectMappingPreservesFieldValues()
    {
        var records = BulkDataOnboardingSyntheticFixture.BuildMappedRecords(
            BulkDataOnboardingSyntheticFixture.BuildDefaultMapping());
        var area = records.First(r => r.SourceTable == "areas");
        Assert.Equal("AREA-01", area.Fields["code"]);
    }

    [Fact]
    public void UnitConversionConvertsMillisecondsToSeconds()
    {
        Assert.True(BulkUnitNormalizationEngine.TryConvertDuration("1500", "ms", "sec", out var converted));
        Assert.Equal(1.5m, converted);
    }

    [Fact]
    public void EnumLikeMappingUsesCanonicalEntityTypePerTable()
    {
        var records = BulkDataOnboardingSyntheticFixture.BuildMappedRecords(
            BulkDataOnboardingSyntheticFixture.BuildDefaultMapping());
        Assert.Contains(records, r => r.CanonicalEntityType == BulkCanonicalEntityTypes.ManufacturingResource);
    }

    [Fact]
    public void InvalidMappingEntityTypeFailsQualityGate()
    {
        var record = new BulkMappedRecord(
            "unknown",
            "X-1",
            "unknown-entity",
            new Dictionary<string, string?>(),
            null,
            null,
            DateTimeOffset.UtcNow);
        var result = BulkDataQualityGateEngine.Evaluate(
            record,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Dictionary<string, string> { ["manufacturing-area"] = "code,name" },
            DateTimeOffset.UtcNow);
        Assert.False(result.Accepted);
    }
}

public sealed class BulkStagingAndQualityTests
{
    [Fact]
    public void DataQualityRejectsMissingRequiredKey()
    {
        var record = new BulkMappedRecord(
            "areas",
            "A-1",
            BulkCanonicalEntityTypes.ManufacturingArea,
            new Dictionary<string, string?> { ["code"] = "AREA-01" },
            null,
            null,
            DateTimeOffset.UtcNow);
        var result = BulkDataQualityGateEngine.Evaluate(
            record,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Dictionary<string, string> { [BulkCanonicalEntityTypes.ManufacturingArea] = "code,name" },
            DateTimeOffset.UtcNow);
        Assert.False(result.Accepted);
        Assert.Contains(result.Issues, i => i.ReasonCode == BulkDataQualityReasonCodes.MissingRequiredKey);
    }

    [Fact]
    public void DuplicateChecksumDetectionUsesStableHash()
    {
        var fields = new Dictionary<string, string?> { ["code"] = "RES-001", ["name"] = "Press" };
        var a = BulkIdempotencyEngine.BuildRecordChecksum(fields);
        var b = BulkIdempotencyEngine.BuildRecordChecksum(fields);
        Assert.Equal(a, b);
    }

    [Fact]
    public void ReimportIdempotencyKeyIsStable()
    {
        var key1 = BulkIdempotencyEngine.BuildImportIdempotencyKey("SYS", "resources", "RES-001", "ABC");
        var key2 = BulkIdempotencyEngine.BuildImportIdempotencyKey("SYS", "resources", "RES-001", "ABC");
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void InvalidTimestampIsRejected()
    {
        var record = new BulkMappedRecord(
            "events",
            "EVT-1",
            BulkCanonicalEntityTypes.ProductionEvent,
            new Dictionary<string, string?> { ["resource_code"] = "RES-001", ["event_type"] = "GOOD", ["occurred_at"] = "GOOD" },
            "not-a-date",
            null,
            DateTimeOffset.UtcNow);
        var result = BulkDataQualityGateEngine.Evaluate(
            record,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Dictionary<string, string> { [BulkCanonicalEntityTypes.ProductionEvent] = "resource_code,event_type,occurred_at" },
            DateTimeOffset.UtcNow);
        Assert.Contains(result.Issues, i => i.ReasonCode == BulkDataQualityReasonCodes.InvalidTimestamp);
    }

    [Fact]
    public void TenantMismatchIsRejected()
    {
        var tenantId = Guid.NewGuid();
        var record = new BulkMappedRecord(
            "areas",
            "A-1",
            BulkCanonicalEntityTypes.ManufacturingArea,
            new Dictionary<string, string?> { ["code"] = "A", ["name"] = "A", ["tenant_id"] = Guid.NewGuid().ToString() },
            null,
            null,
            DateTimeOffset.UtcNow);
        var result = BulkDataQualityGateEngine.Evaluate(
            record,
            tenantId,
            Guid.NewGuid(),
            new Dictionary<string, string> { [BulkCanonicalEntityTypes.ManufacturingArea] = "code,name" },
            DateTimeOffset.UtcNow);
        Assert.Contains(result.Issues, i => i.ReasonCode == BulkDataQualityReasonCodes.TenantMismatch);
    }
}

public sealed class BulkEntityResolutionTests
{
    [Fact]
    public void ExactResolutionMatchesKnownCode()
    {
        var known = new Dictionary<string, Guid> { ["MCH-001"] = Guid.NewGuid() };
        var result = BulkEntityResolutionEngine.ResolveExact("MCH-001", known);
        Assert.Equal(BulkEntityResolutionOutcomes.Exact, result.Outcome);
    }

    [Fact]
    public void AmbiguousResolutionDoesNotGuess()
    {
        var known = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            ["Press-01"] = Guid.NewGuid(),
        };
        var result = BulkEntityResolutionEngine.ResolveExact("Press 01", known);
        Assert.Equal(BulkEntityResolutionOutcomes.Unresolved, result.Outcome);
    }

    [Fact]
    public void UnresolvedEntityStaysUnresolved()
    {
        var result = BulkEntityResolutionEngine.ResolveExact("UNKNOWN", new Dictionary<string, Guid>());
        Assert.Equal(BulkEntityResolutionOutcomes.Unresolved, result.Outcome);
    }
}

public sealed class BulkTimeNormalizationTests
{
    [Fact]
    public void UtcTimestampNormalizesCleanly()
    {
        var (normalized, ambiguous) = BulkTimeNormalizationEngine.Normalize("2026-01-01T10:00:00Z");
        Assert.NotNull(normalized);
        Assert.False(ambiguous);
    }

    [Fact]
    public void AmbiguousLocalTimestampWithoutZoneIsMarkedAmbiguous()
    {
        var (normalized, ambiguous) = BulkTimeNormalizationEngine.Normalize("01/15/2024 08:30:00");
        Assert.Null(normalized);
        Assert.True(ambiguous);
    }
}

public sealed class BulkDryRunAndImpactTests
{
    [Fact]
    public void DryRunImpactPreviewDoesNotRequireMutationFlag()
    {
        var staging = new List<BulkImportStagingRecord>
        {
            new()
            {
                CanonicalEntityType = BulkCanonicalEntityTypes.ManufacturingArea,
                SourceKey = "AREA-01",
                ValidationState = BulkStagingValidationStates.Valid,
            },
        };
        var impact = BulkImpactPreviewEngine.Build(staging, new HashSet<string>());
        Assert.Equal(1, impact.AreasToCreate);
    }

    [Fact]
    public void ImpactPreviewCountsQuarantineRows()
    {
        var staging = new List<BulkImportStagingRecord>
        {
            new() { ValidationState = BulkStagingValidationStates.Quarantined, SourceKey = "Q-1", CanonicalEntityType = "x" },
        };
        var impact = BulkImpactPreviewEngine.Build(staging, new HashSet<string>());
        Assert.Equal(1, impact.QuarantinedRows);
    }
}

public sealed class BulkHistoricalTierAndKnowledgeTests
{
    [Fact]
    public void HistoricalTierAssignsArchiveForOldTelemetry()
    {
        var tier = BulkHistoricalTierEngine.Classify(DateTimeOffset.UtcNow.AddYears(-3), DateTimeOffset.UtcNow);
        Assert.Equal(BulkHistoricalDataTiers.Archive, tier);
    }

    [Fact]
    public void ImportedHistoricalDataDoesNotAutoPromoteKnowledge()
    {
        Assert.True(BulkKnowledgeSafetyGuard.BlocksAutoPromotion(false));
    }

    [Fact]
    public void ContextKnowledgePromotionGateStillAuthoritative()
    {
        var result = KnowledgePromotionGate.Evaluate(new KnowledgePromotionRequest(
            Guid.NewGuid(),
            "imported",
            string.Empty,
            string.Empty,
            [],
            [],
            "{}",
            "{}",
            string.Empty,
            HasVerifiedOutcome: false,
            HasSufficientEvidence: false,
            HasUnresolvedConflict: false,
            ObservationCompleted: false,
            Confidence: 0m,
            ApplicabilityConstraintsJson: "{}"));
        Assert.Equal(ContextKnowledgePromotionStatuses.LearningOnly, result.PromotionStatus);
    }
}

public sealed class BulkReconciliationAndLineageTests
{
    [Fact]
    public void ReconciliationCountsBalanceWhenStagesMatch()
    {
        var job = new BulkDataImportJob
        {
            TotalSourceRows = 10,
            StagedRows = 10,
            ValidRows = 8,
            InvalidRows = 0,
            QuarantinedRows = 1,
            DuplicateRows = 1,
        };
        var summary = BulkReconciliationEngine.Build(job);
        Assert.True(summary.Balanced);
    }

    [Fact]
    public void LineageRecordFieldsAreDefinedOnEntity()
    {
        var lineage = new BulkImportLineageRecord
        {
            SourceSystem = "SYS",
            SourceDatabase = "db",
            SourceTable = "resources",
            SourceKey = "RES-001",
        };
        Assert.Equal("RES-001", lineage.SourceKey);
    }
}

public sealed class BulkRollbackAndCheckpointTests
{
    [Fact]
    public void CheckpointModelSupportsResumeMetadata()
    {
        var checkpoint = new BulkImportCheckpoint("events", 3, 750, DateTimeOffset.UtcNow);
        Assert.Equal(750, checkpoint.ProcessedRows);
    }

    [Fact]
    public void EntityMutationTracksRollbackEligibility()
    {
        var mutation = new BulkImportEntityMutation { RollbackEligible = true, RolledBack = false };
        Assert.True(mutation.RollbackEligible);
        Assert.False(mutation.RolledBack);
    }
}

public sealed class BulkDeltaFoundationTests
{
    [Fact]
    public void ImportJobSupportsWatermarkJson()
    {
        var job = new BulkDataImportJob { WatermarkJson = """{"updated_at":"2026-01-01T00:00:00Z"}""" };
        Assert.Contains("updated_at", job.WatermarkJson);
    }

    [Fact]
    public void ImportVersionKindsIncludeDelta()
    {
        Assert.Equal("delta", BulkImportVersionKinds.Delta);
    }
}

public sealed class BulkFileSafetyTests
{
    [Fact]
    public void PathTraversalFilenameIsRejected()
    {
        Assert.NotNull(BulkFileSafetyValidator.ValidateFile("../secrets.csv", 100));
    }

    [Fact]
    public void UnsupportedExtensionIsRejected()
    {
        Assert.NotNull(BulkFileSafetyValidator.ValidateFile("dump.sql", 100));
    }

    [Fact]
    public void OversizedFileIsRejected()
    {
        Assert.NotNull(BulkFileSafetyValidator.ValidateFile("data.csv", 200_000_000));
    }
}

public sealed class BulkSyntheticDatasetTests
{
    [Fact]
    public void SyntheticFoundationDatasetHasExpectedScale()
    {
        var discovery = BulkDataOnboardingSyntheticFixture.BuildDiscovery();
        var records = BulkDataOnboardingSyntheticFixture.BuildMappedRecords(
            BulkDataOnboardingSyntheticFixture.BuildDefaultMapping());

        Assert.Equal(10, discovery.Tables.Count);
        Assert.Equal(3214, records.Count);
    }

    [Fact]
    public void SyntheticValidationPipelineProcessesFullDatasetWithinReasonableTime()
    {
        var records = BulkDataOnboardingSyntheticFixture.BuildMappedRecords(
            BulkDataOnboardingSyntheticFixture.BuildDefaultMapping());
        var requiredFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [BulkCanonicalEntityTypes.ManufacturingArea] = "code,name",
            [BulkCanonicalEntityTypes.ProductionLine] = "code,name,area_code",
            [BulkCanonicalEntityTypes.ProductionStation] = "code,name,line_code",
            [BulkCanonicalEntityTypes.ManufacturingResource] = "code,name,station_code,resource_type",
            [BulkCanonicalEntityTypes.Material] = "code,name,unit_of_measure",
            [BulkCanonicalEntityTypes.ProductionOrder] = "order_code,product_code,line_code,quantity",
            [BulkCanonicalEntityTypes.ProductionEvent] = "resource_code,event_type,good_qty,occurred_at",
            [BulkCanonicalEntityTypes.QualityRecord] = "order_code,defect_code,recorded_at",
            [BulkCanonicalEntityTypes.MaintenanceRecord] = "resource_code,work_type,scheduled_at",
            [BulkCanonicalEntityTypes.MoldTool] = "mold_code,mold_name,cavity_count",
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var quarantined = 0;
        foreach (var record in records)
        {
            var result = BulkDataQualityGateEngine.Evaluate(
                record,
                Guid.NewGuid(),
                Guid.NewGuid(),
                requiredFields,
                DateTimeOffset.UtcNow);
            if (!result.Accepted)
            {
                quarantined++;
            }
        }
        stopwatch.Stop();

        Assert.True(quarantined >= 2);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000);
        Assert.True(records.Count / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001) > 500);
    }

    [Fact]
    public void SyntheticDatasetIncludesBadRowsForQuarantine()
    {
        var records = BulkDataOnboardingSyntheticFixture.BuildMappedRecords(
            BulkDataOnboardingSyntheticFixture.BuildDefaultMapping());
        Assert.Contains(records, r => r.SourceKey == "ORD-BAD-001");
        Assert.Contains(records, r => r.SourceKey == "EVT-FUTURE");
        Assert.True(records.Count > 3000);
    }

    [Fact]
    public void SyntheticDatasetIncludesHierarchyAndTelemetry()
    {
        var records = BulkDataOnboardingSyntheticFixture.BuildMappedRecords(
            BulkDataOnboardingSyntheticFixture.BuildDefaultMapping());
        Assert.Contains(records, r => r.SourceTable == "areas");
        Assert.Contains(records, r => r.SourceTable == "events");
        Assert.Contains(records, r => r.SourceTable == "quality");
    }
}

public sealed class BulkMigrationFoundationTests
{
    [Fact]
    public void MigrationEnablesRlsOnBulkImportJobs()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "NevaxisFactOS.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var migration = File.ReadAllText(Path.Combine(
            directory!.FullName,
            "packages",
            "NevaxisFactOS.Persistence.Migrations",
            "Migrations",
            "20260918120000_AddBulkDataOnboardingFoundation.cs"));
        Assert.Contains("bulk_data_import_jobs", migration);
        Assert.Contains("ENABLE ROW LEVEL SECURITY", migration);
        Assert.Contains("FORCE ROW LEVEL SECURITY", migration);
        Assert.Contains("bulk-onboarding.read", migration);
        Assert.Contains("bulk-onboarding.commit", migration);
    }
}

public sealed class BulkOnboardingSafetyModeTests
{
    [Fact]
    public void DefaultSafetyModeIsObserve()
    {
        var job = new BulkDataImportJob();
        Assert.Equal(BulkOnboardingSafetyModes.Observe, job.SafetyMode);
    }
}

public sealed class BulkJobLifecycleTests
{
    [Fact]
    public void JobStatusesIncludeRequiredLifecycleStates()
    {
        Assert.Equal("completed", BulkDataImportJobStatuses.Completed);
        Assert.Equal("dry-running", BulkDataImportJobStatuses.DryRunning);
        Assert.Equal("ready-to-commit", BulkDataImportJobStatuses.ReadyToCommit);
        Assert.Equal("quarantined", BulkDataImportJobStatuses.Quarantined);
    }
}

public sealed class BulkInjectionCompatibilityTests
{
    [Fact]
    public void ProductionEventCanonicalTypeIsSupported()
    {
        var records = BulkDataOnboardingSyntheticFixture.BuildMappedRecords(
            BulkDataOnboardingSyntheticFixture.BuildDefaultMapping());
        Assert.Contains(records, r => r.CanonicalEntityType == BulkCanonicalEntityTypes.ProductionEvent);
    }
}

public sealed class BulkIntegrationSpineReuseTests
{
    [Fact]
    public void IdempotencyKeyFormatAlignsWithIntegrationSpinePattern()
    {
        var key = BulkIdempotencyEngine.BuildImportIdempotencyKey("SYS", "events", "EVT-1", "HASH");
        Assert.StartsWith("bulk:", key);
    }
}

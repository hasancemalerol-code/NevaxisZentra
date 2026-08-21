using CrevaFactOS.Persistence.IotPlc;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotIotPlcSeed
{
    internal static async Task EnsureIotPlcPilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = IotPlcConstants.DevelopmentPilotProvenance;

        await EnsureEndpointAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId,
            tenantId,
            facilityId,
            "EP-OPC-INJ-LINE",
            "OPC UA Read-Only Injection Line Endpoint",
            IotPlcProtocolTypes.OpcUa,
            IotPlcConnectionDirections.ReadOnlyPull,
            IotPlcEndpointStatuses.Active,
            provenance,
            null,
            """{"host":"127.0.0.1","port":4840,"securityMode":"none","note":"Development metadata only — no live OPC UA client"}""",
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureEndpointAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotEndpointEdgeId,
            tenantId,
            facilityId,
            "EP-EDGE-RELAY-001",
            "Edge Agent Relay — Injection Floor",
            IotPlcProtocolTypes.EdgeAgent,
            IotPlcConnectionDirections.EdgeRelay,
            IotPlcEndpointStatuses.Active,
            provenance,
            null,
            """{"relayMode":"read-only","gatewayCode":"SIM-GW-001","note":"Development metadata only"}""",
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureEndpointAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotEndpointMqttId,
            tenantId,
            facilityId,
            "EP-MQTT-PUSH-001",
            "MQTT Push Endpoint (Suspended Pilot)",
            IotPlcProtocolTypes.Mqtt,
            IotPlcConnectionDirections.InboundPush,
            IotPlcEndpointStatuses.Suspended,
            provenance,
            IotPlcConstants.NonProductionSecretPlaceholder,
            """{"topicPrefix":"dev/sim/injection","qos":0,"note":"Development metadata only — no real broker"}""",
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureSignalAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotSignalInj1StateId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId,
            "SIG-INJ-001-STATE",
            "INJ-001 Machine Running State",
            "ns=2;s=INJ001.MachineRunning",
            IotPlcDataTypes.Boolean,
            null,
            IotPlcSemanticTypes.MachineState,
            IotPlcAssetTypes.Machine,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            true,
            "1s",
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureSignalAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotSignalInj1CycleId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId,
            "SIG-INJ-001-CYCLES",
            "INJ-001 Cycle Counter",
            "ns=2;s=INJ001.CycleCount",
            IotPlcDataTypes.Integer,
            "cycles",
            IotPlcSemanticTypes.CycleCounter,
            IotPlcAssetTypes.Machine,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            true,
            "5s",
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureSignalAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotSignalInj2GoodId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointEdgeId,
            "SIG-INJ-002-GOOD",
            "INJ-002 Good Part Counter",
            "edge://INJ-002/counters/good",
            IotPlcDataTypes.Integer,
            "parts",
            IotPlcSemanticTypes.GoodPartCounter,
            IotPlcAssetTypes.Machine,
            DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
            true,
            "10s",
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureSignalAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotSignalInj3TempId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointEdgeId,
            "SIG-INJ-003-TEMP",
            "INJ-003 Barrel Temperature",
            "edge://INJ-003/telemetry/barrelTempC",
            IotPlcDataTypes.Decimal,
            "°C",
            IotPlcSemanticTypes.Temperature,
            IotPlcAssetTypes.Machine,
            DevelopmentPilotIds.EdgePilotInjectionMachine3Id,
            true,
            "2s",
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureSignalAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotSignalChillerPowerId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId,
            "SIG-CHILLER-POWER",
            "Chiller Active Power Reading",
            "ns=2;s=CHILLER001.ActivePowerKw",
            IotPlcDataTypes.Decimal,
            "kW",
            IotPlcSemanticTypes.PowerReading,
            IotPlcAssetTypes.AuxiliaryEquipment,
            DevelopmentPilotIds.EnergyPilotPointChillerId,
            true,
            "15s",
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureObservationsAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, provenance, cancellationToken);

        await EnsureHealthObservationsAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsureEndpointAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string endpointCode,
        string displayName,
        string protocolType,
        string connectionDirection,
        string endpointStatus,
        string configurationProvenance,
        string? externalSecretReference,
        string connectionProfileJson,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.IotPlcEndpoints
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (existing is null)
        {
            dbContext.IotPlcEndpoints.Add(new IotPlcEndpoint
            {
                Id = id,
                TenantId = tenantId,
                FacilityId = facilityId,
                EndpointCode = endpointCode,
                DisplayName = displayName,
                ProtocolType = protocolType,
                ConnectionDirection = connectionDirection,
                EndpointStatus = endpointStatus,
                ConfigurationProvenance = configurationProvenance,
                ExternalSecretReference = externalSecretReference,
                ConnectionProfileJson = connectionProfileJson,
                LastObservedConnectionState = null,
                LastObservedAtUtc = null,
                Version = 1,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
                UpdatedBy = actorSubjectId,
            });
        }
        else
        {
            existing.EndpointCode = endpointCode;
            existing.DisplayName = displayName;
            existing.ProtocolType = protocolType;
            existing.ConnectionDirection = connectionDirection;
            existing.EndpointStatus = endpointStatus;
            existing.ConfigurationProvenance = configurationProvenance;
            existing.ExternalSecretReference = externalSecretReference;
            existing.ConnectionProfileJson = connectionProfileJson;
            existing.UpdatedAtUtc = seedTimestampUtc;
            existing.UpdatedBy = actorSubjectId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSignalAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid endpointId,
        string signalCode,
        string displayName,
        string sourceAddress,
        string dataType,
        string? engineeringUnit,
        string semanticType,
        string mappedAssetType,
        Guid mappedCanonicalAssetId,
        bool isEnabled,
        string samplingExpectation,
        string sourceProvenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.IotPlcSignals
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (existing is null)
        {
            dbContext.IotPlcSignals.Add(new IotPlcSignal
            {
                Id = id,
                TenantId = tenantId,
                FacilityId = facilityId,
                EndpointId = endpointId,
                SignalCode = signalCode,
                DisplayName = displayName,
                SourceAddress = sourceAddress,
                DataType = dataType,
                EngineeringUnit = engineeringUnit,
                SemanticType = semanticType,
                MappedAssetType = mappedAssetType,
                MappedCanonicalAssetId = mappedCanonicalAssetId,
                IsEnabled = isEnabled,
                SamplingExpectation = samplingExpectation,
                SourceProvenance = sourceProvenance,
                Version = 1,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
                UpdatedBy = actorSubjectId,
            });
        }
        else
        {
            existing.SignalCode = signalCode;
            existing.DisplayName = displayName;
            existing.SourceAddress = sourceAddress;
            existing.DataType = dataType;
            existing.EngineeringUnit = engineeringUnit;
            existing.SemanticType = semanticType;
            existing.MappedAssetType = mappedAssetType;
            existing.MappedCanonicalAssetId = mappedCanonicalAssetId;
            existing.IsEnabled = isEnabled;
            existing.SamplingExpectation = samplingExpectation;
            existing.SourceProvenance = sourceProvenance;
            existing.UpdatedAtUtc = seedTimestampUtc;
            existing.UpdatedBy = actorSubjectId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureObservationsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var baseTime = new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotObsInj1StateId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId,
            DevelopmentPilotIds.IotPlcPilotSignalInj1StateId,
            IotPlcAssetTypes.Machine,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            baseTime,
            seedTimestampUtc,
            true,
            null,
            null,
            null,
            null,
            IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated,
            "PILOT-SEQ-INJ1-STATE-001",
            provenance,
            actorSubjectId,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotObsInj1CycleId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId,
            DevelopmentPilotIds.IotPlcPilotSignalInj1CycleId,
            IotPlcAssetTypes.Machine,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            baseTime.AddMinutes(1),
            seedTimestampUtc,
            null,
            12450L,
            null,
            null,
            null,
            IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated,
            "PILOT-SEQ-INJ1-CYCLE-001",
            provenance,
            actorSubjectId,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotObsInj2GoodId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointEdgeId,
            DevelopmentPilotIds.IotPlcPilotSignalInj2GoodId,
            IotPlcAssetTypes.Machine,
            DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
            baseTime.AddMinutes(2),
            seedTimestampUtc,
            null,
            8920L,
            null,
            null,
            null,
            IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated,
            "PILOT-SEQ-INJ2-GOOD-001",
            provenance,
            actorSubjectId,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotObsInj3TempId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointEdgeId,
            DevelopmentPilotIds.IotPlcPilotSignalInj3TempId,
            IotPlcAssetTypes.Machine,
            DevelopmentPilotIds.EdgePilotInjectionMachine3Id,
            baseTime.AddMinutes(3),
            seedTimestampUtc,
            null,
            null,
            218.5m,
            null,
            null,
            IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated,
            "PILOT-SEQ-INJ3-TEMP-001",
            provenance,
            actorSubjectId,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotObsStaleConflictId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId,
            DevelopmentPilotIds.IotPlcPilotSignalChillerPowerId,
            IotPlcAssetTypes.AuxiliaryEquipment,
            DevelopmentPilotIds.EnergyPilotPointChillerId,
            baseTime.AddHours(-4),
            seedTimestampUtc,
            null,
            null,
            42.3m,
            null,
            null,
            IotPlcQualityStates.Stale,
            IotPlcEvidenceStates.SourceConflict,
            "PILOT-SEQ-CHILLER-STALE-001",
            provenance,
            actorSubjectId,
            cancellationToken);
    }

    private static async Task EnsureObservationAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid endpointId,
        Guid signalId,
        string mappedAssetType,
        Guid mappedCanonicalAssetId,
        DateTimeOffset observedAtUtc,
        DateTimeOffset receivedAtUtc,
        bool? booleanValue,
        long? integerValue,
        decimal? decimalValue,
        string? textValue,
        DateTimeOffset? timestampValue,
        string qualityState,
        string evidenceState,
        string sourceSequenceReference,
        string sourceProvenance,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.IotPlcSignalObservations
            .IgnoreQueryFilters()
            .AnyAsync(o => o.Id == id, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.IotPlcSignalObservations.Add(new IotPlcSignalObservation
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            EndpointId = endpointId,
            SignalDefinitionId = signalId,
            MappedAssetType = mappedAssetType,
            MappedCanonicalAssetId = mappedCanonicalAssetId,
            ObservedAtUtc = observedAtUtc,
            ReceivedAtUtc = receivedAtUtc,
            BooleanValue = booleanValue,
            IntegerValue = integerValue,
            DecimalValue = decimalValue,
            TextValue = textValue,
            TimestampValue = timestampValue,
            QualityState = qualityState,
            EvidenceState = evidenceState,
            SourceSequenceReference = sourceSequenceReference,
            SourceProvenance = sourceProvenance,
            CreatedAtUtc = receivedAtUtc,
            CreatedBy = actorSubjectId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureHealthObservationsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var baseTime = new DateTimeOffset(2026, 8, 20, 10, 30, 0, TimeSpan.Zero);

        await EnsureHealthAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotHealthOnlineId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId,
            baseTime,
            seedTimestampUtc,
            IotPlcConnectionStates.Online,
            "development-simulator",
            IotPlcEvidenceStates.Simulated,
            "simulated-online",
            45,
            baseTime.AddMinutes(-1),
            actorSubjectId,
            cancellationToken);

        await EnsureHealthAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotHealthDegradedId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointEdgeId,
            baseTime.AddMinutes(5),
            seedTimestampUtc,
            IotPlcConnectionStates.Degraded,
            "development-simulator",
            IotPlcEvidenceStates.Simulated,
            "simulated-degraded-latency",
            850,
            baseTime.AddMinutes(4),
            actorSubjectId,
            cancellationToken);

        await EnsureHealthAsync(
            dbContext,
            DevelopmentPilotIds.IotPlcPilotHealthOfflineId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointMqttId,
            baseTime.AddMinutes(10),
            seedTimestampUtc,
            IotPlcConnectionStates.Offline,
            "development-simulator",
            IotPlcEvidenceStates.Simulated,
            "simulated-offline-suspended",
            null,
            null,
            actorSubjectId,
            cancellationToken);
    }

    private static async Task EnsureHealthAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid endpointId,
        DateTimeOffset observedAtUtc,
        DateTimeOffset receivedAtUtc,
        string healthState,
        string source,
        string evidenceState,
        string reasonCode,
        int? latencyMs,
        DateTimeOffset? lastSuccessfulObservationAtUtc,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.IotPlcConnectionHealthObservations
            .IgnoreQueryFilters()
            .AnyAsync(h => h.Id == id, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.IotPlcConnectionHealthObservations.Add(new IotPlcConnectionHealthObservation
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            EndpointId = endpointId,
            ObservedAtUtc = observedAtUtc,
            ReceivedAtUtc = receivedAtUtc,
            HealthState = healthState,
            Source = source,
            EvidenceState = evidenceState,
            ReasonCode = reasonCode,
            LatencyMs = latencyMs,
            LastSuccessfulObservationAtUtc = lastSuccessfulObservationAtUtc,
            CreatedAtUtc = receivedAtUtc,
            CreatedBy = actorSubjectId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

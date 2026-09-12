using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportShipmentWriteRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportShipmentWriteRepository
{
    // Allowed status transitions
    private static readonly Dictionary<ExportShipmentStatus, IReadOnlySet<ExportShipmentStatus>> AllowedTransitions =
        new()
        {
            [ExportShipmentStatus.Draft] = new HashSet<ExportShipmentStatus> { ExportShipmentStatus.Preparing, ExportShipmentStatus.Cancelled },
            [ExportShipmentStatus.Preparing] = new HashSet<ExportShipmentStatus> { ExportShipmentStatus.Ready, ExportShipmentStatus.Cancelled },
            [ExportShipmentStatus.Ready] = new HashSet<ExportShipmentStatus> { ExportShipmentStatus.HandedToBroker, ExportShipmentStatus.Preparing, ExportShipmentStatus.Cancelled },
            [ExportShipmentStatus.HandedToBroker] = new HashSet<ExportShipmentStatus>
            {
                ExportShipmentStatus.ClearedForDispatch,
                ExportShipmentStatus.Preparing,
                ExportShipmentStatus.Cancelled,
            },
            [ExportShipmentStatus.ClearedForDispatch] = new HashSet<ExportShipmentStatus> { ExportShipmentStatus.Dispatched },
            [ExportShipmentStatus.Dispatched] = new HashSet<ExportShipmentStatus> { ExportShipmentStatus.Closed },
            [ExportShipmentStatus.Closed] = new HashSet<ExportShipmentStatus>(),
            [ExportShipmentStatus.Cancelled] = new HashSet<ExportShipmentStatus>(),
        };

    public async Task<ExportShipmentWriteResult> CreateAsync(
        ExportOperationContext context,
        CreateExportShipmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var reference = (command.ShipmentReference ?? string.Empty).Trim();
        if (reference.Length == 0)
        {
            return ExportShipmentWriteResult.Fail(ExportFailureCodes.ValidationFailed, "Shipment reference is required.");
        }

        var destination = (command.DestinationCountryCode ?? string.Empty).Trim().ToUpperInvariant();
        if (destination.Length != 2)
        {
            return ExportShipmentWriteResult.Fail(ExportFailureCodes.InvalidDestinationCountry, "Destination country code must be a 2-letter code.");
        }

        if (command.TransportMode is not null && !ExportTransportMode.IsKnown(command.TransportMode))
        {
            return ExportShipmentWriteResult.Fail(Export2LFailureCodes.InvalidTransportMode, $"Transport mode '{command.TransportMode}' is not recognized.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportShipmentWriteResult.Fail(ExportFailureCodes.NotFound, "Export case was not found.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var shipment = new ExportShipment
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ExportCaseId = command.ExportCaseId,
                        ShipmentReference = reference,
                        Status = ExportShipmentStatus.Draft,
                        TransportMode = command.TransportMode,
                        IncotermCode = string.IsNullOrWhiteSpace(command.IncotermCode) ? null : command.IncotermCode.Trim().ToUpperInvariant(),
                        DestinationCountryCode = destination,
                        DestinationCity = string.IsNullOrWhiteSpace(command.DestinationCity) ? null : command.DestinationCity.Trim(),
                        DeliveryAddress = string.IsNullOrWhiteSpace(command.DeliveryAddress) ? null : command.DeliveryAddress.Trim(),
                        ForwarderName = string.IsNullOrWhiteSpace(command.ForwarderName) ? null : command.ForwarderName.Trim(),
                        CarrierName = string.IsNullOrWhiteSpace(command.CarrierName) ? null : command.CarrierName.Trim(),
                        PlannedPickupAtUtc = command.PlannedPickupAtUtc,
                        PlannedDepartureAtUtc = command.PlannedDepartureAtUtc,
                        PlannedArrivalAtUtc = command.PlannedArrivalAtUtc,
                        BookingReference = string.IsNullOrWhiteSpace(command.BookingReference) ? null : command.BookingReference.Trim(),
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                        UpdatedAtUtc = now,
                        LastModifiedBySubjectId = actorSubjectId,
                        Version = 1,
                    };

                    dbContext.ExportShipments.Add(shipment);

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.ShipmentCreated,
                        "Shipment created",
                        $"{reference} → {destination}",
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.shipment",
                        shipment.Id);

                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportShipmentCreated,
                        ExportAuditActions.ExportShipmentCreated,
                        "export.shipment",
                        shipment.Id.ToString(),
                        correlationId,
                        now,
                        new { exportCaseId = command.ExportCaseId.ToString(), shipmentId = shipment.Id.ToString(), reference });

                    helper.AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportShipmentCreated,
                        "export-shipment",
                        shipment.Id.ToString(),
                        correlationId,
                        $"{shipment.Id}:1:{ExportOutboxMessageTypes.ExportShipmentCreated}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["exportCaseId"] = command.ExportCaseId.ToString(),
                            ["shipmentId"] = shipment.Id.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportShipmentWriteResult.Success(
                        shipment.Id, shipment.Version, ExportValueConversions.ToStorage(shipment.Status));
                },
                cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
        {
            return ExportShipmentWriteResult.Fail(
                Export2LFailureCodes.DuplicateShipmentReference,
                $"A shipment with reference '{reference}' already exists for this export case.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportShipmentWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportShipmentWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export shipment service is unavailable.");
        }
    }

    public async Task<ExportShipmentWriteResult> UpdateAsync(
        ExportOperationContext context,
        UpdateExportShipmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (command.TransportMode is not null && !ExportTransportMode.IsKnown(command.TransportMode))
        {
            return ExportShipmentWriteResult.Fail(Export2LFailureCodes.InvalidTransportMode, $"Transport mode '{command.TransportMode}' is not recognized.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var shipment = await dbContext.ExportShipments
                        .SingleOrDefaultAsync(
                            s => s.TenantId == context.TenantId
                                && s.FacilityId == context.FacilityId
                                && s.ExportCaseId == command.ExportCaseId
                                && s.Id == command.ShipmentId,
                            ct);

                    if (shipment is null)
                    {
                        return ExportShipmentWriteResult.Fail(Export2LFailureCodes.ShipmentNotFound, "Shipment was not found.");
                    }

                    if (shipment.Version != command.ExpectedVersion)
                    {
                        return ExportShipmentWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                            "The shipment has been modified. Please reload and try again.");
                    }

                    if (shipment.Status is ExportShipmentStatus.Closed or ExportShipmentStatus.Cancelled)
                    {
                        return ExportShipmentWriteResult.Fail(Export2LFailureCodes.InvalidShipmentStatus,
                            "Cannot update a closed or cancelled shipment.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();
                    var changed = false;

                    if (command.TransportMode is not null && command.TransportMode != shipment.TransportMode) { shipment.TransportMode = command.TransportMode; changed = true; }
                    if (command.IncotermCode is not null) { var v = string.IsNullOrWhiteSpace(command.IncotermCode) ? null : command.IncotermCode.Trim().ToUpperInvariant(); if (v != shipment.IncotermCode) { shipment.IncotermCode = v; changed = true; } }
                    if (command.DestinationCountryCode is not null) { var v = command.DestinationCountryCode.Trim().ToUpperInvariant(); if (v != shipment.DestinationCountryCode) { shipment.DestinationCountryCode = v; changed = true; } }
                    if (command.DestinationCity is not null) { var v = string.IsNullOrWhiteSpace(command.DestinationCity) ? null : command.DestinationCity.Trim(); if (v != shipment.DestinationCity) { shipment.DestinationCity = v; changed = true; } }
                    if (command.DeliveryAddress is not null) { var v = string.IsNullOrWhiteSpace(command.DeliveryAddress) ? null : command.DeliveryAddress.Trim(); if (v != shipment.DeliveryAddress) { shipment.DeliveryAddress = v; changed = true; } }
                    if (command.ForwarderName is not null) { var v = string.IsNullOrWhiteSpace(command.ForwarderName) ? null : command.ForwarderName.Trim(); if (v != shipment.ForwarderName) { shipment.ForwarderName = v; changed = true; } }
                    if (command.CarrierName is not null) { var v = string.IsNullOrWhiteSpace(command.CarrierName) ? null : command.CarrierName.Trim(); if (v != shipment.CarrierName) { shipment.CarrierName = v; changed = true; } }
                    if (command.PlannedPickupAtUtc.HasValue && command.PlannedPickupAtUtc != shipment.PlannedPickupAtUtc) { shipment.PlannedPickupAtUtc = command.PlannedPickupAtUtc; changed = true; }
                    if (command.PlannedDepartureAtUtc.HasValue && command.PlannedDepartureAtUtc != shipment.PlannedDepartureAtUtc) { shipment.PlannedDepartureAtUtc = command.PlannedDepartureAtUtc; changed = true; }
                    if (command.PlannedArrivalAtUtc.HasValue && command.PlannedArrivalAtUtc != shipment.PlannedArrivalAtUtc) { shipment.PlannedArrivalAtUtc = command.PlannedArrivalAtUtc; changed = true; }
                    if (command.ActualPickupAtUtc.HasValue && command.ActualPickupAtUtc != shipment.ActualPickupAtUtc) { shipment.ActualPickupAtUtc = command.ActualPickupAtUtc; changed = true; }
                    if (command.ActualDepartureAtUtc.HasValue && command.ActualDepartureAtUtc != shipment.ActualDepartureAtUtc) { shipment.ActualDepartureAtUtc = command.ActualDepartureAtUtc; changed = true; }
                    if (command.ActualArrivalAtUtc.HasValue && command.ActualArrivalAtUtc != shipment.ActualArrivalAtUtc) { shipment.ActualArrivalAtUtc = command.ActualArrivalAtUtc; changed = true; }
                    if (command.BookingReference is not null) { var v = string.IsNullOrWhiteSpace(command.BookingReference) ? null : command.BookingReference.Trim(); if (v != shipment.BookingReference) { shipment.BookingReference = v; changed = true; } }
                    if (command.TrackingReference is not null) { var v = string.IsNullOrWhiteSpace(command.TrackingReference) ? null : command.TrackingReference.Trim(); if (v != shipment.TrackingReference) { shipment.TrackingReference = v; changed = true; } }
                    if (command.TotalGrossWeight.HasValue && command.TotalGrossWeight != shipment.TotalGrossWeight) { shipment.TotalGrossWeight = command.TotalGrossWeight; changed = true; }
                    if (command.TotalNetWeight.HasValue && command.TotalNetWeight != shipment.TotalNetWeight) { shipment.TotalNetWeight = command.TotalNetWeight; changed = true; }
                    if (command.TotalPackageCount.HasValue && command.TotalPackageCount != shipment.TotalPackageCount) { shipment.TotalPackageCount = command.TotalPackageCount; changed = true; }

                    if (!changed)
                    {
                        return ExportShipmentWriteResult.Success(shipment.Id, shipment.Version, ExportValueConversions.ToStorage(shipment.Status));
                    }

                    shipment.Version += 1;
                    shipment.UpdatedAtUtc = now;
                    shipment.LastModifiedBySubjectId = actorSubjectId;

                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is not null)
                    {
                        exportCase.Version += 1;
                        exportCase.UpdatedAtUtc = now;
                        exportCase.LastModifiedBySubjectId = actorSubjectId;

                        var helper = new ExportReadinessPersistence(dbContext);
                        helper.AppendActivity(
                            exportCase,
                            ExportActivityEventTypes.ShipmentUpdated,
                            "Shipment updated",
                            shipment.ShipmentReference,
                            now,
                            ExportActivitySourceType.User,
                            actorSubjectId,
                            correlationId,
                            "export.shipment",
                            shipment.Id);

                        helper.AddAudit(
                            context,
                            ExportAuditEventTypes.ExportShipmentUpdated,
                            ExportAuditActions.ExportShipmentUpdated,
                            "export.shipment",
                            shipment.Id.ToString(),
                            correlationId,
                            now,
                            new { exportCaseId = command.ExportCaseId.ToString(), shipmentId = shipment.Id.ToString() });
                    }

                    await dbContext.SaveChangesAsync(ct);
                    return ExportShipmentWriteResult.Success(shipment.Id, shipment.Version, ExportValueConversions.ToStorage(shipment.Status));
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportShipmentWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The shipment has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportShipmentWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export shipment service is unavailable.");
        }
    }

    public async Task<ExportShipmentWriteResult> TransitionAsync(
        ExportOperationContext context,
        TransitionExportShipmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var shipment = await dbContext.ExportShipments
                        .SingleOrDefaultAsync(
                            s => s.TenantId == context.TenantId
                                && s.FacilityId == context.FacilityId
                                && s.ExportCaseId == command.ExportCaseId
                                && s.Id == command.ShipmentId,
                            ct);

                    if (shipment is null)
                    {
                        return ExportShipmentWriteResult.Fail(Export2LFailureCodes.ShipmentNotFound, "Shipment was not found.");
                    }

                    if (shipment.Version != command.ExpectedVersion)
                    {
                        return ExportShipmentWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                            "The shipment has been modified. Please reload and try again.");
                    }

                    if (!AllowedTransitions.TryGetValue(shipment.Status, out var allowed)
                        || !allowed.Contains(command.TargetStatus))
                    {
                        return ExportShipmentWriteResult.Fail(
                            Export2LFailureCodes.InvalidTransition,
                            $"Cannot transition shipment from {ExportValueConversions.ToStorage(shipment.Status)} to {ExportValueConversions.ToStorage(command.TargetStatus)}.");
                    }

                    if (command.TargetStatus == ExportShipmentStatus.Cancelled
                        && string.IsNullOrWhiteSpace(command.CancellationReason))
                    {
                        return ExportShipmentWriteResult.Fail(ExportFailureCodes.ValidationFailed, "Cancellation reason is required.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();
                    var previousStatus = shipment.Status;

                    shipment.Status = command.TargetStatus;
                    if (command.TargetStatus == ExportShipmentStatus.Cancelled)
                    {
                        shipment.CancellationReason = command.CancellationReason!.Trim();
                    }

                    shipment.Version += 1;
                    shipment.UpdatedAtUtc = now;
                    shipment.LastModifiedBySubjectId = actorSubjectId;

                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is not null)
                    {
                        exportCase.Version += 1;
                        exportCase.UpdatedAtUtc = now;
                        exportCase.LastModifiedBySubjectId = actorSubjectId;

                        var helper = new ExportReadinessPersistence(dbContext);
                        helper.AppendActivity(
                            exportCase,
                            ExportActivityEventTypes.ShipmentStatusChanged,
                            "Shipment status changed",
                            $"{ExportValueConversions.ToStorage(previousStatus)} → {ExportValueConversions.ToStorage(command.TargetStatus)}",
                            now,
                            ExportActivitySourceType.User,
                            actorSubjectId,
                            correlationId,
                            "export.shipment",
                            shipment.Id);

                        helper.AddAudit(
                            context,
                            ExportAuditEventTypes.ExportShipmentStatusChanged,
                            ExportAuditActions.ExportShipmentStatusChanged,
                            "export.shipment",
                            shipment.Id.ToString(),
                            correlationId,
                            now,
                            new
                            {
                                exportCaseId = command.ExportCaseId.ToString(),
                                shipmentId = shipment.Id.ToString(),
                                previousStatus = ExportValueConversions.ToStorage(previousStatus),
                                newStatus = ExportValueConversions.ToStorage(command.TargetStatus),
                            });

                        helper.AddOutbox(
                            context.TenantId,
                            ExportOutboxMessageTypes.ExportShipmentStatusChanged,
                            "export-shipment",
                            shipment.Id.ToString(),
                            correlationId,
                            $"{shipment.Id}:{shipment.Version}:{ExportOutboxMessageTypes.ExportShipmentStatusChanged}",
                            now,
                            new Dictionary<string, string?>
                            {
                                ["tenantId"] = context.TenantId.ToString(),
                                ["facilityId"] = context.FacilityId.ToString(),
                                ["exportCaseId"] = command.ExportCaseId.ToString(),
                                ["shipmentId"] = shipment.Id.ToString(),
                                ["status"] = ExportValueConversions.ToStorage(shipment.Status),
                                ["occurredAtUtc"] = now.ToString("O"),
                            });
                    }

                    await dbContext.SaveChangesAsync(ct);
                    return ExportShipmentWriteResult.Success(shipment.Id, shipment.Version, ExportValueConversions.ToStorage(shipment.Status));
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportShipmentWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The shipment has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportShipmentWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export shipment service is unavailable.");
        }
    }

    private async Task<ExportCase?> LoadCaseAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportCases
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId
                    && c.FacilityId == context.FacilityId
                    && c.Id == exportCaseId,
                cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}

public sealed class UnavailableExportShipmentWriteRepository : IExportShipmentWriteRepository
{
    private static ExportShipmentWriteResult Unavailable() =>
        ExportShipmentWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export shipment persistence is disabled.");

    public Task<ExportShipmentWriteResult> CreateAsync(ExportOperationContext context, CreateExportShipmentCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable());
    public Task<ExportShipmentWriteResult> UpdateAsync(ExportOperationContext context, UpdateExportShipmentCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable());
    public Task<ExportShipmentWriteResult> TransitionAsync(ExportOperationContext context, TransitionExportShipmentCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable());
}

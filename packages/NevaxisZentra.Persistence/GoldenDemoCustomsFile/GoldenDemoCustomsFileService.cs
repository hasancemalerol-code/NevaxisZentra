using System.Globalization;
using System.Text.Json;

namespace NevaxisZentra.Persistence.GoldenDemoCustomsFile;

public interface IGoldenDemoCustomsFileService
{
    Task<GoldenDemoCustomsFileWorkspaceSnapshot> GetWorkspaceAsync(
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFileWorkspaceSnapshot> AssignOutcomeAsync(
        Guid outcomeId,
        AssignCustomsOutcomeRequest request,
        string subjectId,
        CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFileWorkspaceSnapshot> RunWorkerEvaluationAsync(
        Guid outcomeId,
        string subjectId,
        CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFileWorkspaceSnapshot> CompleteCorrectiveWorkAsync(
        Guid outcomeId,
        Guid workId,
        CompleteCustomsCorrectiveWorkRequest request,
        string subjectId,
        CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFileWorkspaceSnapshot> RecheckEvidenceAsync(
        Guid outcomeId,
        string subjectId,
        CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFileWorkspaceSnapshot> ApprovePackageAsync(
        Guid outcomeId,
        ApproveCustomsPackageRequest request,
        string subjectId,
        CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFileWorkspaceSnapshot> AssemblePackageAsync(
        Guid outcomeId,
        string subjectId,
        CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFileWorkspaceSnapshot> VerifyOutcomeAsync(
        Guid outcomeId,
        VerifyCustomsOutcomeRequest request,
        string subjectId,
        CancellationToken cancellationToken);
    Task ResetPilotOutcomeAsync(Guid outcomeId, CancellationToken cancellationToken);
}

public sealed class GoldenDemoCustomsFileService : IGoldenDemoCustomsFileService
{
    private readonly IGoldenDemoCustomsFileRepository repository;

    public GoldenDemoCustomsFileService(IGoldenDemoCustomsFileRepository repository) =>
        this.repository = repository;

    public async Task<GoldenDemoCustomsFileWorkspaceSnapshot> GetWorkspaceAsync(
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var outcome = await repository.GetPilotOutcomeAsync(tenantId, facilityId, cancellationToken)
            ?? throw new GoldenDemoCustomsFileException(
                GoldenDemoCustomsFileFailureCodes.OutcomeNotFound,
                "No customs file golden demo outcome found for this facility.");
        return await BuildSnapshotAsync(outcome.Id, cancellationToken);
    }

    public async Task<GoldenDemoCustomsFileWorkspaceSnapshot> AssignOutcomeAsync(
        Guid outcomeId,
        AssignCustomsOutcomeRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var outcome = await RequireOutcomeAsync(outcomeId, cancellationToken);
        outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.Assigned;
        await repository.UpdateOutcomeAsync(outcome, cancellationToken);
        await LogActivityAsync(
            outcome,
            GoldenDemoCustomsFileActivityStages.OutcomeAssigned,
            subjectId,
            "Outcome assigned to Customs File Digital Worker",
            "assigned",
            outcome.Id.ToString(),
            cancellationToken);
        return await BuildSnapshotAsync(outcomeId, cancellationToken);
    }

    public async Task<GoldenDemoCustomsFileWorkspaceSnapshot> RunWorkerEvaluationAsync(
        Guid outcomeId,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var outcome = await RequireOutcomeAsync(outcomeId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.ContextLoaded;
        await repository.UpdateOutcomeAsync(outcome, cancellationToken);
        await LogActivityAsync(
            outcome,
            GoldenDemoCustomsFileActivityStages.ContextLoaded,
            GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
            "Order and shipment context loaded",
            "completed",
            outcome.OrderNumber,
            cancellationToken);

        await EnsureInitialDocumentsAsync(outcome, now, cancellationToken);
        outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.DocumentsPrepared;
        await repository.UpdateOutcomeAsync(outcome, cancellationToken);
        await LogActivityAsync(
            outcome,
            GoldenDemoCustomsFileActivityStages.DocumentsPrepared,
            GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
            "Commercial Invoice and Packing List prepared",
            "completed",
            outcome.OrderNumber,
            cancellationToken);

        var documents = await repository.GetDocumentsAsync(outcomeId, cancellationToken);
        var packingList = documents.First(d =>
            d.IsCurrent && d.DocumentType == GoldenDemoCustomsFileDocumentTypes.PackingList);
        await LogActivityAsync(
            outcome,
            GoldenDemoCustomsFileActivityStages.RequirementsEvaluated,
            GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
            "Requirements and consistency checks evaluated",
            "completed",
            outcome.OrderNumber,
            cancellationToken);

        if (GoldenDemoCustomsFileConsistencyEngine.HasNetWeightMismatch(outcome, packingList))
        {
            var packingContent = GoldenDemoCustomsFileJson.DeserializeDocumentContent(packingList.ContentJson);
            var evidence = JsonSerializer.Serialize(new
            {
                shipmentNetWeightKg = outcome.ShipmentNetWeightKg,
                packingListNetWeightKg = packingContent.NetWeightKg,
            });
            var blocker = new GoldenDemoCustomsFileBlocker
            {
                Id = Guid.NewGuid(),
                TenantId = outcome.TenantId,
                FacilityId = outcome.FacilityId,
                OutcomeId = outcome.Id,
                BlockerCode = GoldenDemoCustomsFileBlockerCodes.NetWeightMismatch,
                Status = GoldenDemoCustomsFileBlockerStatuses.Open,
                Reason =
                    $"Packing List net weight {FormatWeight(packingContent.NetWeightKg)} kg does not match shipment net weight {FormatWeight(outcome.ShipmentNetWeightKg)} kg.",
                EvidenceJson = evidence,
                RelatedDocumentId = packingList.Id,
                DetectedAtUtc = now,
            };
            await repository.AddBlockerAsync(blocker, cancellationToken);

            var work = new GoldenDemoCustomsFileCorrectiveWork
            {
                Id = Guid.NewGuid(),
                TenantId = outcome.TenantId,
                FacilityId = outcome.FacilityId,
                OutcomeId = outcome.Id,
                BlockerId = blocker.Id,
                Title = "Correct Packing List Weight Data",
                Status = GoldenDemoCustomsFileCorrectiveWorkStatuses.Created,
                ResponsibleRole = "export-documentation-operator",
                RelatedDocumentId = packingList.Id,
                CreatedAtUtc = now,
            };
            await repository.AddCorrectiveWorkAsync(work, cancellationToken);

            outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.CorrectiveWorkCreated;
            await repository.UpdateOutcomeAsync(outcome, cancellationToken);
            await LogActivityAsync(
                outcome,
                GoldenDemoCustomsFileActivityStages.BlockerDetected,
                GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
                "Packing List net weight mismatch detected",
                "open",
                blocker.Id.ToString(),
                cancellationToken);
            await LogActivityAsync(
                outcome,
                GoldenDemoCustomsFileActivityStages.CorrectiveWorkCreated,
                GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
                work.Title,
                work.Status,
                work.Id.ToString(),
                cancellationToken);
        }
        else
        {
            outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.EvidenceRechecked;
            await repository.UpdateOutcomeAsync(outcome, cancellationToken);
        }

        return await BuildSnapshotAsync(outcomeId, cancellationToken);
    }

    public async Task<GoldenDemoCustomsFileWorkspaceSnapshot> CompleteCorrectiveWorkAsync(
        Guid outcomeId,
        Guid workId,
        CompleteCustomsCorrectiveWorkRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var outcome = await RequireOutcomeAsync(outcomeId, cancellationToken);
        var work = await repository.GetCorrectiveWorkAsync(workId, cancellationToken)
            ?? throw new GoldenDemoCustomsFileException(
                GoldenDemoCustomsFileFailureCodes.CorrectiveWorkNotFound,
                "Corrective work not found.");
        if (work.OutcomeId != outcomeId)
        {
            throw new GoldenDemoCustomsFileException(
                GoldenDemoCustomsFileFailureCodes.CorrectiveWorkNotFound,
                "Corrective work not found for this outcome.");
        }

        if (work.Status == GoldenDemoCustomsFileCorrectiveWorkStatuses.Completed)
        {
            throw new GoldenDemoCustomsFileException(
                GoldenDemoCustomsFileFailureCodes.InvalidState,
                "Corrective work is already completed.");
        }

        var now = DateTimeOffset.UtcNow;
        var documents = await repository.GetDocumentsAsync(outcomeId, cancellationToken);
        var currentPacking = documents.First(d =>
            d.IsCurrent && d.DocumentType == GoldenDemoCustomsFileDocumentTypes.PackingList);
        currentPacking.IsCurrent = false;
        currentPacking.Status = GoldenDemoCustomsFileDocumentStatuses.Superseded;
        await repository.UpdateDocumentAsync(currentPacking, cancellationToken);

        var correctedContent = GoldenDemoCustomsFileDocumentFactory.BuildPackingListContent(
            outcome,
            outcome.ShipmentNetWeightKg);
        var newPacking = new GoldenDemoCustomsFileDocument
        {
            Id = Guid.NewGuid(),
            TenantId = outcome.TenantId,
            FacilityId = outcome.FacilityId,
            OutcomeId = outcome.Id,
            DocumentType = GoldenDemoCustomsFileDocumentTypes.PackingList,
            VersionNumber = currentPacking.VersionNumber + 1,
            IsCurrent = true,
            Status = GoldenDemoCustomsFileDocumentStatuses.Ready,
            ContentJson = GoldenDemoCustomsFileJson.SerializeDocumentContent(correctedContent),
            CreatedAtUtc = now,
        };
        await repository.AddDocumentAsync(newPacking, cancellationToken);

        work.Status = GoldenDemoCustomsFileCorrectiveWorkStatuses.Completed;
        work.CompletedAtUtc = now;
        work.CompletionEvidenceJson = JsonSerializer.Serialize(new
        {
            correctedNetWeightKg = outcome.ShipmentNetWeightKg,
            newDocumentId = newPacking.Id,
            newVersion = newPacking.VersionNumber,
        });
        work.RelatedDocumentId = newPacking.Id;
        await repository.UpdateCorrectiveWorkAsync(work, cancellationToken);

        outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.CorrectiveWorkCompleted;
        await repository.UpdateOutcomeAsync(outcome, cancellationToken);

        await LogActivityAsync(
            outcome,
            GoldenDemoCustomsFileActivityStages.CorrectionCompleted,
            subjectId,
            work.Title,
            "completed",
            work.Id.ToString(),
            cancellationToken);
        await LogActivityAsync(
            outcome,
            GoldenDemoCustomsFileActivityStages.DocumentVersionUpdated,
            GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
            $"Packing List v{newPacking.VersionNumber}",
            newPacking.Status,
            newPacking.Id.ToString(),
            cancellationToken);

        return await BuildSnapshotAsync(outcomeId, cancellationToken);
    }

    public async Task<GoldenDemoCustomsFileWorkspaceSnapshot> RecheckEvidenceAsync(
        Guid outcomeId,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var outcome = await RequireOutcomeAsync(outcomeId, cancellationToken);
        var documents = await repository.GetDocumentsAsync(outcomeId, cancellationToken);
        var blockers = await repository.GetBlockersAsync(outcomeId, cancellationToken);
        var commercialInvoice = documents.FirstOrDefault(d =>
            d.IsCurrent && d.DocumentType == GoldenDemoCustomsFileDocumentTypes.CommercialInvoice);
        var packingList = documents.FirstOrDefault(d =>
            d.IsCurrent && d.DocumentType == GoldenDemoCustomsFileDocumentTypes.PackingList);
        var consistency = GoldenDemoCustomsFileConsistencyEngine.Evaluate(outcome, commercialInvoice, packingList);
        var allPass = consistency.All(c => c.Status == "pass");

        foreach (var blocker in blockers.Where(b => b.Status == GoldenDemoCustomsFileBlockerStatuses.Open))
        {
            if (allPass)
            {
                blocker.Status = GoldenDemoCustomsFileBlockerStatuses.Resolved;
                blocker.ResolvedAtUtc = DateTimeOffset.UtcNow;
                await repository.UpdateBlockerAsync(blocker, cancellationToken);
            }
        }

        outcome.Status = allPass
            ? GoldenDemoCustomsFileOutcomeStatuses.EvidenceRechecked
            : GoldenDemoCustomsFileOutcomeStatuses.CorrectiveWorkCompleted;
        await repository.UpdateOutcomeAsync(outcome, cancellationToken);
        await LogActivityAsync(
            outcome,
            GoldenDemoCustomsFileActivityStages.EvidenceRechecked,
            GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
            "Evidence re-checked against shipment source",
            allPass ? "resolved" : "blocked",
            outcome.ShipmentReference,
            cancellationToken);

        if (allPass)
        {
            outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.ApprovalPending;
            await repository.UpdateOutcomeAsync(outcome, cancellationToken);
            await LogActivityAsync(
                outcome,
                GoldenDemoCustomsFileActivityStages.ApprovalRequested,
                GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
                "Customs package approval requested",
                "pending",
                outcome.Id.ToString(),
                cancellationToken);
        }

        return await BuildSnapshotAsync(outcomeId, cancellationToken);
    }

    public async Task<GoldenDemoCustomsFileWorkspaceSnapshot> ApprovePackageAsync(
        Guid outcomeId,
        ApproveCustomsPackageRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var outcome = await RequireOutcomeAsync(outcomeId, cancellationToken);
        var blockers = await repository.GetBlockersAsync(outcomeId, cancellationToken);
        if (blockers.Any(b => b.Status == GoldenDemoCustomsFileBlockerStatuses.Open))
        {
            throw new GoldenDemoCustomsFileException(
                GoldenDemoCustomsFileFailureCodes.InvalidState,
                "Cannot approve while blocking issues remain.");
        }

        if (outcome.ApprovedAtUtc is not null)
        {
            throw new GoldenDemoCustomsFileException(
                GoldenDemoCustomsFileFailureCodes.InvalidState,
                "Customs package is already approved.");
        }

        outcome.ApprovedBySubjectId = subjectId;
        outcome.ApprovedAtUtc = DateTimeOffset.UtcNow;
        outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.Approved;
        await repository.UpdateOutcomeAsync(outcome, cancellationToken);
        await LogActivityAsync(
            outcome,
            GoldenDemoCustomsFileActivityStages.ApprovalCompleted,
            subjectId,
            "Customs package approved",
            "approved",
            outcome.Id.ToString(),
            cancellationToken);
        return await BuildSnapshotAsync(outcomeId, cancellationToken);
    }

    public async Task<GoldenDemoCustomsFileWorkspaceSnapshot> AssemblePackageAsync(
        Guid outcomeId,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var outcome = await RequireOutcomeAsync(outcomeId, cancellationToken);
        if (outcome.ApprovedAtUtc is null)
        {
            throw new GoldenDemoCustomsFileException(
                GoldenDemoCustomsFileFailureCodes.InvalidState,
                "Customs package must be approved before assembly.");
        }

        var documents = await repository.GetDocumentsAsync(outcomeId, cancellationToken);
        var currentDocs = documents.Where(d => d.IsCurrent).ToList();
        var manifest = currentDocs
            .Select(d => $"{DocumentLabel(d.DocumentType)} v{d.VersionNumber} — {d.Status.ToUpperInvariant()}")
            .ToList();
        manifest.Add($"Order {outcome.OrderNumber} · Shipment {outcome.ShipmentReference}");

        var package = new GoldenDemoCustomsFilePackage
        {
            Id = Guid.NewGuid(),
            TenantId = outcome.TenantId,
            FacilityId = outcome.FacilityId,
            OutcomeId = outcome.Id,
            PackageVersion = 1,
            ManifestJson = GoldenDemoCustomsFileJson.SerializeManifest(manifest),
            DocumentReferenceIdsJson = GoldenDemoCustomsFileJson.SerializeGuidList(currentDocs.Select(d => d.Id)),
            AssembledAtUtc = DateTimeOffset.UtcNow,
        };
        await repository.AddPackageAsync(package, cancellationToken);

        outcome.CurrentPackageId = package.Id;
        outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.PackageAssembled;
        await repository.UpdateOutcomeAsync(outcome, cancellationToken);
        await LogActivityAsync(
            outcome,
            GoldenDemoCustomsFileActivityStages.PackageAssembled,
            GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
            "Customs clearance package assembled",
            "assembled",
            package.Id.ToString(),
            cancellationToken);
        return await BuildSnapshotAsync(outcomeId, cancellationToken);
    }

    public async Task<GoldenDemoCustomsFileWorkspaceSnapshot> VerifyOutcomeAsync(
        Guid outcomeId,
        VerifyCustomsOutcomeRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var outcome = await RequireOutcomeAsync(outcomeId, cancellationToken);
        var snapshot = await BuildSnapshotAsync(outcomeId, cancellationToken);
        var run = new GoldenDemoCustomsFileVerificationRun
        {
            Id = Guid.NewGuid(),
            TenantId = outcome.TenantId,
            FacilityId = outcome.FacilityId,
            OutcomeId = outcome.Id,
            Result = snapshot.Verification.Result,
            ChecksJson = JsonSerializer.Serialize(snapshot.Verification.Checks),
            Summary = snapshot.Verification.Summary,
            TriggeredBySubjectId = subjectId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        await repository.AddVerificationAsync(run, cancellationToken);

        outcome.Status = snapshot.Verification.Result switch
        {
            GoldenDemoCustomsFileVerificationResults.VerifiedReady => GoldenDemoCustomsFileOutcomeStatuses.VerifiedReady,
            GoldenDemoCustomsFileVerificationResults.PartiallyReady => GoldenDemoCustomsFileOutcomeStatuses.PartiallyReady,
            GoldenDemoCustomsFileVerificationResults.NotReady => GoldenDemoCustomsFileOutcomeStatuses.NotReady,
            _ => GoldenDemoCustomsFileOutcomeStatuses.InsufficientData,
        };
        await repository.UpdateOutcomeAsync(outcome, cancellationToken);
        await LogActivityAsync(
            outcome,
            GoldenDemoCustomsFileActivityStages.VerificationExecuted,
            subjectId,
            "Outcome verification executed",
            snapshot.Verification.Result,
            run.Id.ToString(),
            cancellationToken);

        if (snapshot.Verification.Result == GoldenDemoCustomsFileVerificationResults.VerifiedReady)
        {
            await LogActivityAsync(
                outcome,
                GoldenDemoCustomsFileActivityStages.VerifiedReady,
                GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker,
                "Customs clearance package verified ready",
                "verified-ready",
                run.Id.ToString(),
                cancellationToken);
        }

        return await BuildSnapshotAsync(outcomeId, cancellationToken);
    }

    public Task ResetPilotOutcomeAsync(Guid outcomeId, CancellationToken cancellationToken) =>
        repository.ResetPilotOutcomeAsync(outcomeId, cancellationToken);

    private async Task EnsureInitialDocumentsAsync(
        GoldenDemoCustomsFileOutcome outcome,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetDocumentsAsync(outcome.Id, cancellationToken);
        if (existing.Count > 0)
        {
            return;
        }

        var invoiceContent = GoldenDemoCustomsFileDocumentFactory.BuildCommercialInvoiceContent(outcome);
        await repository.AddDocumentAsync(
            new GoldenDemoCustomsFileDocument
            {
                Id = Guid.NewGuid(),
                TenantId = outcome.TenantId,
                FacilityId = outcome.FacilityId,
                OutcomeId = outcome.Id,
                DocumentType = GoldenDemoCustomsFileDocumentTypes.CommercialInvoice,
                VersionNumber = 1,
                IsCurrent = true,
                Status = GoldenDemoCustomsFileDocumentStatuses.Ready,
                ContentJson = GoldenDemoCustomsFileJson.SerializeDocumentContent(invoiceContent),
                CreatedAtUtc = now,
            },
            cancellationToken);

        var mismatchNetWeight = outcome.ShipmentNetWeightKg - 12.5m;
        var packingContent = GoldenDemoCustomsFileDocumentFactory.BuildPackingListContent(outcome, mismatchNetWeight);
        await repository.AddDocumentAsync(
            new GoldenDemoCustomsFileDocument
            {
                Id = Guid.NewGuid(),
                TenantId = outcome.TenantId,
                FacilityId = outcome.FacilityId,
                OutcomeId = outcome.Id,
                DocumentType = GoldenDemoCustomsFileDocumentTypes.PackingList,
                VersionNumber = 1,
                IsCurrent = true,
                Status = GoldenDemoCustomsFileDocumentStatuses.Invalid,
                ContentJson = GoldenDemoCustomsFileJson.SerializeDocumentContent(packingContent),
                CreatedAtUtc = now,
            },
            cancellationToken);

        var trade = GoldenDemoCustomsFileJson.DeserializeTradeRequirements(outcome.TradeRequirementsJson);
        if (trade.RequiresAtr)
        {
            await AddStatusDocumentAsync(outcome, GoldenDemoCustomsFileDocumentTypes.AtrStatus, now, cancellationToken);
        }

        if (trade.RequiresEur1)
        {
            await AddStatusDocumentAsync(outcome, GoldenDemoCustomsFileDocumentTypes.Eur1Status, now, cancellationToken);
        }

        if (trade.RequiresCertificateOfOrigin)
        {
            await AddStatusDocumentAsync(
                outcome,
                GoldenDemoCustomsFileDocumentTypes.CertificateOfOriginStatus,
                now,
                cancellationToken);
        }
    }

    private async Task AddStatusDocumentAsync(
        GoldenDemoCustomsFileOutcome outcome,
        string documentType,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await repository.AddDocumentAsync(
            new GoldenDemoCustomsFileDocument
            {
                Id = Guid.NewGuid(),
                TenantId = outcome.TenantId,
                FacilityId = outcome.FacilityId,
                OutcomeId = outcome.Id,
                DocumentType = documentType,
                VersionNumber = 1,
                IsCurrent = true,
                Status = GoldenDemoCustomsFileDocumentStatuses.Ready,
                ContentJson = JsonSerializer.Serialize(new { status = "not-required-for-demo-route" }),
                CreatedAtUtc = now,
            },
            cancellationToken);
    }

    private async Task<GoldenDemoCustomsFileWorkspaceSnapshot> BuildSnapshotAsync(
        Guid outcomeId,
        CancellationToken cancellationToken)
    {
        var outcome = await RequireOutcomeAsync(outcomeId, cancellationToken);
        var documents = await repository.GetDocumentsAsync(outcomeId, cancellationToken);
        var blockers = await repository.GetBlockersAsync(outcomeId, cancellationToken);
        var works = await repository.GetCorrectiveWorksAsync(outcomeId, cancellationToken);
        var activities = await repository.GetActivitiesAsync(outcomeId, cancellationToken);
        var package = await repository.GetPackageAsync(outcomeId, cancellationToken);
        var verification = await repository.GetLatestVerificationAsync(outcomeId, cancellationToken);
        return GoldenDemoCustomsFileSnapshotProjector.Project(
            outcome,
            documents,
            blockers,
            works,
            activities,
            package,
            verification);
    }

    private async Task<GoldenDemoCustomsFileOutcome> RequireOutcomeAsync(
        Guid outcomeId,
        CancellationToken cancellationToken) =>
        await repository.GetOutcomeAsync(outcomeId, cancellationToken)
        ?? throw new GoldenDemoCustomsFileException(
            GoldenDemoCustomsFileFailureCodes.OutcomeNotFound,
            "Customs file outcome not found.");

    private async Task LogActivityAsync(
        GoldenDemoCustomsFileOutcome outcome,
        string stageCode,
        string actor,
        string action,
        string status,
        string evidenceReference,
        CancellationToken cancellationToken)
    {
        await repository.AddActivityAsync(
            new GoldenDemoCustomsFileActivity
            {
                Id = Guid.NewGuid(),
                TenantId = outcome.TenantId,
                FacilityId = outcome.FacilityId,
                OutcomeId = outcome.Id,
                StageCode = stageCode,
                Actor = actor,
                Action = action,
                Status = status,
                EvidenceReference = evidenceReference,
                OccurredAtUtc = DateTimeOffset.UtcNow,
            },
            cancellationToken);
    }

    private static string DocumentLabel(string documentType) => documentType switch
    {
        GoldenDemoCustomsFileDocumentTypes.CommercialInvoice => "Commercial Invoice",
        GoldenDemoCustomsFileDocumentTypes.PackingList => "Packing List",
        GoldenDemoCustomsFileDocumentTypes.AtrStatus => "A.TR Status",
        GoldenDemoCustomsFileDocumentTypes.Eur1Status => "EUR.1 Status",
        GoldenDemoCustomsFileDocumentTypes.CertificateOfOriginStatus => "Certificate of Origin Status",
        _ => documentType,
    };

    private static string FormatWeight(decimal value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportAiContextBuilder(CrevaFactOSDbContext dbContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<ExportAiContextSnapshotModel?> BuildForExportCaseAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken)
    {
        var exportCase = await dbContext.ExportCases
            .AsNoTracking()
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId
                    && c.FacilityId == context.FacilityId
                    && c.Id == exportCaseId,
                cancellationToken);

        if (exportCase is null) return null;

        var documents = await dbContext.ExportDocuments
            .AsNoTracking()
            .Where(d => d.TenantId == context.TenantId && d.FacilityId == context.FacilityId && d.ExportCaseId == exportCaseId)
            .OrderBy(d => d.DocumentType)
            .ToListAsync(cancellationToken);

        var shipments = await dbContext.ExportShipments
            .AsNoTracking()
            .Where(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId && s.ExportCaseId == exportCaseId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        var broker = await dbContext.ExportBrokerWorkspaces
            .AsNoTracking()
            .Where(w => w.TenantId == context.TenantId && w.FacilityId == context.FacilityId && w.ExportCaseId == exportCaseId
                && w.Status != ExportBrokerWorkspaceStatus.Cancelled && w.Status != ExportBrokerWorkspaceStatus.Closed)
            .OrderByDescending(w => w.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        int openCorrections = 0;
        if (broker is not null)
        {
            openCorrections = await dbContext.ExportBrokerCorrections
                .AsNoTracking()
                .CountAsync(
                    c => c.TenantId == context.TenantId && c.BrokerWorkspaceId == broker.Id
                        && c.Status != ExportBrokerCorrectionStatus.Resolved,
                    cancellationToken);
        }

        ExportAiContextComplianceRef? complianceRef = null;
        if (exportCase.CurrentComplianceEvaluationId.HasValue)
        {
            var evaluation = await dbContext.ExportComplianceEvaluations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId
                        && e.Id == exportCase.CurrentComplianceEvaluationId.Value,
                    cancellationToken);

            if (evaluation is not null)
            {
                var requirements = await dbContext.ExportComplianceRequirements
                    .AsNoTracking()
                    .Where(r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId
                        && r.EvaluationId == evaluation.Id)
                    .OrderBy(r => r.RequirementCode)
                    .ToListAsync(cancellationToken);

                string? rulePackName = null;
                if (evaluation.RulePackId.HasValue)
                {
                    var pack = await dbContext.ExportCountryRulePacks
                        .AsNoTracking()
                        .SingleOrDefaultAsync(p => p.Id == evaluation.RulePackId.Value, cancellationToken);
                    rulePackName = pack?.Name;
                }

                complianceRef = new ExportAiContextComplianceRef(
                    evaluation.Id,
                    evaluation.EvaluationNumber,
                    ExportValueConversions.ToStorage(evaluation.Result),
                    evaluation.RulePackVersionNumber,
                    rulePackName,
                    requirements.Select(r => new ExportAiContextRequirementRef(
                        r.Id,
                        r.RequirementCode,
                        r.Title,
                        ExportValueConversions.ToStorage(r.Status),
                        r.Blocking,
                        r.RequiredDocumentType,
                        r.SourceReference)).ToArray());
            }
        }

        ExportAiContextResearchRef? researchRef = null;
        if (exportCase.LinkedResearchCaseId.HasValue)
        {
            var research = await dbContext.ExportCountryResearchCases
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId
                        && r.Id == exportCase.LinkedResearchCaseId.Value,
                    cancellationToken);

            if (research is not null)
            {
                var findings = await dbContext.ExportCountryResearchFindings
                    .AsNoTracking()
                    .Where(f => f.CountryResearchCaseId == research.Id)
                    .ToListAsync(cancellationToken);

                researchRef = new ExportAiContextResearchRef(
                    research.Id,
                    research.ResearchNumber,
                    ExportValueConversions.ToStorage(research.Status),
                    findings.Count,
                    findings.Count(f => f.VerificationStatus != ExportSourceVerificationStatus.Verified));
            }
        }

        var knowledge = exportCase.DestinationCountryCode.Length == 2
            ? await dbContext.ExportCountryKnowledgeEntries
                .AsNoTracking()
                .Where(k => k.TenantId == context.TenantId
                    && k.CountryCode == exportCase.DestinationCountryCode)
                .OrderByDescending(k => k.UpdatedAtUtc)
                .Take(10)
                .ToListAsync(cancellationToken)
            : [];

        var production = await ExportCrossModuleIntelligence.LoadProductionAsync(
            dbContext, context, exportCase.SourceOrderId, cancellationToken);
        var planning = await ExportCrossModuleIntelligence.LoadPlanningAsync(
            dbContext, context, exportCase.SourceOrderId, shipments, cancellationToken);
        var quality = ExportCrossModuleIntelligence.LoadQuality();
        var erp = ExportCrossModuleIntelligence.LoadErp(exportCase.SourceOrderReference, exportCase.CustomerName);
        var risks = ExportCrossModuleIntelligence.DetectRisks(
            exportCase, shipments, complianceRef, production, planning, quality, erp);

        return new ExportAiContextSnapshotModel(
            exportCaseId,
            exportCase.LinkedResearchCaseId,
            new ExportAiContextCaseRef(
                exportCase.Id,
                exportCase.CaseNumber,
                exportCase.DestinationCountryCode,
                exportCase.OriginCountryCode,
                ExportValueConversions.ToStorage(exportCase.ReadinessStatus),
                exportCase.ReadinessPercent,
                ExportValueConversions.ToStorage(exportCase.ComplianceStatus),
                exportCase.HsCode,
                ExportValueConversions.ToStorage(exportCase.HsCodeStatus),
                ExportValueConversions.ToStorage(exportCase.OriginConfirmationStatus)),
            exportCase.SourceOrderReference,
            exportCase.CustomerName,
            documents.Select(d => new ExportAiContextDocumentRef(
                d.Id,
                d.DocumentType,
                ExportValueConversions.ToStorage(d.DocumentStatus),
                d.DisplayName)).ToArray(),
            ExportValueConversions.ToStorage(exportCase.PackingStatus),
            shipments.Select(s => new ExportAiContextShipmentRef(
                s.Id,
                s.ShipmentReference,
                ExportValueConversions.ToStorage(s.Status),
                s.PlannedDepartureAtUtc,
                s.PlannedPickupAtUtc)).ToArray(),
            broker is null ? null : new ExportAiContextBrokerRef(
                broker.Id,
                ExportValueConversions.ToStorage(broker.Status),
                openCorrections),
            complianceRef,
            researchRef,
            knowledge.Select(k => new ExportAiContextKnowledgeRef(
                k.Id,
                k.Title,
                ExportValueConversions.ToStorage(k.Authority),
                ExportValueConversions.ToStorage(k.VerificationStatus))).ToArray(),
            production,
            planning,
            quality,
            erp,
            risks,
            DateTimeOffset.UtcNow);
    }

    public async Task<ExportAiContextSnapshotModel?> BuildForResearchCaseAsync(
        ExportOperationContext context,
        Guid researchCaseId,
        CancellationToken cancellationToken)
    {
        var research = await dbContext.ExportCountryResearchCases
            .AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId && r.Id == researchCaseId,
                cancellationToken);

        if (research is null) return null;

        var findings = await dbContext.ExportCountryResearchFindings
            .AsNoTracking()
            .Where(f => f.CountryResearchCaseId == researchCaseId)
            .OrderBy(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var knowledge = await dbContext.ExportCountryKnowledgeEntries
            .AsNoTracking()
            .Where(k => k.TenantId == context.TenantId && k.CountryCode == research.CountryCode)
            .OrderByDescending(k => k.UpdatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        var researchRef = new ExportAiContextResearchRef(
            research.Id,
            research.ResearchNumber,
            ExportValueConversions.ToStorage(research.Status),
            findings.Count,
            findings.Count(f => f.VerificationStatus != ExportSourceVerificationStatus.Verified));

        ExportAiContextCaseRef? caseRef = null;
        if (research.LinkedExportCaseId.HasValue)
        {
            return await BuildForExportCaseAsync(context, research.LinkedExportCaseId.Value, cancellationToken);
        }

        return new ExportAiContextSnapshotModel(
            research.LinkedExportCaseId,
            researchCaseId,
            caseRef,
            null,
            null,
            [],
            "unknown",
            [],
            null,
            null,
            researchRef,
            knowledge.Select(k => new ExportAiContextKnowledgeRef(
                k.Id,
                k.Title,
                ExportValueConversions.ToStorage(k.Authority),
                ExportValueConversions.ToStorage(k.VerificationStatus))).ToArray(),
            ExportCrossModuleIntelligence.UnavailableProduction(),
            ExportCrossModuleIntelligence.UnavailablePlanning(),
            ExportCrossModuleIntelligence.LoadQuality(),
            ExportCrossModuleIntelligence.UnavailableErp(),
            [],
            DateTimeOffset.UtcNow);
    }

    public static string Serialize(ExportAiContextSnapshotModel model) =>
        JsonSerializer.Serialize(model, JsonOptions);

    public static ExportAiContextSnapshotModel? Deserialize(string json) =>
        JsonSerializer.Deserialize<ExportAiContextSnapshotModel>(json, JsonOptions);

    public static string ComputeHash(string json)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

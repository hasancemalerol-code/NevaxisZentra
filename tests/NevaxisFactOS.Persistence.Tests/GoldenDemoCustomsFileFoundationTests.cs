using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.GoldenDemoCustomsFile;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class GoldenDemoCustomsFileFoundationTests
{
    [Fact]
    public void PermissionKeysAreStable()
    {
        Assert.Equal("golden-demo-customs-file.read", GoldenDemoCustomsFilePermissionKeys.Read);
        Assert.Equal("golden-demo-customs-file.manage", GoldenDemoCustomsFilePermissionKeys.Manage);
        Assert.Equal("golden-demo-customs-file.approve", GoldenDemoCustomsFilePermissionKeys.Approve);
        Assert.Equal("golden-demo-customs-file.execute", GoldenDemoCustomsFilePermissionKeys.Execute);
        Assert.Equal("golden-demo-customs-file.verify", GoldenDemoCustomsFilePermissionKeys.Verify);
        Assert.Equal("golden-demo-customs-file.reset", GoldenDemoCustomsFilePermissionKeys.Reset);
    }

    [Fact]
    public void ConsistencyEngineDetectsNetWeightMismatch()
    {
        var outcome = BuildOutcome(shipmentNetWeightKg: 485m);
        var invoice = BuildDocument(
            GoldenDemoCustomsFileDocumentTypes.CommercialInvoice,
            485m,
            GoldenDemoCustomsFileDocumentStatuses.Ready);
        var packing = BuildDocument(
            GoldenDemoCustomsFileDocumentTypes.PackingList,
            472.5m,
            GoldenDemoCustomsFileDocumentStatuses.Invalid);

        var checks = GoldenDemoCustomsFileConsistencyEngine.Evaluate(outcome, invoice, packing);

        Assert.Contains(checks, c => c.CheckCode == "net-weight-consistency" && c.Status == "fail");
        Assert.True(GoldenDemoCustomsFileConsistencyEngine.HasNetWeightMismatch(outcome, packing));
    }

    [Fact]
    public void CompletedWorkAloneDoesNotClearBlockerUntilRecheck()
    {
        var outcome = BuildOutcome(485m);
        var invoice = BuildDocument(
            GoldenDemoCustomsFileDocumentTypes.CommercialInvoice,
            485m,
            GoldenDemoCustomsFileDocumentStatuses.Ready);
        var packingInvalid = BuildDocument(
            GoldenDemoCustomsFileDocumentTypes.PackingList,
            472.5m,
            GoldenDemoCustomsFileDocumentStatuses.Invalid);
        var blocker = new GoldenDemoCustomsFileBlocker
        {
            Id = Guid.NewGuid(),
            BlockerCode = GoldenDemoCustomsFileBlockerCodes.NetWeightMismatch,
            Status = GoldenDemoCustomsFileBlockerStatuses.Open,
        };

        var requirementsBefore = GoldenDemoCustomsFileRequirementEngine.Evaluate(
            outcome,
            [invoice, packingInvalid],
            [blocker],
            GoldenDemoCustomsFileConsistencyEngine.Evaluate(outcome, invoice, packingInvalid));
        Assert.Contains(
            requirementsBefore,
            r => r.RequirementCode == GoldenDemoCustomsFileRequirementCodes.NoBlockingIssues && r.Status == "fail");

        var packingCorrected = BuildDocument(
            GoldenDemoCustomsFileDocumentTypes.PackingList,
            485m,
            GoldenDemoCustomsFileDocumentStatuses.Ready,
            versionNumber: 2);
        blocker.Status = GoldenDemoCustomsFileBlockerStatuses.Resolved;
        var requirementsAfter = GoldenDemoCustomsFileRequirementEngine.Evaluate(
            outcome,
            [invoice, packingCorrected],
            [blocker],
            GoldenDemoCustomsFileConsistencyEngine.Evaluate(outcome, invoice, packingCorrected));
        Assert.Contains(
            requirementsAfter,
            r => r.RequirementCode == GoldenDemoCustomsFileRequirementCodes.PackingDataConsistent && r.Status == "pass");
    }

    [Fact]
    public void VerificationEngineRequiresEvidenceForVerifiedReady()
    {
        var outcome = BuildOutcome(485m);
        outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.PackageAssembled;
        outcome.ApprovedAtUtc = DateTimeOffset.UtcNow;
        var invoice = BuildDocument(
            GoldenDemoCustomsFileDocumentTypes.CommercialInvoice,
            485m,
            GoldenDemoCustomsFileDocumentStatuses.Ready);
        var packing = BuildDocument(
            GoldenDemoCustomsFileDocumentTypes.PackingList,
            485m,
            GoldenDemoCustomsFileDocumentStatuses.Ready,
            versionNumber: 2);
        var checks = GoldenDemoCustomsFileConsistencyEngine.Evaluate(outcome, invoice, packing);
        var requirements = GoldenDemoCustomsFileRequirementEngine.Evaluate(outcome, [invoice, packing], [], checks);
        var package = new GoldenDemoCustomsFilePackage
        {
            Id = Guid.NewGuid(),
            PackageVersion = 1,
            ManifestJson = "[]",
            DocumentReferenceIdsJson = "[]",
            AssembledAtUtc = DateTimeOffset.UtcNow,
        };

        var verification = GoldenDemoCustomsFileVerificationEngine.Evaluate(
            outcome,
            requirements,
            package,
            [],
            null);

        Assert.Equal(GoldenDemoCustomsFileVerificationResults.VerifiedReady, verification.Result);
        var hero = GoldenDemoCustomsFileHeroEngine.Build(
            verification,
            [invoice, packing],
            [],
            package,
            outcome,
            checks);
        Assert.True(hero.ShowSuccessHero);
        Assert.Equal("GÜMRÜK DOSYASI HAZIR", hero.Headline);
    }

    [Fact]
    public void VerificationEngineDoesNotReturnVerifiedReadyWithoutApproval()
    {
        var outcome = BuildOutcome(485m);
        outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.PackageAssembled;
        var invoice = BuildDocument(
            GoldenDemoCustomsFileDocumentTypes.CommercialInvoice,
            485m,
            GoldenDemoCustomsFileDocumentStatuses.Ready);
        var packing = BuildDocument(
            GoldenDemoCustomsFileDocumentTypes.PackingList,
            485m,
            GoldenDemoCustomsFileDocumentStatuses.Ready);
        var requirements = GoldenDemoCustomsFileRequirementEngine.Evaluate(
            outcome,
            [invoice, packing],
            [],
            GoldenDemoCustomsFileConsistencyEngine.Evaluate(outcome, invoice, packing));
        var package = new GoldenDemoCustomsFilePackage { Id = Guid.NewGuid(), PackageVersion = 1 };

        var verification = GoldenDemoCustomsFileVerificationEngine.Evaluate(
            outcome,
            requirements,
            package,
            [],
            null);

        Assert.NotEqual(GoldenDemoCustomsFileVerificationResults.VerifiedReady, verification.Result);
    }

    private static GoldenDemoCustomsFileOutcome BuildOutcome(decimal shipmentNetWeightKg) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrderNumber = "EXP-DEMO-DE-001",
            CustomerName = "PackPro GmbH",
            DestinationCountry = "Germany",
            Currency = "EUR",
            Incoterm = "DAP",
            PackageCount = 12,
            ShipmentGrossWeightKg = 510m,
            ShipmentNetWeightKg = shipmentNetWeightKg,
            ShipmentReference = "SHP-DE-2026-0142",
            ProductLinesJson = GoldenDemoCustomsFileJson.SerializeProductLines(
            [
                new GoldenDemoCustomsFileProductLine(
                    "PP-LID-240",
                    "PP Container Lid 240mm",
                    12000,
                    0.42m,
                    "EUR",
                    "3923.50.00",
                    "TR"),
            ]),
            TradeRequirementsJson = GoldenDemoCustomsFileJson.SerializeTradeRequirements(
                new GoldenDemoCustomsFileTradeRequirements(false, false, false)),
        };

    private static GoldenDemoCustomsFileDocument BuildDocument(
        string documentType,
        decimal netWeightKg,
        string status,
        int versionNumber = 1) =>
        new()
        {
            Id = Guid.NewGuid(),
            DocumentType = documentType,
            VersionNumber = versionNumber,
            IsCurrent = true,
            Status = status,
            ContentJson = GoldenDemoCustomsFileJson.SerializeDocumentContent(
                new GoldenDemoCustomsFileDocumentContent(
                    "EXP-DEMO-DE-001",
                    "SHP-DE-2026-0142",
                    12,
                    netWeightKg,
                    510m,
                    GoldenDemoCustomsFileJson.DeserializeProductLines(
                        GoldenDemoCustomsFileJson.SerializeProductLines(
                        [
                            new GoldenDemoCustomsFileProductLine(
                                "PP-LID-240",
                                "PP Container Lid 240mm",
                                12000,
                                0.42m,
                                "EUR",
                                "3923.50.00",
                                "TR"),
                        ])),
                    "EUR",
                    "DAP",
                    "Germany",
                    "PackPro GmbH")),
        };
}

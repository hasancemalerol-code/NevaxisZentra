using System.Text;
using NevaxisFactOS.Persistence.Integration;
using NevaxisFactOS.Persistence.Integration.FileImport;
using NevaxisFactOS.Persistence.Integration.Mapping;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class IntegrationMappingValidationTests
{
    [Fact]
    public void RejectsDuplicateSourceAndTargetMappings()
    {
        var definition = new IntegrationMappingDefinition
        {
            SourceEntityType = "materials",
            SourceKeyField = "code",
            FieldMappings =
            [
                new IntegrationFieldMappingRule { SourceField = "code", TargetField = "code", Required = true },
                new IntegrationFieldMappingRule { SourceField = "code", TargetField = "name", Required = true },
            ],
        };

        var result = IntegrationMappingValidator.ValidateDefinition(
            definition,
            ["code", "name"]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "duplicate-source-field");
    }

    [Fact]
    public void RequiresMaterialCodeAndNameTargets()
    {
        var definition = new IntegrationMappingDefinition
        {
            SourceEntityType = "materials",
            SourceKeyField = "code",
            FieldMappings =
            [
                new IntegrationFieldMappingRule { SourceField = "code", TargetField = "code", Required = true },
            ],
        };

        var result = IntegrationMappingValidator.ValidateDefinition(definition, ["code"]);
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "required-target-missing");
    }

    [Fact]
    public void RejectsIncompatibleDecimalValueDuringRowMapping()
    {
        var definition = BuildMaterialDefinition();
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = "MAT-1",
            ["name"] = "Test",
            ["qty"] = "not-a-number",
        };
        definition.FieldMappings.Add(new IntegrationFieldMappingRule
        {
            SourceField = "qty",
            TargetField = "qty",
            TargetType = IntegrationMappingTargetTypes.Number,
        });

        var mapped = IntegrationMappingValidator.MapRow(1, row, definition);
        Assert.False(mapped.IsValid);
        Assert.Contains(
            mapped.Issues,
            issue => issue.Code == IntegrationQuarantineReasonCodes.MalformedDecimal);
    }

    private static IntegrationMappingDefinition BuildMaterialDefinition() =>
        new()
        {
            SourceEntityType = "materials",
            SourceKeyField = "code",
            FieldMappings =
            [
                new IntegrationFieldMappingRule
                {
                    SourceField = "code",
                    TargetField = "code",
                    Required = true,
                },
                new IntegrationFieldMappingRule
                {
                    SourceField = "name",
                    TargetField = "name",
                    Required = true,
                },
            ],
        };
}

public sealed class IntegrationDryRunEngineTests
{
    [Fact]
    public void DryRunProducesCountsWithoutMutationSemantics()
    {
        var definition = new IntegrationMappingDefinition
        {
            TargetCanonicalEntityType = IntegrationCanonicalProjectionTypes.Material,
            SourceEntityType = "materials",
            SourceKeyField = "code",
            FieldMappings =
            [
                new IntegrationFieldMappingRule { SourceField = "code", TargetField = "code", Required = true },
                new IntegrationFieldMappingRule { SourceField = "name", TargetField = "name", Required = true },
            ],
        };

        var table = new IntegrationFileTable(
            IntegrationFileKinds.Csv,
            ["code", "name"],
            [
                new Dictionary<string, string> { ["code"] = "MAT-1", ["name"] = "Valid" },
                new Dictionary<string, string> { ["code"] = "MAT-2", ["name"] = "" },
                new Dictionary<string, string> { ["code"] = "MAT-1", ["name"] = "Duplicate" },
            ]);

        var fingerprint = IntegrationSchemaFingerprint.Compute(table.Headers);
        var result = IntegrationFileDryRunEngine.Execute(table, definition, fingerprint, new Dictionary<string, IntegrationSourceIdentity>());

        Assert.True(result.Success);
        Assert.Equal(3, result.TotalRows);
        Assert.Equal(1, result.ValidRows);
        Assert.Equal(1, result.InvalidRows);
        Assert.Equal(1, result.DuplicateRows);
        Assert.Equal(1, result.NewProjections);
    }

    [Fact]
    public void SchemaDriftBlocksSilentExecution()
    {
        var definition = new IntegrationMappingDefinition
        {
            SourceEntityType = "materials",
            SourceKeyField = "code",
            FieldMappings =
            [
                new IntegrationFieldMappingRule { SourceField = "code", TargetField = "code", Required = true },
                new IntegrationFieldMappingRule { SourceField = "name", TargetField = "name", Required = true },
            ],
        };

        var table = new IntegrationFileTable(
            IntegrationFileKinds.Csv,
            ["code", "name", "unexpected"],
            [new Dictionary<string, string> { ["code"] = "MAT-1", ["name"] = "Valid", ["unexpected"] = "x" }]);

        var result = IntegrationFileDryRunEngine.Execute(
            table,
            definition,
            "old-fingerprint",
            new Dictionary<string, IntegrationSourceIdentity>());

        Assert.False(result.Success);
        Assert.Equal("integration.schema-drift", result.ErrorCode);
    }

    [Fact]
    public void ExistingIdentityMarksUnchangedRows()
    {
        var definition = new IntegrationMappingDefinition
        {
            SourceEntityType = "materials",
            SourceKeyField = "code",
            FieldMappings =
            [
                new IntegrationFieldMappingRule { SourceField = "code", TargetField = "code", Required = true },
                new IntegrationFieldMappingRule { SourceField = "name", TargetField = "name", Required = true },
            ],
            Defaults = new Dictionary<string, string> { ["SourceType"] = "imported" },
        };

        var row = new Dictionary<string, string> { ["code"] = "MAT-1", ["name"] = "Valid" };
        var mapped = IntegrationMappingValidator.MapRow(1, row, definition);
        var existing = new Dictionary<string, IntegrationSourceIdentity>
        {
            ["MAT-1"] = new IntegrationSourceIdentity
            {
                SourceRecordId = "MAT-1",
                ContentFingerprint = mapped.ContentFingerprint,
            },
        };

        var table = new IntegrationFileTable(
            IntegrationFileKinds.Csv,
            ["code", "name"],
            [row]);

        var fingerprint = IntegrationSchemaFingerprint.Compute(table.Headers);
        var result = IntegrationFileDryRunEngine.Execute(table, definition, fingerprint, existing);

        Assert.True(result.Success);
        Assert.Equal(1, result.Unchanged);
        Assert.Equal(0, result.NewProjections);
    }
}

public sealed class IntegrationXlsxParserTests
{
    [Fact]
    public void XlsxPreviewReadsHeadersAndRowsWithoutFormulaExecution()
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var sheet = workbook.AddWorksheet("Materials");
        sheet.Cell(1, 1).Value = "code";
        sheet.Cell(1, 2).Value = "name";
        sheet.Cell(2, 1).Value = "MAT-1";
        sheet.Cell(2, 2).Value = "Granule A";
        sheet.Cell(3, 1).Value = "MAT-2";
        sheet.Cell(3, 2).FormulaA1 = "=1+1";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var parsed = IntegrationXlsxParser.Parse(stream, new IntegrationXlsxParseOptions("Materials"));
        Assert.True(parsed.Success);
        Assert.Equal(["code", "name"], parsed.Headers);
        Assert.Equal(2, parsed.Rows.Count);
    }
}

public sealed class IntegrationContentFingerprintTests
{
    [Fact]
    public void SamePayloadProducesStableFingerprint()
    {
        var values = new Dictionary<string, string?> { ["code"] = "MAT-1", ["name"] = "Test" };
        var first = IntegrationContentFingerprint.Compute(values);
        var second = IntegrationContentFingerprint.Compute(values);
        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentPayloadProducesDifferentFingerprint()
    {
        var first = IntegrationContentFingerprint.Compute(
            new Dictionary<string, string?> { ["code"] = "MAT-1", ["name"] = "A" });
        var second = IntegrationContentFingerprint.Compute(
            new Dictionary<string, string?> { ["code"] = "MAT-1", ["name"] = "B" });
        Assert.NotEqual(first, second);
    }
}

using System.Text.Json;

using System.Globalization;

namespace NevaxisFactOS.Persistence.BulkDataOnboarding;

public static class BulkDataOnboardingSyntheticFixture
{
    public const string SourceSystem = "SYNTHETIC-FACTORY-01";
    public const string SourceDatabase = "synthetic_factory";
    public const string SourceVersion = "1.0.0";

    public static BulkSourceDiscoveryResult BuildDiscovery()
    {
        var tables = new[]
        {
            BuildTable("areas", BulkSchemaRoleHints.LinesStations, ["area_code", "area_name"], 3),
            BuildTable("lines", BulkSchemaRoleHints.LinesStations, ["line_code", "line_name", "area_code"], 6),
            BuildTable("stations", BulkSchemaRoleHints.LinesStations, ["station_code", "station_name", "line_code", "sequence_order"], 24),
            BuildTable("resources", BulkSchemaRoleHints.MachinesResources, ["resource_code", "resource_name", "station_code", "resource_type", "machine_registry_code"], 48),
            BuildTable("materials", BulkSchemaRoleHints.Materials, ["material_code", "material_name", "unit_of_measure"], 120),
            BuildTable("orders", BulkSchemaRoleHints.ProductionOrders, ["order_code", "product_code", "line_code", "quantity"], 200),
            BuildTable("events", BulkSchemaRoleHints.EventsHistory, ["event_id", "resource_code", "event_type", "occurred_at", "good_qty"], 2500),
            BuildTable("quality", BulkSchemaRoleHints.Quality, ["quality_id", "order_code", "defect_code", "recorded_at"], 180),
            BuildTable("maintenance", BulkSchemaRoleHints.Maintenance, ["maintenance_id", "resource_code", "work_type", "scheduled_at"], 90),
            BuildTable("molds", BulkSchemaRoleHints.MoldsTools, ["mold_code", "mold_name", "cavity_count"], 40),
        };

        return new BulkSourceDiscoveryResult(SourceSystem, SourceVersion, tables);
    }

    public static IReadOnlyList<BulkMappedRecord> BuildMappedRecords(BulkMappingProfileDefinition mapping)
    {
        var now = DateTimeOffset.UtcNow;
        var records = new List<BulkMappedRecord>();

        for (var area = 1; area <= 3; area++)
        {
            records.Add(Record("areas", $"AREA-{area:D2}", BulkCanonicalEntityTypes.ManufacturingArea, new Dictionary<string, string?>
            {
                ["code"] = $"AREA-{area:D2}",
                ["name"] = $"Area {area}",
            }, now));
        }

        for (var line = 1; line <= 6; line++)
        {
            var area = ((line - 1) / 2) + 1;
            records.Add(Record("lines", $"LINE-{line:D2}", BulkCanonicalEntityTypes.ProductionLine, new Dictionary<string, string?>
            {
                ["code"] = $"LINE-{line:D2}",
                ["name"] = $"Line {line}",
                ["area_code"] = $"AREA-{area:D2}",
            }, now));
        }

        for (var station = 1; station <= 24; station++)
        {
            var line = ((station - 1) / 4) + 1;
            records.Add(Record("stations", $"ST-{station:D3}", BulkCanonicalEntityTypes.ProductionStation, new Dictionary<string, string?>
            {
                ["code"] = $"ST-{station:D3}",
                ["name"] = $"Station {station}",
                ["line_code"] = $"LINE-{line:D2}",
                ["sequence_order"] = station.ToString(CultureInfo.InvariantCulture),
                ["station_kind"] = station % 4 == 0 ? "quality" : "process",
            }, now));
        }

        for (var resource = 1; resource <= 48; resource++)
        {
            var station = ((resource - 1) / 2) + 1;
            var resourceType = resource % 5 == 0 ? "human" : resource % 3 == 0 ? "buffer" : "machine";
            records.Add(Record("resources", $"RES-{resource:D3}", BulkCanonicalEntityTypes.ManufacturingResource, new Dictionary<string, string?>
            {
                ["code"] = $"RES-{resource:D3}",
                ["name"] = $"Resource {resource}",
                ["station_code"] = $"ST-{station:D3}",
                ["resource_type"] = resourceType,
                ["machine_registry_code"] = resourceType == "machine" ? $"MCH-{resource:D3}" : null,
            }, now));
        }

        for (var material = 1; material <= 120; material++)
        {
            records.Add(Record("materials", $"MAT-{material:D4}", BulkCanonicalEntityTypes.Material, new Dictionary<string, string?>
            {
                ["code"] = $"MAT-{material:D4}",
                ["name"] = $"Material {material}",
                ["unit_of_measure"] = "kg",
            }, now));
        }

        for (var order = 1; order <= 200; order++)
        {
            var line = (order % 6) + 1;
            records.Add(Record("orders", $"ORD-{order:D5}", BulkCanonicalEntityTypes.ProductionOrder, new Dictionary<string, string?>
            {
                ["order_code"] = $"ORD-{order:D5}",
                ["product_code"] = $"PRD-{order % 40 + 1:D3}",
                ["line_code"] = $"LINE-{line:D2}",
                ["quantity"] = (100 + order).ToString(CultureInfo.InvariantCulture),
            }, now));
        }

        for (var eventIndex = 1; eventIndex <= 2500; eventIndex++)
        {
            var resource = (eventIndex % 48) + 1;
            var occurredAt = now.AddMinutes(-eventIndex);
            records.Add(Record("events", $"EVT-{eventIndex:D5}", BulkCanonicalEntityTypes.ProductionEvent, new Dictionary<string, string?>
            {
                ["resource_code"] = $"RES-{resource:D3}",
                ["event_type"] = eventIndex % 17 == 0 ? "REJECT" : "GOOD",
                ["good_qty"] = (eventIndex % 17 == 0 ? 0 : 1).ToString(CultureInfo.InvariantCulture),
                ["occurred_at"] = occurredAt.ToString("O"),
            }, occurredAt));
        }

        for (var quality = 1; quality <= 180; quality++)
        {
            records.Add(Record("quality", $"QC-{quality:D4}", BulkCanonicalEntityTypes.QualityRecord, new Dictionary<string, string?>
            {
                ["order_code"] = $"ORD-{quality:D5}",
                ["defect_code"] = quality % 11 == 0 ? "INVALID" : $"DEF-{quality % 20:D2}",
                ["recorded_at"] = now.AddHours(-quality).ToString("O"),
            }, now.AddHours(-quality)));
        }

        for (var maintenance = 1; maintenance <= 90; maintenance++)
        {
            records.Add(Record("maintenance", $"MNT-{maintenance:D3}", BulkCanonicalEntityTypes.MaintenanceRecord, new Dictionary<string, string?>
            {
                ["resource_code"] = $"RES-{maintenance:D3}",
                ["work_type"] = maintenance % 4 == 0 ? "INVALID" : "preventive",
                ["scheduled_at"] = now.AddDays(-maintenance).ToString("O"),
            }, now.AddDays(-maintenance)));
        }

        for (var mold = 1; mold <= 40; mold++)
        {
            records.Add(Record("molds", $"MOLD-{mold:D3}", BulkCanonicalEntityTypes.MoldTool, new Dictionary<string, string?>
            {
                ["mold_code"] = $"MOLD-{mold:D3}",
                ["mold_name"] = $"Mold {mold}",
                ["cavity_count"] = (mold % 8 + 1).ToString(CultureInfo.InvariantCulture),
            }, now));
        }

        records.Add(Record("resources", "RES-DUP-001", BulkCanonicalEntityTypes.ManufacturingResource, new Dictionary<string, string?>
        {
            ["code"] = "RES-001",
            ["name"] = "Duplicate Resource",
            ["station_code"] = "ST-001",
            ["resource_type"] = "machine",
        }, now));

        records.Add(Record("orders", "ORD-BAD-001", BulkCanonicalEntityTypes.ProductionOrder, new Dictionary<string, string?>
        {
            ["order_code"] = string.Empty,
            ["product_code"] = "PRD-001",
            ["line_code"] = "LINE-99",
            ["quantity"] = "not-a-number",
        }, now));

        records.Add(Record("events", "EVT-FUTURE", BulkCanonicalEntityTypes.ProductionEvent, new Dictionary<string, string?>
        {
            ["resource_code"] = "RES-001",
            ["event_type"] = "GOOD",
            ["good_qty"] = "1",
            ["occurred_at"] = now.AddYears(2).ToString("O"),
        }, now.AddYears(2)));

        return records;
    }

    public static BulkMappingProfileDefinition BuildDefaultMapping() =>
        new(SourceSystem, SourceVersion, []);

    private static BulkSourceTableDescriptor BuildTable(
        string table,
        string role,
        string[] columns,
        long rowCount) =>
        new(
            SourceDatabase,
            "public",
            table,
            columns.Select(c => new BulkColumnDescriptor(
                c,
                c.Contains("at", StringComparison.OrdinalIgnoreCase) ? "timestamp" : "text",
                false,
                c.EndsWith("_code", StringComparison.OrdinalIgnoreCase) || c == "event_id",
                null)).ToList(),
            rowCount,
            [columns[0]],
            columns.Where(c => c.Contains("at", StringComparison.OrdinalIgnoreCase)).ToList());

    private static BulkMappedRecord Record(
        string table,
        string key,
        string entityType,
        Dictionary<string, string?> fields,
        DateTimeOffset timestamp) =>
        new(
            table,
            key,
            entityType,
            fields,
            timestamp.ToString("O"),
            timestamp.ToUniversalTime(),
            DateTimeOffset.UtcNow);
}

namespace NevaxisZentra.Persistence.Integration.FileImport;

public sealed record IntegrationFileTable(
    string FileKind,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows,
    IReadOnlyList<string>? SheetNames = null);

public static class IntegrationFileTableReader
{
    public static IntegrationFileTable? Read(
        string fileName,
        Stream content,
        string? delimiter = null,
        bool? hasHeaderRow = true,
        string? encodingName = null,
        string? sheetName = null,
        int maxRows = IntegrationFileIngestionLimits.MaxImportRows)
    {
        var extension = Path.GetExtension(fileName);
        if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
        {
            var delimiterChar = string.IsNullOrEmpty(delimiter) ? ',' : delimiter[0];
            var parseResult = IntegrationCsvParser.Parse(
                content,
                new IntegrationCsvParseOptions(delimiterChar, hasHeaderRow ?? true, encodingName),
                maxRows);
            if (!parseResult.Success)
            {
                return null;
            }

            return new IntegrationFileTable(
                IntegrationFileKinds.Csv,
                parseResult.Headers,
                ToRowDictionaries(parseResult.Headers, parseResult.Rows),
                null);
        }

        if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var workbookInfo = IntegrationXlsxParser.ListSheets(content);
            content.Position = 0;
            var parseResult = IntegrationXlsxParser.Parse(
                content,
                new IntegrationXlsxParseOptions(sheetName, hasHeaderRow ?? true, maxRows));
            if (!parseResult.Success)
            {
                return null;
            }

            return new IntegrationFileTable(
                IntegrationFileKinds.Xlsx,
                parseResult.Headers,
                ToRowDictionaries(parseResult.Headers, parseResult.Rows),
                workbookInfo.Sheets.Select(s => s.Name).ToList());
        }

        return null;
    }

    private static List<IReadOnlyDictionary<string, string>> ToRowDictionaries(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        return rows.Select(row =>
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Count; index++)
            {
                dict[headers[index]] = index < row.Count ? row[index] : string.Empty;
            }

            return (IReadOnlyDictionary<string, string>)dict;
        }).ToList();
    }
}

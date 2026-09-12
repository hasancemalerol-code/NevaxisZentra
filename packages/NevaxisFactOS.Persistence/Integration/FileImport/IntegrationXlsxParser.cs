using System.Globalization;

namespace NevaxisFactOS.Persistence.Integration.FileImport;

public sealed record IntegrationXlsxParseOptions(
    string? SheetName = null,
    bool HasHeaderRow = true,
    int MaxRows = IntegrationFileIngestionLimits.MaxPreviewRows,
    int MaxColumns = IntegrationFileIngestionLimits.MaxColumns);

public sealed record IntegrationXlsxSheetInfo(string Name, int RowCount, int ColumnCount);

public sealed record IntegrationXlsxWorkbookInfo(
    IReadOnlyList<IntegrationXlsxSheetInfo> Sheets);

public static class IntegrationXlsxParser
{
    public static IntegrationXlsxWorkbookInfo ListSheets(Stream content)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook(content);
        var sheets = workbook.Worksheets
            .Select(sheet => new IntegrationXlsxSheetInfo(
                sheet.Name,
                sheet.LastRowUsed()?.RowNumber() ?? 0,
                sheet.LastColumnUsed()?.ColumnNumber() ?? 0))
            .ToList();
        return new IntegrationXlsxWorkbookInfo(sheets);
    }

    public static IntegrationCsvParseResult Parse(
        Stream content,
        IntegrationXlsxParseOptions? options = null)
    {
        options ??= new IntegrationXlsxParseOptions();
        using var workbook = new ClosedXML.Excel.XLWorkbook(content);
        var worksheet = options.SheetName is null
            ? workbook.Worksheets.First()
            : workbook.Worksheet(options.SheetName);

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastRow == 0 || lastColumn == 0)
        {
            return IntegrationCsvParseResult.Fail("Worksheet contains no data.");
        }

        lastColumn = Math.Min(lastColumn, options.MaxColumns);
        var maxDataRow = Math.Min(lastRow, options.MaxRows + (options.HasHeaderRow ? 1 : 0));

        var rows = new List<IReadOnlyList<string>>();
        for (var rowIndex = 1; rowIndex <= maxDataRow; rowIndex++)
        {
            var values = new List<string>();
            for (var columnIndex = 1; columnIndex <= lastColumn; columnIndex++)
            {
                var cell = worksheet.Cell(rowIndex, columnIndex);
                values.Add(ReadCellValue(cell));
            }

            if (values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(values);
        }

        if (rows.Count == 0)
        {
            return IntegrationCsvParseResult.Fail("Worksheet contains no readable rows.");
        }

        if (options.HasHeaderRow)
        {
            return IntegrationCsvParseResult.Ok(rows[0], rows.Skip(1).ToList());
        }

        var syntheticHeaders = Enumerable.Range(1, rows[0].Count)
            .Select(index => $"column_{index}")
            .ToList();
        return IntegrationCsvParseResult.Ok(syntheticHeaders, rows);
    }

    private static string ReadCellValue(ClosedXML.Excel.IXLCell cell)
    {
        if (cell.HasFormula)
        {
            if (cell.Value.IsBlank)
            {
                return string.Empty;
            }

            return cell.CachedValue.ToString(CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        }

        return cell.GetFormattedString().Trim();
    }
}

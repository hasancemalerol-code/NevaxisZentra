using System.Text;

namespace NevaxisZentra.Persistence.Integration.FileImport;

public sealed record IntegrationCsvParseOptions(
    char Delimiter = ',',
    bool HasHeaderRow = true,
    string? EncodingName = "utf-8");

public sealed record IntegrationCsvParseResult(
    bool Success,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    string? ErrorMessage)
{
    public static IntegrationCsvParseResult Fail(string message) =>
        new(false, [], [], message);

    public static IntegrationCsvParseResult Ok(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows) =>
        new(true, headers, rows, null);
}

public static class IntegrationCsvParser
{
    public static IntegrationCsvParseResult Parse(
        Stream content,
        IntegrationCsvParseOptions? options = null,
        int maxRows = IntegrationFileIngestionLimits.MaxPreviewRows)
    {
        options ??= new IntegrationCsvParseOptions();
        var encoding = ResolveEncoding(options.EncodingName);
        using var reader = new StreamReader(content, encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var rows = new List<IReadOnlyList<string>>();
        string? line;
        while (rows.Count < maxRows && (line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            rows.Add(ParseLine(line, options.Delimiter));
        }

        if (rows.Count == 0)
        {
            return IntegrationCsvParseResult.Fail("File contains no readable rows.");
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

    private static Encoding ResolveEncoding(string? encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName))
        {
            return Encoding.UTF8;
        }

        try
        {
            return Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    private static List<string> ParseLine(string line, char delimiter)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (character == delimiter && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        values.Add(current.ToString().Trim());
        return values;
    }
}

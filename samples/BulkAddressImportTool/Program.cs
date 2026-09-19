using System.Globalization;
using ISOCodex.Countries;
using System.Text;
using System.Text.Json;
using ISOCodex.Addressing;
using ISOCodex.Addressing.Formatting;
using ISOCodex.Addressing.France;
using ISOCodex.Addressing.GreatBritain;
using ISOCodex.Addressing.Ireland;
using ISOCodex.Addressing.Spain;
using ISOCodex.Addressing.Validation;
using Microsoft.Extensions.DependencyInjection;

var inputPath = args.Length > 0
    ? ResolveInputPath(args[0])
    : Path.Combine(AppContext.BaseDirectory, "SampleData", "addresses.csv");

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input CSV was not found: {inputPath}");
    return 1;
}

var outputDirectory = Path.Combine(AppContext.BaseDirectory, "Output");
Directory.CreateDirectory(outputDirectory);

var services = new ServiceCollection();
services
    .AddAddressing()
    .AddGreatBritainAddressing()
    .AddIrelandAddressing()
    .AddFranceAddressing()
    .AddSpainAddressing()
    .AddGenericAddressingFallbacks();

var serviceProvider = services.BuildServiceProvider();
var validatorFactory = serviceProvider.GetRequiredService<IAddressValidatorFactory>();
var formatter = serviceProvider.GetRequiredService<IAddressFormatter>();

var rows = ReadCsv(inputPath);
var results = rows.Select(row => ProcessRow(row, validatorFactory, formatter)).ToArray();

var jsonPath = Path.Combine(outputDirectory, "import-results.json");
var csvPath = Path.Combine(outputDirectory, "import-results.csv");
var summaryPath = Path.Combine(outputDirectory, "import-summary.txt");

File.WriteAllText(jsonPath, JsonSerializer.Serialize(results, new JsonSerializerOptions
{
    WriteIndented = true
}));

WriteResultsCsv(csvPath, results);
WriteSummary(summaryPath, inputPath, results);

Console.WriteLine(File.ReadAllText(summaryPath));
Console.WriteLine($"Wrote {jsonPath}");
Console.WriteLine($"Wrote {csvPath}");

return 0;

static string ResolveInputPath(string value)
{
    if (Path.IsPathRooted(value))
    {
        return value;
    }

    var cwdPath = Path.GetFullPath(value);
    if (File.Exists(cwdPath))
    {
        return cwdPath;
    }

    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, value));
}

static ImportResult ProcessRow(
    CsvRow row,
    IAddressValidatorFactory validatorFactory,
    IAddressFormatter formatter)
{
    if (row.ImportError is not null)
    {
        return new ImportResult(
            row.RowNumber,
            row.Raw,
            null,
            null,
            "NotValidated",
            [new PersistedIssue("Import.RowCouldNotBeMapped", null, row.ImportError)],
            null);
    }

    var issues = new List<PersistedIssue>();
    var countryText = Get(row.Values, "countryCode")?.Trim();
    if (!CountryAlpha2Code.TryParse(countryText ?? string.Empty, out var country))
    {
        issues.Add(new PersistedIssue("CountryCode.Invalid", "countryCode", "Country code must be an ISO 3166-1 alpha-2 code."));
    }

    var line1 = Get(row.Values, "line1")?.Trim();
    var city = Get(row.Values, "city")?.Trim();
    var postalCode = Get(row.Values, "postalCode")?.Trim();

    if (string.IsNullOrWhiteSpace(line1))
    {
        issues.Add(new PersistedIssue("AddressLine1.Required", "Line1", "Address line 1 is required."));
    }

    if (string.IsNullOrWhiteSpace(city))
    {
        issues.Add(new PersistedIssue("Locality.Required", "City", "City or locality is required."));
    }

    if (string.IsNullOrWhiteSpace(postalCode))
    {
        issues.Add(new PersistedIssue("PostalCode.Required", "PostalCode", "Postal code is required."));
    }

    if (issues.Count > 0)
    {
        return new ImportResult(row.RowNumber, row.Raw, countryText, null, "Invalid", issues, null);
    }

    var address = new Address(
        line1!,
        TrimToNull(Get(row.Values, "line2")),
        city!,
        TrimToNull(Get(row.Values, "stateOrProvince")),
        new PostalCode(postalCode!),
        country);

    var validation = validatorFactory.GetValidator(address.CountryCode).Validate(address);
    var validationIssues = validation.Issues
        .Select(issue => new PersistedIssue(issue.Code, issue.PropertyName, issue.Message))
        .ToArray();

    var status = validationIssues.Length > 0
        ? "Invalid"
        : IsCountrySpecific(country) ? "Valid" : "AcceptedUnverified";

    var formatted = validationIssues.Length == 0
        ? formatter.Format(address, new AddressFormatOptions { Style = AddressFormatStyle.SingleLine })
        : null;

    return new ImportResult(
        row.RowNumber,
        row.Raw,
        country.Value,
        ToPersistedAddress(address),
        status,
        validationIssues,
        formatted);
}

static IReadOnlyList<CsvRow> ReadCsv(string inputPath)
{
    using var reader = new StreamReader(inputPath);
    var headerLine = reader.ReadLine();
    if (headerLine is null)
    {
        return Array.Empty<CsvRow>();
    }

    var headers = ParseCsvLine(headerLine).Select(header => header.Trim()).ToArray();
    var rows = new List<CsvRow>();
    var rowNumber = 1;
    while (!reader.EndOfStream)
    {
        rowNumber++;
        var raw = reader.ReadLine() ?? string.Empty;
        try
        {
            var values = ParseCsvLine(raw);
            if (values.Count != headers.Length)
            {
                rows.Add(new CsvRow(rowNumber, raw, new Dictionary<string, string>(), $"Expected {headers.Length} columns but found {values.Count}."));
                continue;
            }

            rows.Add(new CsvRow(
                rowNumber,
                raw,
                headers.Zip(values, (header, value) => new { header, value })
                    .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase),
                null));
        }
        catch (FormatException ex)
        {
            rows.Add(new CsvRow(rowNumber, raw, new Dictionary<string, string>(), ex.Message));
        }
    }

    return rows;
}

static IReadOnlyList<string> ParseCsvLine(string line)
{
    var values = new List<string>();
    var current = new StringBuilder();
    var inQuotes = false;

    for (var i = 0; i < line.Length; i++)
    {
        var c = line[i];
        if (c == '"')
        {
            if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Append('"');
                i++;
            }
            else
            {
                inQuotes = !inQuotes;
            }
        }
        else if (c == ',' && !inQuotes)
        {
            values.Add(current.ToString());
            current.Clear();
        }
        else
        {
            current.Append(c);
        }
    }

    if (inQuotes)
    {
        throw new FormatException("CSV row has an unterminated quoted field.");
    }

    values.Add(current.ToString());
    return values;
}

static void WriteResultsCsv(string path, IReadOnlyList<ImportResult> results)
{
    var lines = new List<string>
    {
        "rowNumber,countryCode,status,issueCodes,formattedSingleLine"
    };

    lines.AddRange(results.Select(result => string.Join(
        ",",
        Csv(result.RowNumber.ToString(CultureInfo.InvariantCulture)),
        Csv(result.CountryAlpha2Code ?? string.Empty),
        Csv(result.Status),
        Csv(string.Join("|", result.Issues.Select(issue => issue.Code))),
        Csv(result.FormattedSingleLine ?? string.Empty))));

    File.WriteAllLines(path, lines);
}

static void WriteSummary(string path, string inputPath, IReadOnlyList<ImportResult> results)
{
    var summary = new StringBuilder();
    summary.AppendLine("Bulk address import summary");
    summary.AppendLine($"Input: {inputPath}");
    summary.AppendLine($"Rows processed: {results.Count}");

    foreach (var group in results.GroupBy(result => result.Status).OrderBy(group => group.Key))
    {
        summary.AppendLine($"{group.Key}: {group.Count()}");
    }

    summary.AppendLine();
    summary.AppendLine("Rows needing review:");
    foreach (var result in results.Where(result => result.Status != "Valid"))
    {
        summary.AppendLine($"- Row {result.RowNumber}: {result.Status} ({string.Join(", ", result.Issues.Select(issue => issue.Code))})");
    }

    File.WriteAllText(path, summary.ToString());
}

static bool IsCountrySpecific(CountryAlpha2Code country)
{
    return country == CountryAlpha2Code.Parse("GB")
        || country == CountryAlpha2Code.Parse("IE")
        || country == CountryAlpha2Code.Parse("FR")
        || country == CountryAlpha2Code.Parse("ES");
}

static PersistedAddress ToPersistedAddress(Address address)
{
    return new PersistedAddress(
        address.Line1,
        address.Line2,
        address.City,
        address.StateOrProvince,
        address.PostalCode.Code,
        address.CountryCode.Value);
}

static string? Get(IReadOnlyDictionary<string, string> values, string key)
{
    return values.TryGetValue(key, out var value) ? value : null;
}

static string? TrimToNull(string? value)
{
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

static string Csv(string value)
{
    return value.Contains(',') || value.Contains('"') || value.Contains('\n')
        ? $"\"{value.Replace("\"", "\"\"")}\""
        : value;
}

public sealed record CsvRow(
    int RowNumber,
    string Raw,
    IReadOnlyDictionary<string, string> Values,
    string? ImportError);

public sealed record PersistedAddress(
    string Line1,
    string? Line2,
    string City,
    string? StateOrProvince,
    string PostalCode,
    string CountryAlpha2Code);

public sealed record PersistedIssue(
    string Code,
    string? PropertyName,
    string Message);

public sealed record ImportResult(
    int RowNumber,
    string Raw,
    string? CountryAlpha2Code,
    PersistedAddress? Address,
    string Status,
    IReadOnlyList<PersistedIssue> Issues,
    string? FormattedSingleLine);

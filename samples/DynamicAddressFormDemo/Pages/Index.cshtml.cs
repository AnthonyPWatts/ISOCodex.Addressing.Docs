using System.Text.Encodings.Web;
using System.Text.Json;
using ISOCodex.Countries;
using ISOCodex.Addressing;
using ISOCodex.Addressing.Formatting;
using ISOCodex.Addressing.Profiles;
using ISOCodex.Addressing.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DynamicAddressFormDemo.Pages;

public class IndexModel : PageModel
{
    private static readonly IReadOnlyList<CountryChoice> CountryChoices =
    [
        new("ES", "Spain"),
        new("FR", "France"),
        new("IE", "Ireland")
    ];

    private static readonly IReadOnlyList<SampleChoice> SampleChoices =
    [
        new("es-valid", "Spain valid", "ES", "Calle Mayor 10", "3 C", "Madrid", "Madrid", "28013"),
        new("es-invalid-postal", "Spain invalid postal code", "ES", "Calle Mayor 10", "", "Madrid", "Madrid", "ABC"),
        new("fr-valid", "France valid", "FR", "10 Rue de Rivoli", "", "Paris", "", "75001"),
        new("fr-missing-city", "France missing city", "FR", "10 Rue de Rivoli", "", "", "", "75001"),
        new("ie-valid", "Ireland valid", "IE", "1 College Green", "", "Dublin", "Dublin", "D02 X285"),
        new("ie-invalid-eircode", "Ireland invalid Eircode", "IE", "1 College Green", "", "Dublin", "Dublin", "BAD CODE")
    ];

    private readonly IAddressProfileProvider _profileProvider;
    private readonly IAddressValidatorFactory _validatorFactory;
    private readonly IAddressFormatter _formatter;

    public IndexModel(
        IAddressProfileProvider profileProvider,
        IAddressValidatorFactory validatorFactory,
        IAddressFormatter formatter)
    {
        _profileProvider = profileProvider;
        _validatorFactory = validatorFactory;
        _formatter = formatter;
    }

    [BindProperty(SupportsGet = true)]
    public string CountryCode { get; set; } = "ES";

    [BindProperty]
    public AddressFormInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SampleId { get; set; }

    public IReadOnlyList<CountryChoice> Countries => CountryChoices;

    public IReadOnlyList<SampleChoice> Samples => SampleChoices;

    public AddressProfile Profile { get; private set; } = default!;

    public string RawProfileJson { get; private set; } = string.Empty;

    public string? FormattedPreview { get; private set; }

    public IReadOnlyList<FieldIssue> FieldIssues { get; private set; } = Array.Empty<FieldIssue>();

    public string Status { get; private set; } = "Not validated";

    public string CountryName => CountryChoices.First(country => country.Code == CountryCode).Name;

    public void OnGet()
    {
        CountryCode = NormalizeCountry(CountryCode);
        var sample = SampleChoices.FirstOrDefault(candidate =>
            candidate.Id == SampleId && candidate.CountryCode == CountryCode);

        if (sample is null)
        {
            SampleId = null;
            Input = new AddressFormInput { CountryCode = CountryCode };
        }
        else
        {
            Input = LoadSample(sample);
        }

        Input.CountryCode = CountryCode;
        LoadProfile();

        if (sample is not null)
        {
            ValidateInput();
        }
    }

    public void OnPost()
    {
        CountryCode = NormalizeCountry(Input.CountryCode);
        Input.CountryCode = CountryCode;
        LoadProfile();

        ValidateInput();
    }

    private void ValidateInput()
    {
        var issues = MapConstructionIssues(Input);
        if (issues.Count > 0)
        {
            Status = "Invalid";
            FieldIssues = issues;
            return;
        }

        var address = new Address(
            Input.Line1!.Trim(),
            TrimToNull(Input.Line2),
            Input.City!.Trim(),
            TrimToNull(Input.StateOrProvince),
            new PostalCode(Input.PostalCode!.Trim()),
            CountryAlpha2Code.Parse(CountryCode));

        var result = _validatorFactory.GetValidator(address.CountryCode).Validate(address);
        Status = result.IsValid ? "Valid" : "Invalid";
        FieldIssues = result.Issues
            .Select(issue => new FieldIssue(ToInputField(issue.PropertyName), issue.Code, issue.Message))
            .ToArray();

        if (result.IsValid)
        {
            FormattedPreview = _formatter.Format(address);
        }
    }

    public string IssueTextFor(string field)
    {
        return string.Join(" ", FieldIssues.Where(issue => issue.Field == field).Select(issue => issue.Message));
    }

    public string FieldLabelFor(string field)
    {
        return Profile.Fields
            .FirstOrDefault(profileField => ToInputField(profileField.Field) == field)
            ?.Label ?? field;
    }

    private void LoadProfile()
    {
        Profile = _profileProvider.GetProfile(CountryAlpha2Code.Parse(CountryCode));
        RawProfileJson = JsonSerializer.Serialize(Profile, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        });
    }

    private static AddressFormInput LoadSample(SampleChoice sample)
    {
        return new AddressFormInput
        {
            CountryCode = sample.CountryCode,
            Line1 = sample.Line1,
            Line2 = sample.Line2,
            City = sample.City,
            StateOrProvince = sample.StateOrProvince,
            PostalCode = sample.PostalCode
        };
    }

    private static string NormalizeCountry(string? countryCode)
    {
        var normalized = countryCode?.ToUpperInvariant();
        return CountryChoices.Any(country => country.Code == normalized) ? normalized! : "ES";
    }

    private List<FieldIssue> MapConstructionIssues(AddressFormInput input)
    {
        var issues = new List<FieldIssue>();
        var requiredFields = Profile.Fields
            .Where(field => field.IsUsed && field.IsRequired)
            .Select(field => ToInputField(field.Field))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (requiredFields.Contains(nameof(AddressFormInput.Line1)) && string.IsNullOrWhiteSpace(input.Line1))
        {
            issues.Add(new FieldIssue(nameof(AddressFormInput.Line1), "AddressLine1.Required", "Address line 1 is required."));
        }

        if (requiredFields.Contains(nameof(AddressFormInput.City)) && string.IsNullOrWhiteSpace(input.City))
        {
            issues.Add(new FieldIssue(nameof(AddressFormInput.City), "Locality.Required", "City or locality is required."));
        }

        if (requiredFields.Contains(nameof(AddressFormInput.PostalCode)) && string.IsNullOrWhiteSpace(input.PostalCode))
        {
            issues.Add(new FieldIssue(nameof(AddressFormInput.PostalCode), "PostalCode.Required", "Postal code is required."));
        }

        if (requiredFields.Contains(nameof(AddressFormInput.StateOrProvince)) && string.IsNullOrWhiteSpace(input.StateOrProvince))
        {
            issues.Add(new FieldIssue(nameof(AddressFormInput.StateOrProvince), "AdministrativeArea.Required", "Administrative area is required."));
        }

        return issues;
    }

    private static string ToInputField(AddressField field)
    {
        return field switch
        {
            AddressField.AddressLine1 => nameof(AddressFormInput.Line1),
            AddressField.AddressLine2 => nameof(AddressFormInput.Line2),
            AddressField.Locality => nameof(AddressFormInput.City),
            AddressField.AdministrativeArea => nameof(AddressFormInput.StateOrProvince),
            AddressField.PostalCode => nameof(AddressFormInput.PostalCode),
            AddressField.Country => nameof(AddressFormInput.CountryCode),
            _ => field.ToString()
        };
    }

    private static string ToInputField(string? propertyName)
    {
        return propertyName switch
        {
            nameof(Address.Line1) => nameof(AddressFormInput.Line1),
            nameof(Address.Line2) => nameof(AddressFormInput.Line2),
            nameof(Address.City) => nameof(AddressFormInput.City),
            nameof(Address.StateOrProvince) => nameof(AddressFormInput.StateOrProvince),
            nameof(Address.PostalCode) => nameof(AddressFormInput.PostalCode),
            nameof(Address.CountryCode) => nameof(AddressFormInput.CountryCode),
            _ => propertyName ?? string.Empty
        };
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class AddressFormInput
{
    public string CountryCode { get; set; } = "ES";

    public string? Line1 { get; set; }

    public string? Line2 { get; set; }

    public string? City { get; set; }

    public string? StateOrProvince { get; set; }

    public string? PostalCode { get; set; }
}

public sealed record CountryChoice(string Code, string Name);

public sealed record SampleChoice(
    string Id,
    string Name,
    string CountryCode,
    string Line1,
    string Line2,
    string City,
    string StateOrProvince,
    string PostalCode);

public sealed record FieldIssue(string Field, string Code, string Message);

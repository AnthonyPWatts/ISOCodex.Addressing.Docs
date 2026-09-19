using ISOCodex.Addressing;
using ISOCodex.Countries;
using ISOCodex.Addressing.Canada;
using ISOCodex.Addressing.Formatting;
using ISOCodex.Addressing.France;
using ISOCodex.Addressing.GreatBritain;
using ISOCodex.Addressing.Ireland;
using ISOCodex.Addressing.Profiles;
using ISOCodex.Addressing.Spain;
using ISOCodex.Addressing.UnitedStates;
using ISOCodex.Addressing.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAddressing()
    .AddGreatBritainAddressing()
    .AddUnitedStatesAddressing()
    .AddCanadaAddressing()
    .AddSpainAddressing()
    .AddFranceAddressing()
    .AddIrelandAddressing();

var app = builder.Build();

var supportedCountries = new[]
{
    new SupportedCountry("GB", "United Kingdom"),
    new SupportedCountry("US", "United States"),
    new SupportedCountry("CA", "Canada"),
    new SupportedCountry("ES", "Spain"),
    new SupportedCountry("FR", "France"),
    new SupportedCountry("IE", "Ireland")
};

app.MapGet("/", () => Results.Redirect("/countries"));

app.MapGet("/countries", () => supportedCountries);

app.MapGet("/address-profile/{countryCode}", (
    string countryCode,
    IAddressProfileProvider profileProvider) =>
{
    if (!TryGetSupportedCountry(countryCode, supportedCountries, out var country, out var error))
    {
        return Results.BadRequest(error);
    }

    var profile = profileProvider.GetProfile(country);
    return Results.Ok(ToProfileResponse(profile));
});

app.MapPost("/checkout/validate-address", (
    AddressRequest request,
    IAddressValidatorFactory validatorFactory) =>
{
    var validation = ValidateRequest(request, supportedCountries, validatorFactory);
    return Results.Ok(new AddressValidationResponse(validation.Status, validation.Issues));
});

app.MapPost("/checkout/preview-address", (
    AddressRequest request,
    IAddressValidatorFactory validatorFactory,
    IAddressFormatter formatter) =>
{
    var validation = ValidateRequest(request, supportedCountries, validatorFactory);
    if (!validation.IsValid || validation.Address is null)
    {
        return Results.BadRequest(new AddressValidationResponse(validation.Status, validation.Issues));
    }

    return Results.Ok(new AddressPreviewResponse(
        validation.Status,
        formatter.Format(validation.Address),
        formatter.Format(validation.Address, new AddressFormatOptions
        {
            Style = AddressFormatStyle.SingleLine
        }),
        validation.Issues));
});

app.MapPost("/checkout/order", (
    CheckoutOrderRequest request,
    IAddressValidatorFactory validatorFactory,
    IAddressFormatter formatter) =>
{
    var billing = ValidateRequest(request.BillingAddress, supportedCountries, validatorFactory, "billingAddress");
    var shipping = ValidateRequest(request.ShippingAddress, supportedCountries, validatorFactory, "shippingAddress");

    var issues = billing.Issues.Concat(shipping.Issues).ToArray();
    if (issues.Length > 0 || billing.Address is null || shipping.Address is null)
    {
        return Results.BadRequest(new OrderValidationResponse("Invalid", issues));
    }

    return Results.Ok(new OrderConfirmationResponse(
        $"POC-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
        request.CustomerEmail,
        formatter.Format(billing.Address),
        formatter.Format(shipping.Address),
        formatter.Format(shipping.Address, new AddressFormatOptions
        {
            Style = AddressFormatStyle.SingleLine
        })));
});

app.Run();

static AddressValidationOutcome ValidateRequest(
    AddressRequest request,
    SupportedCountry[] supportedCountries,
    IAddressValidatorFactory validatorFactory,
    string? prefix = null)
{
    if (!TryGetSupportedCountry(request.CountryCode, supportedCountries, out var country, out var countryError))
    {
        return new AddressValidationOutcome(
            "UnsupportedCountry",
            null,
            PrefixIssues(new[] { countryError }, prefix));
    }

    var mappingIssues = MapConstructionIssues(request, country);
    if (mappingIssues.Count > 0)
    {
        return new AddressValidationOutcome(
            "Invalid",
            null,
            PrefixIssues(mappingIssues, prefix));
    }

    var address = new Address(
        request.Line1!.Trim(),
        TrimToNull(request.Line2),
        request.City!.Trim(),
        TrimToNull(request.StateOrProvince),
        new PostalCode(request.PostalCode!.Trim()),
        country);

    var validationResult = validatorFactory.GetValidator(country).Validate(address);
    return new AddressValidationOutcome(
        validationResult.IsValid ? "Valid" : "Invalid",
        address,
        PrefixIssues(validationResult.Issues.Select(ToApiIssue), prefix).ToArray());
}

static bool TryGetSupportedCountry(
    string? value,
    SupportedCountry[] supportedCountries,
    out CountryAlpha2Code country,
    out ApiValidationIssue error)
{
    country = default;
    if (!CountryAlpha2Code.TryParse(value ?? string.Empty, out var parsed))
    {
        error = new ApiValidationIssue(
            "CountryCode.Invalid",
            "countryCode",
            "Country code must be an ISO 3166-1 alpha-2 code.");
        return false;
    }

    if (!supportedCountries.Any(c => c.Code == parsed.Value))
    {
        error = new ApiValidationIssue(
            "CountryCode.Unsupported",
            "countryCode",
            $"Country '{parsed.Value}' is not supported by this checkout service.");
        return false;
    }

    country = parsed;
    error = default!;
    return true;
}

static List<ApiValidationIssue> MapConstructionIssues(AddressRequest request, CountryAlpha2Code country)
{
    var issues = new List<ApiValidationIssue>();

    if (string.IsNullOrWhiteSpace(request.Line1))
    {
        issues.Add(new ApiValidationIssue("AddressLine1.Required", "line1", "Address line 1 is required."));
    }

    if (string.IsNullOrWhiteSpace(request.City))
    {
        issues.Add(new ApiValidationIssue("Locality.Required", "city", "City or locality is required."));
    }

    if (string.IsNullOrWhiteSpace(request.PostalCode))
    {
        issues.Add(new ApiValidationIssue("PostalCode.Required", "postalCode", "Postal code is required."));
    }

    if (country == CountryAlpha2Code.Parse("ES") && string.IsNullOrWhiteSpace(request.StateOrProvince))
    {
        issues.Add(new ApiValidationIssue("AdministrativeArea.Required", "stateOrProvince", "Province is required for Spanish addresses."));
    }

    return issues;
}

static ProfileResponse ToProfileResponse(AddressProfile profile)
{
    return new ProfileResponse(
        profile.CountryCode.Value,
        profile.Source.ToString(),
        profile.ExamplePostalCode,
        profile.ExampleFormattedAddress,
        profile.Fields
            .OrderBy(field => field.DisplayOrder)
            .Select(field => new FieldResponse(
                field.Field.ToString(),
                ToRequestFieldName(field.Field),
                field.Label,
                field.IsRequired,
                field.IsUsed,
                field.DisplayOrder,
                field.Placeholder,
                field.HelpText,
                field.InputKind.ToString(),
                field.Options.Select(option => new FieldOptionResponse(option.Value, option.Label)).ToArray()))
            .ToArray());
}

static string ToRequestFieldName(AddressField field)
{
    return field switch
    {
        AddressField.AddressLine1 => "line1",
        AddressField.AddressLine2 => "line2",
        AddressField.Locality => "city",
        AddressField.AdministrativeArea => "stateOrProvince",
        AddressField.PostalCode => "postalCode",
        AddressField.Country => "countryCode",
        _ => field.ToString()
    };
}

static ApiValidationIssue ToApiIssue(AddressValidationIssue issue)
{
    return new ApiValidationIssue(issue.Code, ToJsonFieldName(issue.PropertyName), issue.Message);
}

static string? ToJsonFieldName(string? propertyName)
{
    return propertyName switch
    {
        null => null,
        nameof(Address.Line1) => "line1",
        nameof(Address.Line2) => "line2",
        nameof(Address.City) => "city",
        nameof(Address.StateOrProvince) => "stateOrProvince",
        nameof(Address.PostalCode) => "postalCode",
        nameof(Address.CountryCode) => "countryCode",
        _ => char.ToLowerInvariant(propertyName[0]) + propertyName[1..]
    };
}

static ApiValidationIssue[] PrefixIssues(IEnumerable<ApiValidationIssue> issues, string? prefix)
{
    if (prefix is null)
    {
        return issues.ToArray();
    }

    return issues
        .Select(issue => issue with
        {
            Field = issue.Field is null ? prefix : $"{prefix}.{issue.Field}"
        })
        .ToArray();
}

static string? TrimToNull(string? value)
{
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record SupportedCountry(string Code, string Name);

public sealed record AddressRequest(
    string? Line1,
    string? Line2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode);

public sealed record CheckoutOrderRequest(
    string CustomerEmail,
    AddressRequest BillingAddress,
    AddressRequest ShippingAddress);

public sealed record ApiValidationIssue(string Code, string? Field, string Message);

public sealed record AddressValidationOutcome(
    string Status,
    Address? Address,
    IReadOnlyList<ApiValidationIssue> Issues)
{
    public bool IsValid => Status == "Valid" && Issues.Count == 0;
}

public sealed record AddressValidationResponse(
    string Status,
    IReadOnlyList<ApiValidationIssue> Issues);

public sealed record AddressPreviewResponse(
    string Status,
    string MultiLine,
    string SingleLine,
    IReadOnlyList<ApiValidationIssue> Issues);

public sealed record OrderValidationResponse(
    string Status,
    IReadOnlyList<ApiValidationIssue> Issues);

public sealed record OrderConfirmationResponse(
    string OrderId,
    string CustomerEmail,
    string BillingAddress,
    string ShippingAddress,
    string ShippingAddressSingleLine);

public sealed record ProfileResponse(
    string CountryCode,
    string Source,
    string? ExamplePostalCode,
    string? ExampleFormattedAddress,
    IReadOnlyList<FieldResponse> Fields);

public sealed record FieldResponse(
    string Field,
    string RequestFieldName,
    string Label,
    bool IsRequired,
    bool IsUsed,
    int DisplayOrder,
    string? Placeholder,
    string? HelpText,
    string InputKind,
    IReadOnlyList<FieldOptionResponse> Options);

public sealed record FieldOptionResponse(string Value, string Label);

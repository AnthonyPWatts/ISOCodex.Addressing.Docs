using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using ISOCodex.Addressing;
using ISOCodex.Addressing.Formatting;
using ISOCodex.Addressing.France;
using ISOCodex.Addressing.Ireland;
using ISOCodex.Addressing.Profiles;
using ISOCodex.Addressing.Spain;
using ISOCodex.Addressing.Validation;
using ISOCodex.Countries;
using ISOCodex.Currency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

internal static class Program
{
    private static int Main()
    {
        try
        {
            var target = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>();
            Console.WriteLine("Build target: " + target?.FrameworkName);
            Console.WriteLine("Installed Framework release key: " + Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", "Release", "unknown"));
            Console.WriteLine("CLR version: " + Environment.Version);
            Console.WriteLine("The build target does not identify the installed in-place Framework runtime.");
            Console.WriteLine("net462 is an exploratory check; supported consumers target net472 or later.");

            var services = new ServiceCollection();
            services.AddAddressing().AddSpainAddressing().AddFranceAddressing().AddIrelandAddressing();
            using (var provider = services.BuildServiceProvider())
            {
                var profiles = provider.GetRequiredService<IAddressProfileProvider>();
                var validators = provider.GetRequiredService<IAddressValidatorFactory>();
                var formatter = provider.GetRequiredService<IAddressFormatter>();

                CheckAddress(profiles, validators, formatter, "ES", "Calle Mayor 10", "Madrid", "Madrid",
                    "28013", "ABC", AddressFieldInputKind.Select);
                CheckAddress(profiles, validators, formatter, "FR", "10 Rue de Rivoli", "Paris", null,
                    "75001", "ABCDE", AddressFieldInputKind.Text);
                CheckAddress(profiles, validators, formatter, "IE", "1 College Green", "Dublin", "Dublin",
                    "D02 X285", "BAD CODE", AddressFieldInputKind.Text);
            }

            var options = new JsonSerializerOptions();
            options.Converters.Add(new CountryAlpha2CodeJsonConverter());
            var gb = CountryAlpha2Code.Parse("gb");
            Check(CountryRegistry.GetByAlpha2(gb).Alpha3.Value == "GBR", "Country lookup");
            Check(JsonSerializer.Serialize(gb, options) == "\"GB\"" &&
                JsonSerializer.Deserialize<CountryAlpha2Code>("\"gb\"", options) == gb, "Country JSON round trip");

            var total = Money.Of(12.34m, CurrencyCode.GBP) + Money.Of(5.66m, CurrencyCode.GBP);
            Check(total.Amount == 18m && total.Currency == CurrencyCode.GBP, "Money arithmetic");
            Check(!Money.TryCreate(1.001m, CurrencyCode.GBP).Succeeded, "Invalid money precision rejected");

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name!.StartsWith("ISOCodex.", StringComparison.Ordinal))
                .OrderBy(assembly => assembly.GetName().Name))
            {
                Console.WriteLine("Loaded: " + assembly.GetName().Name + " from " + assembly.Location);
            }

            Console.WriteLine("PASS: all 16 published-package checks completed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void CheckAddress(IAddressProfileProvider profiles, IAddressValidatorFactory validators,
        IAddressFormatter formatter, string code, string line1, string city, string? province,
        string validPostalCode, string invalidPostalCode, AddressFieldInputKind expectedInputKind)
    {
        var country = CountryAlpha2Code.Parse(code);
        var profile = profiles.GetProfile(country);
        Check(profile.Fields.Single(field => field.Field == AddressField.AdministrativeArea).InputKind == expectedInputKind,
            code + " profile metadata");

        var valid = new Address(line1, null, city, province, new PostalCode(validPostalCode), country);
        var invalid = new Address(line1, null, city, province, new PostalCode(invalidPostalCode), country);
        var validator = validators.GetValidator(country);
        Check(validator.Validate(valid).IsValid, code + " valid address");
        var invalidResult = validator.Validate(invalid);
        Check(!invalidResult.IsValid && invalidResult.Issues.Any(issue => issue.PropertyName == nameof(Address.PostalCode)),
            code + " invalid postal code rejected with a field issue");

        var formatted = formatter.Format(valid);
        Check(formatted.Contains(line1) && formatted.Contains(validPostalCode) &&
            formatted.Contains(CountryRegistry.GetByAlpha2(country).EnglishShortName), code + " formatted address");
        Console.WriteLine(formatted);
    }

    private static void Check(bool passed, string description)
    {
        if (!passed) throw new InvalidOperationException("FAIL: " + description);
        Console.WriteLine("PASS: " + description);
    }
}

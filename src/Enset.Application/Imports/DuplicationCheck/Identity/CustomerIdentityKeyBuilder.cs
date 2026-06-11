using System.Text.RegularExpressions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.DuplicationCheck.Models;

namespace Enset.Application.Imports.DuplicationCheck.Identity;

public static class CustomerIdentityKeyBuilder
{
    public static CustomerIdentity Build(CustomerImportDto dto)
    {
        var identity = new CustomerIdentity
        {
            NormalizedCompanyName = NormalizeCompanyName(dto.CompanyName),
            NormalizedStreet = NormalizeStreet(dto.Street),
            NormalizedHouseNumber = NormalizeHouseNumber(dto.HouseNumber),
            NormalizedPostalCode = Normalize(dto.PostalCode),
            NormalizedCity = NormalizeCity(dto.City),
            VatNumber = Normalize(dto.VatNumber),
            CompanyRegistrationNumber = Normalize(dto.CompanyRegistrationNumber),
            ExternalCustomerId = Normalize(dto.ExternalCustomerId)
        };

        identity.ExactKey = BuildExactKey(identity);

        return identity;
    }

    private static string BuildExactKey(CustomerIdentity identity)
    {
        if (!string.IsNullOrWhiteSpace(identity.ExternalCustomerId))
            return $"EXT:{identity.ExternalCustomerId}";

        if (!string.IsNullOrWhiteSpace(identity.VatNumber))
            return $"VAT:{identity.VatNumber}";

        if (!string.IsNullOrWhiteSpace(identity.CompanyRegistrationNumber))
            return $"REG:{identity.CompanyRegistrationNumber}";

        return string.Join("|",
            identity.NormalizedCompanyName,
            identity.NormalizedStreet,
            identity.NormalizedHouseNumber,
            identity.NormalizedPostalCode,
            identity.NormalizedCity);
    }

    public static string NormalizeCompanyName(string? value)
    {
        var text = Normalize(value);

        text = Regex.Replace(text, @"\bGMBH\b", "");
        text = Regex.Replace(text, @"\bGESELLSCHAFTMBH\b", "");
        text = Regex.Replace(text, @"\bGESELLSCHAFTMITBESCHRAENKTERHAFTUNG\b", "");
        text = Regex.Replace(text, @"\bAG\b", "");
        text = Regex.Replace(text, @"\bKG\b", "");
        text = Regex.Replace(text, @"\bOG\b", "");
        text = Regex.Replace(text, @"\bEU\b", "");

        return text;
    }

    public static string NormalizeStreet(string? value)
    {
        var text = Normalize(value);

        text = text.Replace("STRASSE", "STR");
        text = text.Replace("STRASZE", "STR");
        text = text.Replace("STREET", "STR");

        return text;
    }

    public static string NormalizeCity(string? value)
    {
        return Normalize(value);
    }

    public static string NormalizeHouseNumber(string? value)
    {
        return Normalize(value);
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim().ToUpperInvariant();

        text = text
            .Replace("Ä", "AE")
            .Replace("Ö", "OE")
            .Replace("Ü", "UE")
            .Replace("ß", "SS");

        text = Regex.Replace(text, @"[^A-Z0-9]", "");

        return text;
    }
}
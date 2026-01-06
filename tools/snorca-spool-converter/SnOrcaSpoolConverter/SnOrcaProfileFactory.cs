using System.Text.RegularExpressions;

namespace SnOrcaSpoolConverter;

public static class SnOrcaProfileFactory
{
    private static readonly Regex HexColorRegex = new(@"#?[0-9a-fA-F]{6}", RegexOptions.Compiled);

    public static SnOrcaFilamentProfile FromSpool(SpoolRecord spool, string vendorOverride)
    {
        var vendor = string.IsNullOrWhiteSpace(vendorOverride) ? spool.Brand.Trim() : vendorOverride.Trim();
        if (vendor.Length == 0) vendor = "Generic";

        var type = TypeMapping.NormalizeType(spool.Material, spool.MaterialType);
        var inherits = TypeMapping.BaseInheritsForType(type);
        var subType = TypeMapping.BuildSubType(spool.Material, spool.MaterialType, type);

        var colorHex = ExtractFirstHex(spool.Rgb) ?? "#FFFFFF";
        var idSuffix = TypeMapping.ShortIdSuffix(spool.Id);

        var displayName = BuildName(vendor, type, subType, spool.ColorName, idSuffix);
        var settingId = $"{TypeMapping.SanitizeId(vendor)}_{TypeMapping.SanitizeId(type)}_{TypeMapping.SanitizeId(idSuffix)}_0";
        var filamentId = $"{TypeMapping.SanitizeId(vendor)}_{TypeMapping.SanitizeId(type)}";

        var notes = new List<string>();
        notes.Add($"3DFP id: {spool.Id}");
        if (!string.IsNullOrWhiteSpace(spool.SpoolUrl)) notes.Add($"SpoolDB: {spool.SpoolUrl}");
        if (!string.IsNullOrWhiteSpace(spool.FilamentUrl)) notes.Add($"3DFP: {spool.FilamentUrl}");
        if (spool.RemainingGrams.HasValue) notes.Add($"Remaining: {spool.RemainingGrams.Value} g");
        if (!string.IsNullOrWhiteSpace(spool.Location)) notes.Add($"Location: {spool.Location}");
        if (!string.IsNullOrWhiteSpace(spool.UpdatedAt)) notes.Add($"Updated: {spool.UpdatedAt}");

        return new SnOrcaFilamentProfile
        {
            Name = displayName,
            Inherits = inherits,
            SettingId = settingId,
            FilamentId = filamentId,
            Vendor = vendor,
            FilamentType = type,
            FilamentSubType = subType,
            DefaultColour = colorHex,
            Notes = notes,
        };
    }

    private static string BuildName(string vendor, string type, string subType, string colorName, string idSuffix)
    {
        var parts = new List<string> { vendor, type };
        if (!string.IsNullOrWhiteSpace(subType)) parts.Add(subType);
        if (!string.IsNullOrWhiteSpace(colorName)) parts.Add(colorName.Trim());
        parts.Add($"#{idSuffix}");
        return string.Join(' ', parts);
    }

    private static string? ExtractFirstHex(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var m = HexColorRegex.Match(input);
        if (!m.Success) return null;
        var raw = m.Value.Trim().TrimStart('#').ToUpperInvariant();
        return $"#{raw}";
    }
}


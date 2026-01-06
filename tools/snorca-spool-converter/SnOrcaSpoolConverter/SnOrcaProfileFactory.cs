using System.Text.RegularExpressions;

namespace SnOrcaSpoolConverter;

public static class SnOrcaProfileFactory
{
    private static readonly Regex HexColorRegex = new(@"#?[0-9a-fA-F]{6}", RegexOptions.Compiled);

    public static SnOrcaFilamentProfile FromMaterialPreset(IEnumerable<SpoolRecord> spools, string vendorOverride, bool hideUnlessSpoolPresent)
    {
        var list = spools?.ToList() ?? [];
        if (list.Count == 0) throw new ArgumentException("No spools provided.", nameof(spools));

        // Use the last (typically most recently updated) record as representative for color/links.
        var spool = list[^1];

        var vendor = string.IsNullOrWhiteSpace(vendorOverride) ? spool.Brand.Trim() : vendorOverride.Trim();
        if (vendor.Length == 0) vendor = "Generic";

        var type = TypeMapping.NormalizeType(spool.Material, spool.MaterialType);
        var subType = TypeMapping.BuildSubType(spool.Material, spool.MaterialType, type);
        var normalizedSubType = subType?.Trim() ?? "";
        var idSubType = string.IsNullOrWhiteSpace(normalizedSubType) ? "BASIC" : normalizedSubType;

        var displayName = BuildMaterialName(vendor, type, normalizedSubType);
        var id = $"{TypeMapping.SanitizeId(vendor)}_{TypeMapping.SanitizeId(type)}_{TypeMapping.SanitizeId(idSubType)}";
        var filamentId = id;
        var settingId = $"{id}_0";

        var colorHex = ExtractFirstHex(spool.Rgb) ?? "#FFFFFF";

        var notes = new List<string>();
        notes.Add($"3DFP import: {list.Count} spool(s)");

        // Keep the URLs from the representative spool, and mention that there may be multiple.
        if (!string.IsNullOrWhiteSpace(spool.SpoolUrl)) notes.Add($"SpoolDB: {spool.SpoolUrl}");
        if (!string.IsNullOrWhiteSpace(spool.FilamentUrl)) notes.Add($"3DFP: {spool.FilamentUrl}");

        var extraConfig = FdmTemplateDefaults.TryGetDefaults(type, normalizedSubType);

        return new SnOrcaFilamentProfile
        {
            Name = displayName,
            Inherits = "",
            Instantiation = !hideUnlessSpoolPresent,
            SettingId = settingId,
            FilamentId = filamentId,
            Vendor = vendor,
            FilamentType = type,
            FilamentSubType = normalizedSubType,
            DefaultColour = colorHex,
            Notes = notes,
            ExtraConfig = extraConfig,
        };
    }

    public static SnOrcaFilamentProfile FromSpool(SpoolRecord spool, string vendorOverride)
    {
        var vendor = string.IsNullOrWhiteSpace(vendorOverride) ? spool.Brand.Trim() : vendorOverride.Trim();
        if (vendor.Length == 0) vendor = "Generic";

        var type = TypeMapping.NormalizeType(spool.Material, spool.MaterialType);
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
            Inherits = TypeMapping.BaseInheritsForType(type),
            SettingId = settingId,
            FilamentId = filamentId,
            Vendor = vendor,
            FilamentType = type,
            FilamentSubType = subType,
            DefaultColour = colorHex,
            Notes = notes,
        };
    }

    private static string BuildMaterialName(string vendor, string type, string subType)
    {
        var parts = new List<string> { vendor, type };
        if (!string.IsNullOrWhiteSpace(subType)) parts.Add(subType.Trim());
        return string.Join(' ', parts);
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

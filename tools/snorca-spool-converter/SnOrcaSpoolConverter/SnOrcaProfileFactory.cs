using System.Text.Json;
using System.Text.RegularExpressions;

namespace SnOrcaSpoolConverter;

public static class SnOrcaProfileFactory
{
    private static readonly Regex HexColorRegex = new(@"#?[0-9a-fA-F]{6}", RegexOptions.Compiled);
    private static readonly string[] BedTempKeys =
    [
        "hot_plate_temp",
        "hot_plate_temp_initial_layer",
        "textured_plate_temp",
        "textured_plate_temp_initial_layer",
        "cool_plate_temp",
        "cool_plate_temp_initial_layer",
        "eng_plate_temp",
        "eng_plate_temp_initial_layer",
    ];

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
        var extraColors = ExtractHexes(spool.Rgb).Skip(1).ToList();

        var notes = new List<string>();
        notes.Add($"3DFP import: {list.Count} spool(s)");
        if (extraColors.Count > 0) notes.Add($"Multi colors: {string.Join(", ", extraColors)}");

        // Keep the URLs from the representative spool, and mention that there may be multiple.
        if (!string.IsNullOrWhiteSpace(spool.SpoolUrl)) notes.Add($"SpoolDB: {spool.SpoolUrl}");
        if (!string.IsNullOrWhiteSpace(spool.FilamentUrl)) notes.Add($"3DFP: {spool.FilamentUrl}");

        var extraConfig = FdmTemplateDefaults.TryGetDefaults(type, normalizedSubType);
        ApplyTempOverrides(extraConfig, spool);

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
        var extraColors = ExtractHexes(spool.Rgb).Skip(1).ToList();
        var idSuffix = TypeMapping.ShortIdSuffix(spool.Id);

        var displayName = BuildName(vendor, type, subType, spool.ColorName, idSuffix);
        var settingId = $"{TypeMapping.SanitizeId(vendor)}_{TypeMapping.SanitizeId(type)}_{TypeMapping.SanitizeId(idSuffix)}_0";
        var filamentId = $"{TypeMapping.SanitizeId(vendor)}_{TypeMapping.SanitizeId(type)}";

        var notes = new List<string>();
        notes.Add($"3DFP id: {spool.Id}");
        if (extraColors.Count > 0) notes.Add($"Multi colors: {string.Join(", ", extraColors)}");
        if (!string.IsNullOrWhiteSpace(spool.SpoolUrl)) notes.Add($"SpoolDB: {spool.SpoolUrl}");
        if (!string.IsNullOrWhiteSpace(spool.FilamentUrl)) notes.Add($"3DFP: {spool.FilamentUrl}");
        if (spool.RemainingGrams.HasValue) notes.Add($"Remaining: {spool.RemainingGrams.Value} g");
        if (!string.IsNullOrWhiteSpace(spool.Location)) notes.Add($"Location: {spool.Location}");
        if (!string.IsNullOrWhiteSpace(spool.UpdatedAt)) notes.Add($"Updated: {spool.UpdatedAt}");

        var extraConfig = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        ApplyTempOverrides(extraConfig, spool);

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
            ExtraConfig = extraConfig,
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

    private static IReadOnlyList<string> ExtractHexes(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Array.Empty<string>();
        var matches = HexColorRegex.Matches(input);
        if (matches.Count == 0) return Array.Empty<string>();
        var list = new List<string>(matches.Count);
        foreach (Match m in matches)
        {
            if (!m.Success) continue;
            var raw = m.Value.Trim().TrimStart('#').ToUpperInvariant();
            if (raw.Length != 6) continue;
            list.Add($"#{raw}");
        }
        return list;
    }

    private static void ApplyTempOverrides(Dictionary<string, JsonElement> extraConfig, SpoolRecord spool)
    {
        var nozzleMin = spool.NozzleMinTemp;
        var nozzleMax = spool.NozzleMaxTemp;
        if (nozzleMin.HasValue && nozzleMax.HasValue)
        {
            extraConfig["nozzle_temperature_range_low"] = JsonSerializer.SerializeToElement(new[] { nozzleMin.Value.ToString() });
            extraConfig["nozzle_temperature_range_high"] = JsonSerializer.SerializeToElement(new[] { nozzleMax.Value.ToString() });

            // Use max as "selected" nozzle temp (reasonable default when only a range is known).
            extraConfig["nozzle_temperature"] = JsonSerializer.SerializeToElement(new[] { nozzleMax.Value.ToString() });
            extraConfig["nozzle_temperature_initial_layer"] = JsonSerializer.SerializeToElement(new[] { nozzleMax.Value.ToString() });
        }

        var bedMin = spool.BedMinTemp;
        var bedMax = spool.BedMaxTemp;
        if (bedMin.HasValue && bedMax.HasValue)
        {
            // Use max as "selected" bed temp (matches common preset style).
            var bed = bedMax.Value.ToString();
            foreach (var key in BedTempKeys)
            {
                // If a template already provides the key, overwrite it; if not, set the common hot/textured keys anyway.
                if (key.StartsWith("hot_plate", StringComparison.Ordinal) || key.StartsWith("textured_plate", StringComparison.Ordinal) || extraConfig.ContainsKey(key))
                    extraConfig[key] = JsonSerializer.SerializeToElement(new[] { bed });
            }
        }
    }
}

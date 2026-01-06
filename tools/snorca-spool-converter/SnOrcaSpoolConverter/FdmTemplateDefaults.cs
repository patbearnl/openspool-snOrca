using System.Text.Json;

namespace SnOrcaSpoolConverter;

public static class FdmTemplateDefaults
{
    private static readonly string[] KeysToCopy =
    [
        "nozzle_temperature_range_low",
        "nozzle_temperature_range_high",
        "nozzle_temperature",
        "nozzle_temperature_initial_layer",
        "hot_plate_temp",
        "hot_plate_temp_initial_layer",
        "textured_plate_temp",
        "textured_plate_temp_initial_layer",
        "cool_plate_temp",
        "cool_plate_temp_initial_layer",
        "eng_plate_temp",
        "eng_plate_temp_initial_layer",
        "filament_density",
        "filament_flow_ratio",
        "filament_max_volumetric_speed",
        "temperature_vitrification",
    ];

    private static readonly Dictionary<string, Dictionary<string, JsonElement>> CacheByTemplatePath = new(StringComparer.OrdinalIgnoreCase);

    public static Dictionary<string, JsonElement> TryGetDefaults(string normalizedType, string subType)
    {
        var templatePath = TryFindTemplatePath(normalizedType, subType);
        if (templatePath is null) return new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        if (CacheByTemplatePath.TryGetValue(templatePath, out var cached)) return cached;

        var loaded = LoadDefaultsFromTemplate(templatePath);
        CacheByTemplatePath[templatePath] = loaded;
        return loaded;
    }

    private static Dictionary<string, JsonElement> LoadDefaultsFromTemplate(string templatePath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(templatePath));
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        var root = doc.RootElement;
        var dict = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var key in KeysToCopy)
        {
            if (!root.TryGetProperty(key, out var value)) continue;
            dict[key] = value.Clone();
        }
        return dict;
    }

    private static string? TryFindTemplatePath(string normalizedType, string subType)
    {
        var candidates = GetCandidateTemplateFileNames(normalizedType, subType);
        var dirs = GetCandidateTemplateDirs();

        foreach (var dir in dirs)
        {
            foreach (var fileName in candidates)
            {
                var fullPath = Path.Combine(dir, fileName);
                if (File.Exists(fullPath)) return fullPath;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidateTemplateDirs()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // SnOrca (Snapmaker Orca) stores filament bases here.
        yield return Path.Combine(appData, "Snapmaker_Orca", "system", "Snapmaker", "filament");
        yield return Path.Combine(appData, "Snapmaker_Orca", "system", "OrcaFilamentLibrary", "filament", "base");

        // Optional: upstream OrcaSlicer layout (useful for contributors/dev setups).
        yield return Path.Combine(appData, "OrcaSlicer", "system", "OrcaFilamentLibrary", "filament", "base");
    }

    private static IEnumerable<string> GetCandidateTemplateFileNames(string normalizedType, string subType)
    {
        var t = (normalizedType ?? "").Trim().ToUpperInvariant();
        var st = (subType ?? "").Trim();

        var wantsSilk = st.IndexOf("silk", StringComparison.OrdinalIgnoreCase) >= 0;

        // Prefer Snapmaker "petg" base when possible (OrcaFilamentLibrary uses "pet").
        var stem = GuessStem(t);

        if (wantsSilk && t.StartsWith("PLA", StringComparison.Ordinal))
        {
            yield return "fdm_filament_pla_silk.json";
        }

        // Snapmaker templates (and some generic variants).
        yield return $"fdm_filament_{stem}.json";
        yield return $"fdm_filament_{stem}_generic.json";

        // OrcaFilamentLibrary fallbacks for PET/PETG naming differences.
        if (stem.Equals("petg", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fdm_filament_pet.json";
            yield return "fdm_filament_pet_generic.json";
        }

        // Extra fallbacks for some materials where Snapmaker only provides *_generic templates.
        if (stem.Equals("pc", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fdm_filament_pc_generic.json";
            yield return "fdm_filament_pc.json";
        }

        if (stem.Equals("pctg", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fdm_filament_pctg_generic.json";
            yield return "fdm_filament_pctg.json";
        }

        if (stem.Equals("pva", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fdm_filament_pva_generic.json";
            yield return "fdm_filament_pva.json";
        }

        if (stem.Equals("bvoh", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fdm_filament_bvoh_generic.json";
            yield return "fdm_filament_bvoh.json";
        }

        if (stem.Equals("pa", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fdm_filament_pa_generic.json";
            yield return "fdm_filament_pa.json";
        }

        if (stem.Equals("abs", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fdm_filament_abs_generic.json";
            yield return "fdm_filament_abs.json";
        }

        if (stem.Equals("asa", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fdm_filament_asa_generic.json";
            yield return "fdm_filament_asa.json";
        }

        if (stem.Equals("tpu", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fdm_filament_tpu_generic.json";
            yield return "fdm_filament_tpu.json";
        }

        if (stem.Equals("pla", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fdm_filament_pla_generic.json";
            yield return "fdm_filament_pla.json";
        }
    }

    private static string GuessStem(string normalizedTypeUpper)
    {
        if (normalizedTypeUpper.StartsWith("PLA", StringComparison.Ordinal)) return "pla";
        if (normalizedTypeUpper.StartsWith("PETG", StringComparison.Ordinal) || normalizedTypeUpper.StartsWith("PET", StringComparison.Ordinal)) return "petg";
        if (normalizedTypeUpper.StartsWith("PCTG", StringComparison.Ordinal)) return "pctg";
        if (normalizedTypeUpper.StartsWith("ABS", StringComparison.Ordinal)) return "abs";
        if (normalizedTypeUpper.StartsWith("ASA", StringComparison.Ordinal)) return "asa";
        if (normalizedTypeUpper.StartsWith("TPU", StringComparison.Ordinal)) return "tpu";
        if (normalizedTypeUpper.StartsWith("PC", StringComparison.Ordinal)) return "pc";
        if (normalizedTypeUpper.StartsWith("PA", StringComparison.Ordinal)) return "pa";
        if (normalizedTypeUpper.StartsWith("PPA", StringComparison.Ordinal)) return "ppa";
        if (normalizedTypeUpper.StartsWith("PPS", StringComparison.Ordinal)) return "pps";
        if (normalizedTypeUpper.StartsWith("PVA", StringComparison.Ordinal)) return "pva";
        if (normalizedTypeUpper.StartsWith("BVOH", StringComparison.Ordinal)) return "bvoh";
        if (normalizedTypeUpper.StartsWith("HIPS", StringComparison.Ordinal)) return "hips";
        if (normalizedTypeUpper.StartsWith("EVA", StringComparison.Ordinal)) return "eva";
        if (normalizedTypeUpper.StartsWith("PHA", StringComparison.Ordinal)) return "pha";
        if (normalizedTypeUpper.StartsWith("PP", StringComparison.Ordinal)) return "pp";
        if (normalizedTypeUpper.StartsWith("PE", StringComparison.Ordinal)) return "pe";
        if (normalizedTypeUpper.StartsWith("SBS", StringComparison.Ordinal)) return "sbs";
        return "pla";
    }
}


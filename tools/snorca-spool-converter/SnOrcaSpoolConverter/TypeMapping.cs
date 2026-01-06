using System.Text.RegularExpressions;

namespace SnOrcaSpoolConverter;

public static class TypeMapping
{
    private static readonly Regex IdSanitize = new(@"[^A-Za-z0-9]+", RegexOptions.Compiled);

    public static string NormalizeType(string material, string materialType)
    {
        var mat = (material ?? "").Trim();
        var mt = (materialType ?? "").Trim();
        var upper = mat.ToUpperInvariant();

        // PLA variants
        if (upper.StartsWith("PLA"))
        {
            // If material type suggests CF/GF/AERO, promote it to the main type to match Orca library naming.
            if (ContainsToken(mt, "CF") || ContainsToken(mt, "CARBON")) return "PLA-CF";
            if (ContainsToken(mt, "GF") || ContainsToken(mt, "GLASS")) return "PLA-CF";
            if (ContainsToken(mt, "AERO") || ContainsToken(mt, "LW")) return "PLA-AERO";
            return "PLA";
        }

        // PET / PETG variants
        if (upper.StartsWith("PETG") || upper.StartsWith("PET"))
        {
            if (ContainsToken(mt, "CF") || ContainsToken(mt, "CARBON")) return "PETG-CF";
            return "PETG";
        }

        if (upper.StartsWith("PCTG")) return "PCTG";

        if (upper.StartsWith("ABS"))
        {
            if (ContainsToken(mt, "GF") || ContainsToken(mt, "GLASS")) return "ABS-GF";
            return "ABS";
        }

        if (upper.StartsWith("ASA"))
        {
            if (ContainsToken(mt, "CF") || ContainsToken(mt, "CARBON")) return "ASA-CF";
            if (ContainsToken(mt, "AERO") || ContainsToken(mt, "LW")) return "ASA-AERO";
            return "ASA";
        }

        if (upper.StartsWith("TPU")) return "TPU";
        if (upper.StartsWith("PC")) return "PC";
        if (upper.StartsWith("PVA")) return "PVA";
        if (upper.StartsWith("BVOH")) return "BVOH";
        if (upper.StartsWith("HIPS")) return "HIPS";
        if (upper.StartsWith("PPS")) return ContainsToken(mt, "CF") ? "PPS-CF" : "PPS";

        if (upper.StartsWith("PPA"))
        {
            if (ContainsToken(mt, "GF") || ContainsToken(mt, "GLASS")) return "PPA-GF";
            return "PPA-CF";
        }

        if (upper.StartsWith("PA6")) return "PA";
        if (upper.StartsWith("PA"))
        {
            if (ContainsToken(mt, "GF") || ContainsToken(mt, "GLASS")) return "PA-GF";
            if (ContainsToken(mt, "CF") || ContainsToken(mt, "CARBON")) return "PA-CF";
            return "PA";
        }

        if (upper.StartsWith("PP"))
        {
            if (ContainsToken(mt, "GF") || ContainsToken(mt, "GLASS")) return "PP-GF";
            if (ContainsToken(mt, "CF") || ContainsToken(mt, "CARBON")) return "PP-CF";
            return "PP";
        }

        if (upper.StartsWith("PE"))
        {
            if (ContainsToken(mt, "CF") || ContainsToken(mt, "CARBON")) return "PE-CF";
            return "PE";
        }

        if (upper.StartsWith("EVA")) return "EVA";
        if (upper.StartsWith("PHA")) return "PHA";

        // Unknown: keep what we got (but trimmed).
        return mat.Length == 0 ? "PLA" : mat;
    }

    public static string BuildSubType(string material, string materialType, string normalizedType)
    {
        var mat = (material ?? "").Trim();
        var mt = (materialType ?? "").Trim();
        var type = (normalizedType ?? "").Trim();

        // Mirror Android importer behavior:
        // - "Basic" is treated as empty
        // - if material != normalized type and material_type present: combine both
        if (mt.Equals("basic", StringComparison.OrdinalIgnoreCase)) return "";

        if (string.IsNullOrWhiteSpace(mt))
        {
            if (!string.IsNullOrWhiteSpace(mat) && !mat.Equals(type, StringComparison.OrdinalIgnoreCase)) return mat;
            return "";
        }

        if (mat.Equals(type, StringComparison.OrdinalIgnoreCase)) return mt;

        return $"{mat} {mt}".Trim();
    }

    public static string BaseInheritsForType(string type)
    {
        var t = (type ?? "").Trim().ToUpperInvariant();
        // User presets must inherit from an existing, already-loaded preset name.
        // In SnOrca, the "fdm_filament_*" bases are not guaranteed to be present as presets
        // (depends on which vendor bundles were loaded), so prefer Generic profiles.
        return t switch
        {
            "PLA-CF"   => "Generic PLA-CF",
            "PETG-CF"  => "Generic PETG-CF",
            "PETG-GF"  => "Generic PETG-GF",
            "PETG-HF"  => "Generic PETG HF",
            "PA-CF"    => "Generic PA-CF",
            "PCTG"     => "Generic PCTG",
            "ABS"      => "Generic ABS",
            "ASA"      => "Generic ASA",
            "TPU"      => "Generic TPU",
            "PC"       => "Generic PC",
            "PA"       => "Generic PA",
            "PVA"      => "Generic PVA",
            "BVOH"     => "Generic BVOH",
            "PETG"     => "Generic PETG",
            "PLA"      => "Generic PLA",
            _ when t.StartsWith("PLA", StringComparison.Ordinal)  => "Generic PLA",
            _ when t.StartsWith("PET", StringComparison.Ordinal)  => "Generic PETG",
            _ when t.StartsWith("ABS", StringComparison.Ordinal)  => "Generic ABS",
            _ when t.StartsWith("ASA", StringComparison.Ordinal)  => "Generic ASA",
            _ when t.StartsWith("TPU", StringComparison.Ordinal)  => "Generic TPU",
            _ when t.StartsWith("PC", StringComparison.Ordinal)   => "Generic PC",
            _ when t.StartsWith("PA", StringComparison.Ordinal)   => "Generic PA",
            _ when t.StartsWith("PVA", StringComparison.Ordinal)  => "Generic PVA",
            _ when t.StartsWith("BVOH", StringComparison.Ordinal) => "Generic BVOH",
            _ => "Generic PLA",
        };
    }

    public static string ShortIdSuffix(string id)
    {
        var trimmed = (id ?? "").Trim();
        if (trimmed.Length <= 6) return trimmed;
        return trimmed[^6..];
    }

    public static string SanitizeId(string input)
    {
        var trimmed = (input ?? "").Trim();
        if (trimmed.Length == 0) return "X";
        var replaced = IdSanitize.Replace(trimmed.ToUpperInvariant(), "_").Trim('_');
        if (replaced.Length == 0) return "X";
        if (replaced.Length > 40) return replaced[..40];
        return replaced;
    }

    private static bool ContainsToken(string value, string token)
    {
        return value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

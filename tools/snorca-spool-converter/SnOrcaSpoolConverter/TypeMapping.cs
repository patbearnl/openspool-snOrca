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
        if (t.StartsWith("PLA")) return "fdm_filament_pla";
        if (t.StartsWith("PETG") || t.StartsWith("PET")) return "fdm_filament_pet";
        if (t.StartsWith("PCTG")) return "fdm_filament_pctg";
        if (t.StartsWith("ABS")) return "fdm_filament_abs";
        if (t.StartsWith("ASA")) return "fdm_filament_asa";
        if (t.StartsWith("TPU")) return "fdm_filament_tpu";
        if (t.StartsWith("PC")) return "fdm_filament_pc";
        if (t.StartsWith("PA")) return "fdm_filament_pa";
        if (t.StartsWith("PPA")) return "fdm_filament_ppa";
        if (t.StartsWith("PPS")) return "fdm_filament_pps";
        if (t.StartsWith("PVA")) return "fdm_filament_pva";
        if (t.StartsWith("BVOH")) return "fdm_filament_bvoh";
        if (t.StartsWith("HIPS")) return "fdm_filament_hips";
        if (t.StartsWith("PP")) return "fdm_filament_pp";
        if (t.StartsWith("PE")) return "fdm_filament_pe";
        if (t.StartsWith("EVA")) return "fdm_filament_eva";
        if (t.StartsWith("PHA")) return "fdm_filament_pha";
        if (t.StartsWith("SBS")) return "fdm_filament_sbs";
        return "fdm_filament_common";
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


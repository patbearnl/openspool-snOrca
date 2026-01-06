using System.Text.Json;

namespace SnOrcaSpoolConverter;

public static class SpoolImporters
{
    public static List<SpoolRecord> ReadFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var text = File.ReadAllText(path);

        return ext switch
        {
            ".csv" => Read3dfpCsv(text),
            ".json" => Read3dfpJson(text),
            _ => GuessByContent(text),
        };
    }

    private static List<SpoolRecord> GuessByContent(string text)
    {
        var t = text.TrimStart();
        if (t.StartsWith("{") || t.StartsWith("[")) return Read3dfpJson(text);
        return Read3dfpCsv(text);
    }

    private static List<SpoolRecord> Read3dfpJson(string json)
    {
        var t = json.Trim();
        if (t.Length == 0) return [];

        using var doc = JsonDocument.Parse(t);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            var list = new List<SpoolRecord>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                if (TryRead3dfpSpoolObject(item, out var record)) list.Add(record);
            }
            return list;
        }

        if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            // If someone pastes a single object, accept it if it looks like 3DFP.
            if (TryRead3dfpSpoolObject(doc.RootElement, out var record)) return [record];
        }

        return [];
    }

    private static bool TryRead3dfpSpoolObject(JsonElement obj, out SpoolRecord record)
    {
        record = default!;

        var id = obj.GetStringOrEmpty("id");
        var brand = obj.GetStringOrEmpty("brand");
        var material = obj.GetStringOrEmpty("material");
        var materialType = obj.GetStringOrEmpty("material_type");
        var color = obj.GetStringOrEmpty("color");
        var rgb = obj.GetStringOrEmpty("rgb");

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(material)) return false;

        record = new SpoolRecord(
            Id: id,
            Brand: brand,
            Material: material,
            MaterialType: materialType,
            ColorName: color,
            Rgb: rgb,
            Location: obj.TryGetString("location"),
            SpoolUrl: obj.TryGetString("spool_url"),
            FilamentUrl: obj.TryGetString("filament_url"),
            RemainingGrams: obj.TryGetInt("remaining_grams"),
            UpdatedAt: obj.TryGetString("updated_at")
        );

        return true;
    }

    private static List<SpoolRecord> Read3dfpCsv(string csv)
    {
        var rows = CsvUtils.Parse(csv);
        if (rows.Count == 0) return [];

        var list = new List<SpoolRecord>();
        foreach (var row in rows)
        {
            var id = row.GetValueOrDefault("id") ?? "";
            var brand = row.GetValueOrDefault("brand") ?? "";
            var material = row.GetValueOrDefault("material") ?? "";

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(material)) continue;

            list.Add(
                new SpoolRecord(
                    Id: id,
                    Brand: brand,
                    Material: material,
                    MaterialType: row.GetValueOrDefault("material_type") ?? "",
                    ColorName: row.GetValueOrDefault("color") ?? "",
                    Rgb: row.GetValueOrDefault("rgb") ?? "",
                    Location: row.GetValueOrDefault("location"),
                    SpoolUrl: row.GetValueOrDefault("spool_url"),
                    FilamentUrl: row.GetValueOrDefault("filament_url"),
                    RemainingGrams: row.TryGetInt("remaining_grams"),
                    UpdatedAt: row.GetValueOrDefault("updated_at")
                )
            );
        }

        return list;
    }

    private static string GetStringOrEmpty(this JsonElement obj, string key)
    {
        if (obj.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString() ?? "";
        return "";
    }

    private static string? TryGetString(this JsonElement obj, string key)
    {
        if (obj.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString();
        return null;
    }

    private static int? TryGetInt(this JsonElement obj, string key)
    {
        if (!obj.TryGetProperty(key, out var v)) return null;
        try
        {
            return v.ValueKind switch
            {
                JsonValueKind.Number => v.GetInt32(),
                JsonValueKind.String => int.TryParse(v.GetString(), out var n) ? n : null,
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }
}

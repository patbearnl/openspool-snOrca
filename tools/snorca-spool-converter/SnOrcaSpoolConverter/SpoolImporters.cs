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
            ".json" => ReadJson(text),
            _ => GuessByContent(text),
        };
    }

    private static List<SpoolRecord> GuessByContent(string text)
    {
        var t = text.TrimStart();
        if (t.StartsWith("{") || t.StartsWith("[")) return ReadJson(text);
        return Read3dfpCsv(text);
    }

    private static List<SpoolRecord> ReadJson(string json)
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
                if (TryRead3dfpSpoolObject(item, out var record) || TryReadSpoolmanObject(item, out record))
                    list.Add(record);
            }
            return list;
        }

        if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            // If someone pastes a single object, accept it if it looks like 3DFP or Spoolman.
            if (TryRead3dfpSpoolObject(doc.RootElement, out var record) || TryReadSpoolmanObject(doc.RootElement, out record))
                return [record];
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
            NozzleMinTemp: null,
            NozzleMaxTemp: null,
            BedMinTemp: null,
            BedMaxTemp: null,
            Location: obj.TryGetString("location"),
            SpoolUrl: obj.TryGetString("spool_url"),
            FilamentUrl: obj.TryGetString("filament_url"),
            RemainingGrams: obj.TryGetInt("remaining_grams"),
            UpdatedAt: obj.TryGetString("updated_at")
        );

        return true;
    }

    private static bool TryReadSpoolmanObject(JsonElement obj, out SpoolRecord record)
    {
        record = default!;

        var id = obj.GetStringOrEmpty("id");
        var manufacturer = obj.GetStringOrEmpty("manufacturer");
        var materialRaw = obj.GetStringOrEmpty("material");
        var name = obj.GetStringOrEmpty("name");
        var finish = obj.GetStringOrEmpty("finish");

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(manufacturer) || string.IsNullOrWhiteSpace(materialRaw))
            return false;

        // Keep converter behavior consistent with the Android app:
        // - normalized type derived from "material"
        // - subtype derived from "finish" only (not from "PLA+", etc.)
        var normalizedType = TypeMapping.NormalizeType(materialRaw, finish);
        var material = normalizedType;
        var materialType = finish;

        var rgb = "";
        if (obj.TryGetProperty("color_hexes", out var hexes) && hexes.ValueKind == JsonValueKind.Array)
        {
            var first = hexes.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.String) rgb = first.GetString() ?? "";
        }
        if (string.IsNullOrWhiteSpace(rgb))
            rgb = obj.GetStringOrEmpty("color_hex");

        var (nozzleMin, nozzleMax) = TryReadRangeOrValue(obj, "extruder_temp_range", "extruder_temp");
        var (bedMin, bedMax) = TryReadRangeOrValue(obj, "bed_temp_range", "bed_temp");

        record = new SpoolRecord(
            Id: id,
            Brand: manufacturer,
            Material: material,
            MaterialType: materialType,
            ColorName: name,
            Rgb: rgb,
            NozzleMinTemp: nozzleMin,
            NozzleMaxTemp: nozzleMax,
            BedMinTemp: bedMin,
            BedMaxTemp: bedMax,
            Location: null,
            SpoolUrl: null,
            FilamentUrl: null,
            RemainingGrams: null,
            UpdatedAt: null
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
                    NozzleMinTemp: null,
                    NozzleMaxTemp: null,
                    BedMinTemp: null,
                    BedMaxTemp: null,
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

    private static (int? Min, int? Max) TryReadRangeOrValue(JsonElement obj, string rangeKey, string valueKey)
    {
        if (obj.TryGetProperty(rangeKey, out var range) && range.ValueKind == JsonValueKind.Array)
        {
            int? min = null;
            int? max = null;
            var items = range.EnumerateArray().ToList();
            if (items.Count >= 1) min = TryReadIntElement(items[0]);
            if (items.Count >= 2) max = TryReadIntElement(items[1]);
            if (min.HasValue && max.HasValue) return (min, max);
        }

        var v = obj.TryGetInt(valueKey);
        if (!v.HasValue) return (null, null);
        return (v, v);
    }

    private static int? TryReadIntElement(JsonElement v)
    {
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

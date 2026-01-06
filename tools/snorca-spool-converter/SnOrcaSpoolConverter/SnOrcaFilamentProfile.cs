using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnOrcaSpoolConverter;

public sealed class SnOrcaFilamentProfile
{
    public string Version { get; init; } = "2.2.1";

    public required string Name { get; init; }
    public string Inherits { get; init; } = "";
    public bool Instantiation { get; init; } = true;
    public required string SettingId { get; init; }
    public required string FilamentId { get; init; }
    public required string Vendor { get; init; }
    public required string FilamentType { get; init; }
    public string FilamentSubType { get; init; } = "";
    public string DefaultColour { get; init; } = "";
    public List<string> Notes { get; init; } = [];
    public Dictionary<string, JsonElement> ExtraConfig { get; init; } = new(StringComparer.Ordinal);

    public string ToJson(bool indented)
    {
        var normalizedSubType = FilamentSubType?.Trim() ?? "";
        var payload = new Dictionary<string, object?>
        {
            ["type"] = "filament",
            ["version"] = Version,
            ["from"] = "user",
            ["instantiation"] = Instantiation ? "true" : "false",
            ["name"] = Name,
            ["inherits"] = Inherits ?? "",
            ["setting_id"] = SettingId,
            ["filament_id"] = FilamentId,
            ["filament_vendor"] = new[] { Vendor },
            ["filament_type"] = new[] { FilamentType },
            ["default_filament_colour"] = new[] { DefaultColour },
            ["filament_notes"] = Notes,
            ["compatible_printers"] = Array.Empty<string>(),
        };

        if (!string.IsNullOrWhiteSpace(normalizedSubType))
            payload["filament_sub_type"] = new[] { normalizedSubType };

        foreach (var (key, value) in ExtraConfig)
        {
            if (payload.ContainsKey(key)) continue;
            payload[key] = value;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        return JsonSerializer.Serialize(payload, options);
    }
}

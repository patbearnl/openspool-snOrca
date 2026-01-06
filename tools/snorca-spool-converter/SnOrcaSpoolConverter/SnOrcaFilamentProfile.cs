using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnOrcaSpoolConverter;

public sealed class SnOrcaFilamentProfile
{
    public required string Name { get; init; }
    public required string Inherits { get; init; }
    public required string SettingId { get; init; }
    public required string FilamentId { get; init; }
    public required string Vendor { get; init; }
    public required string FilamentType { get; init; }
    public string FilamentSubType { get; init; } = "";
    public string DefaultColour { get; init; } = "";
    public List<string> Notes { get; init; } = [];

    public string ToJson(bool indented)
    {
        var payload = new Dictionary<string, object?>
        {
            ["type"] = "filament",
            ["from"] = "user",
            ["instantiation"] = "true",
            ["name"] = Name,
            ["inherits"] = Inherits,
            ["setting_id"] = SettingId,
            ["filament_id"] = FilamentId,
            ["filament_vendor"] = new[] { Vendor },
            ["filament_type"] = new[] { FilamentType },
            ["filament_sub_type"] = new[] { string.IsNullOrWhiteSpace(FilamentSubType) ? "Basic" : FilamentSubType },
            ["default_filament_colour"] = new[] { DefaultColour },
            ["filament_notes"] = Notes,
            ["compatible_printers"] = Array.Empty<string>(),
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        return JsonSerializer.Serialize(payload, options);
    }
}


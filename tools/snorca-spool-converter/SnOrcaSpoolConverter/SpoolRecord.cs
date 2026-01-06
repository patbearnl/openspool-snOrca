namespace SnOrcaSpoolConverter;

public sealed record SpoolRecord(
    string Id,
    string Brand,
    string Material,
    string MaterialType,
    string ColorName,
    string Rgb,
    int? NozzleMinTemp,
    int? NozzleMaxTemp,
    int? BedMinTemp,
    int? BedMaxTemp,
    string? Location,
    string? SpoolUrl,
    string? FilamentUrl,
    int? RemainingGrams,
    string? UpdatedAt
);

namespace SnOrcaSpoolConverter;

public static class ListExtensions
{
    public static List<SpoolRecord> DeduplicateByIdKeepLast(this List<SpoolRecord> records)
    {
        var dict = new Dictionary<string, SpoolRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in records)
        {
            if (string.IsNullOrWhiteSpace(r.Id)) continue;
            dict[r.Id.Trim()] = r;
        }
        return dict.Values.ToList();
    }
}


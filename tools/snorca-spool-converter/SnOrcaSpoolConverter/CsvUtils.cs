namespace SnOrcaSpoolConverter;

public static class CsvUtils
{
    public static List<Dictionary<string, string>> Parse(string csv)
    {
        var lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (lines.Count == 0) return [];

        var header = ParseLine(lines[0]).Select(h => h.Trim()).ToList();
        if (header.Count == 0) return [];

        var rows = new List<Dictionary<string, string>>();
        foreach (var line in lines.Skip(1))
        {
            var values = ParseLine(line);
            if (values.Count == 0) continue;

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < header.Count; i++)
            {
                var key = header[i];
                if (string.IsNullOrWhiteSpace(key)) continue;
                row[key] = i < values.Count ? values[i] : "";
            }
            rows.Add(row);
        }

        return rows;
    }

    private static List<string> ParseLine(string line)
    {
        var outValues = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                var next = (i + 1) < line.Length ? line[i + 1] : '\0';
                if (inQuotes && next == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                outValues.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        outValues.Add(current.ToString());
        return outValues;
    }
}

public static class CsvRowExtensions
{
    public static int? TryGetInt(this Dictionary<string, string> row, string key)
    {
        if (!row.TryGetValue(key, out var v)) return null;
        if (int.TryParse(v.Trim(), out var n)) return n;
        return null;
    }
}


using System.Text.RegularExpressions;

namespace SnOrcaSpoolConverter;

public static class FileNameUtils
{
    private static readonly Regex InvalidChars = new($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);
    private static readonly Regex CollapseWhitespace = new(@"\s+", RegexOptions.Compiled);

    public static string SanitizeFileName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0) return "spool";
        var safe = InvalidChars.Replace(trimmed, "-");
        safe = CollapseWhitespace.Replace(safe, " ");
        return safe.Trim();
    }
}


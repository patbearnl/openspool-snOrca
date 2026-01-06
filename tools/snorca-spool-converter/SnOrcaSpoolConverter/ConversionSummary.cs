namespace SnOrcaSpoolConverter;

public sealed class ConversionSummary
{
    public int Written { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; } = [];
}


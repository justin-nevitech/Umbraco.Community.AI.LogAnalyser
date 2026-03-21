namespace AI.LogAnalyser.Models;

public class LogContextSettings
{
    public const string SectionName = "AILogAnalyser:LogContext";

    public int MaxSurroundingEntries { get; set; } = 10;

    public int SurroundingWindowMinutes { get; set; } = 5;

    public int FrequencyMaxScan { get; set; } = 500;

    public int FrequencyWindowMinutes { get; set; } = 60;
}

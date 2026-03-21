namespace AI.LogAnalyser.Models;

public class LogContextEntry
{
    public DateTimeOffset Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Exception { get; set; }
    public int Count { get; set; } = 1;
}

namespace Umbraco.Community.AI.LogAnalyser.Models;

public class LogAnalyserRequest
{
    public string Level { get; set; } = string.Empty;

    public string Timestamp { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? MessageTemplate { get; set; }

    public string? Exception { get; set; }

    public string? Properties { get; set; }
}

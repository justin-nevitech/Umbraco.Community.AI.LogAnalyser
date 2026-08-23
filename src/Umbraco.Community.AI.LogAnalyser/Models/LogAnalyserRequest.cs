namespace Umbraco.Community.AI.LogAnalyser.Models;

public class LogAnalyserRequest
{
    public string Level { get; set; } = string.Empty;

    public string Timestamp { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? MessageTemplate { get; set; }

    public string? Exception { get; set; }

    public string? Properties { get; set; }

    /// <summary>
    /// Optional per-request opt-out from snarky mode. Setting this to <c>false</c> suppresses
    /// snark for this request; any other value defers to the configured
    /// <c>AILogAnalyser:SnarkyMode</c>. It cannot enable the mode when configuration has it off.
    /// </summary>
    public bool? Snarky { get; set; }
}

namespace Umbraco.Community.AI.LogAnalyser.Models;

public class LogAnalyserResponse
{
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Whether this analysis was generated in snarky mode.
    /// </summary>
    public bool Snarky { get; set; }
}

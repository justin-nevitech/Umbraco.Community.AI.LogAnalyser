namespace Umbraco.Community.AI.LogAnalyser.Models;

public class LogContextResult
{
    public List<LogContextEntry> Before { get; set; } = [];
    public List<LogContextEntry> After { get; set; } = [];
}

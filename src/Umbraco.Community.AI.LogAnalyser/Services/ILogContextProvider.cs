using Umbraco.Community.AI.LogAnalyser.Models;

namespace Umbraco.Community.AI.LogAnalyser.Services;

public interface ILogContextProvider
{
    Task<LogContextResult> GetSurroundingLogsAsync(
        DateTimeOffset timestamp,
        string renderedMessage,
        CancellationToken ct);

    Task<int> GetErrorFrequencyAsync(
        DateTimeOffset timestamp,
        string renderedMessage,
        CancellationToken ct);
}

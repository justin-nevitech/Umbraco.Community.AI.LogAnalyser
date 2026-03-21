using AI.LogAnalyser.Models;

namespace AI.LogAnalyser.Services;

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

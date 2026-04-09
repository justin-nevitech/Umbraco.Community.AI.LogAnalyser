using Umbraco.Community.AI.LogAnalyser.Models;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.AI.LogAnalyser.Services;

public class LogContextProvider : ILogContextProvider
{
    private readonly ILogViewerService _logViewerService;
    private readonly LogContextSettings _settings;

    public LogContextProvider(
        ILogViewerService logViewerService,
        IOptions<LogContextSettings> settings)
    {
        _logViewerService = logViewerService;
        _settings = settings.Value;
    }

    public async Task<LogContextResult> GetSurroundingLogsAsync(
        DateTimeOffset timestamp,
        string renderedMessage,
        CancellationToken ct)
    {
        var beforeTask = GetLogsBeforeAsync(timestamp, renderedMessage, ct);
        var afterTask = GetLogsAfterAsync(timestamp, renderedMessage, ct);

        await Task.WhenAll(beforeTask, afterTask);

        return new LogContextResult
        {
            Before = await beforeTask,
            After = await afterTask,
        };
    }

    private async Task<List<LogContextEntry>> GetLogsBeforeAsync(
        DateTimeOffset timestamp, string renderedMessage, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var window = TimeSpan.FromMinutes(_settings.SurroundingWindowMinutes);
        var maxEntries = _settings.MaxSurroundingEntries;

        var startDate = timestamp.Subtract(window);
        // take extra to account for excluding the current entry
        var result = await _logViewerService.GetPagedLogsAsync(
            startDate, timestamp, skip: 0, take: maxEntries + 1,
            orderDirection: Direction.Descending);

        if (!result.Success || result.Result is null)
            return [];

        var entries = result.Result.Items
            .Where(e => !IsCurrentEntry(e, timestamp, renderedMessage))
            .Take(maxEntries)
            .Reverse()
            .Select(ToContextEntry)
            .ToList();

        return DeduplicateConsecutive(entries);
    }

    private async Task<List<LogContextEntry>> GetLogsAfterAsync(
        DateTimeOffset timestamp, string renderedMessage, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var window = TimeSpan.FromMinutes(_settings.SurroundingWindowMinutes);
        var maxEntries = _settings.MaxSurroundingEntries;

        var endDate = timestamp.Add(window);
        var result = await _logViewerService.GetPagedLogsAsync(
            timestamp, endDate, skip: 0, take: maxEntries + 1,
            orderDirection: Direction.Ascending);

        if (!result.Success || result.Result is null)
            return [];

        var entries = result.Result.Items
            .Where(e => !IsCurrentEntry(e, timestamp, renderedMessage))
            .Take(maxEntries)
            .Select(ToContextEntry)
            .ToList();

        return DeduplicateConsecutive(entries);
    }

    private static bool IsCurrentEntry(
        Umbraco.Cms.Core.Logging.Viewer.ILogEntry entry,
        DateTimeOffset timestamp,
        string renderedMessage)
    {
        return entry.Timestamp == timestamp
            && string.Equals(entry.RenderedMessage, renderedMessage, StringComparison.Ordinal);
    }

    private static LogContextEntry ToContextEntry(Umbraco.Cms.Core.Logging.Viewer.ILogEntry entry)
    {
        return new LogContextEntry
        {
            Timestamp = entry.Timestamp,
            Level = entry.Level.ToString(),
            Message = entry.RenderedMessage,
            Exception = entry.Exception,
        };
    }

    public async Task<int> GetErrorFrequencyAsync(
        DateTimeOffset timestamp,
        string renderedMessage,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var frequencyWindow = TimeSpan.FromMinutes(_settings.FrequencyWindowMinutes);
        var startDate = timestamp.Subtract(frequencyWindow);
        var result = await _logViewerService.GetPagedLogsAsync(
            startDate, timestamp, skip: 0, take: _settings.FrequencyMaxScan,
            orderDirection: Direction.Descending);

        if (!result.Success || result.Result is null)
            return 0;

        return result.Result.Items
            .Count(e => string.Equals(e.RenderedMessage, renderedMessage, StringComparison.Ordinal));
    }

    /// <summary>
    /// Collapses consecutive entries with the same level and message into a single entry with a count.
    /// </summary>
    private static List<LogContextEntry> DeduplicateConsecutive(List<LogContextEntry> entries)
    {
        if (entries.Count == 0)
            return entries;

        var result = new List<LogContextEntry> { entries[0] };

        for (var i = 1; i < entries.Count; i++)
        {
            var current = entries[i];
            var previous = result[^1];

            if (string.Equals(current.Level, previous.Level, StringComparison.Ordinal)
                && string.Equals(current.Message, previous.Message, StringComparison.Ordinal))
            {
                previous.Count++;
            }
            else
            {
                result.Add(current);
            }
        }

        return result;
    }
}

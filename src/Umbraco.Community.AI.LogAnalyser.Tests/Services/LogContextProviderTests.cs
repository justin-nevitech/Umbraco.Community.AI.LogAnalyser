using Umbraco.Community.AI.LogAnalyser.Models;
using Umbraco.Community.AI.LogAnalyser.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Logging.Viewer;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Xunit;

namespace Umbraco.Community.AI.LogAnalyser.Tests.Services;

public class LogContextProviderTests
{
    private readonly ILogViewerService _logViewerService;
    private readonly LogContextSettings _settings;
    private readonly LogContextProvider _sut;

    private static readonly DateTimeOffset TestTimestamp = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private const string TestMessage = "Test log message";

    public LogContextProviderTests()
    {
        _logViewerService = Substitute.For<ILogViewerService>();
        _settings = new LogContextSettings
        {
            MaxSurroundingEntries = 10,
            SurroundingWindowMinutes = 5,
            FrequencyMaxScan = 500,
            FrequencyWindowMinutes = 60,
        };
        var options = Substitute.For<IOptions<LogContextSettings>>();
        options.Value.Returns(_settings);

        _sut = new LogContextProvider(_logViewerService, options);
    }

    #region GetSurroundingLogsAsync

    [Fact]
    public async Task GetSurroundingLogsAsync_ReturnsBeforeAndAfterEntries()
    {
        var beforeEntry = CreateLogEntry(TestTimestamp.AddSeconds(-30), "Before entry", LogLevel.Information);
        var afterEntry = CreateLogEntry(TestTimestamp.AddSeconds(30), "After entry", LogLevel.Warning);

        SetupPagedLogs(Direction.Descending, [beforeEntry]);
        SetupPagedLogs(Direction.Ascending, [afterEntry]);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(1);
        result.Before[0].Message.Should().Be("Before entry");
        result.After.Should().HaveCount(1);
        result.After[0].Message.Should().Be("After entry");
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_ExcludesCurrentEntry()
    {
        var currentEntry = CreateLogEntry(TestTimestamp, TestMessage, LogLevel.Error);
        var otherEntry = CreateLogEntry(TestTimestamp.AddSeconds(-10), "Other entry", LogLevel.Information);

        SetupPagedLogs(Direction.Descending, [currentEntry, otherEntry]);
        SetupPagedLogs(Direction.Ascending, [currentEntry]);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(1);
        result.Before[0].Message.Should().Be("Other entry");
        result.After.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_ReturnsEmpty_WhenServiceFails()
    {
        SetupFailedPagedLogs(Direction.Descending);
        SetupFailedPagedLogs(Direction.Ascending);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().BeEmpty();
        result.After.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_ReturnsEmpty_WhenResultIsNull()
    {
        SetupNullResultPagedLogs(Direction.Descending);
        SetupNullResultPagedLogs(Direction.Ascending);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().BeEmpty();
        result.After.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_DeduplicatesConsecutiveEntries()
    {
        var entry1 = CreateLogEntry(TestTimestamp.AddSeconds(-30), "Repeated message", LogLevel.Error);
        var entry2 = CreateLogEntry(TestTimestamp.AddSeconds(-20), "Repeated message", LogLevel.Error);
        var entry3 = CreateLogEntry(TestTimestamp.AddSeconds(-10), "Repeated message", LogLevel.Error);

        SetupPagedLogs(Direction.Descending, [entry3, entry2, entry1]);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(1);
        result.Before[0].Message.Should().Be("Repeated message");
        result.Before[0].Count.Should().Be(3);
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_DoesNotDeduplicateDifferentMessages()
    {
        var entry1 = CreateLogEntry(TestTimestamp.AddSeconds(-30), "Message A", LogLevel.Error);
        var entry2 = CreateLogEntry(TestTimestamp.AddSeconds(-20), "Message B", LogLevel.Error);

        SetupPagedLogs(Direction.Descending, [entry2, entry1]);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_DoesNotDeduplicateDifferentLevels()
    {
        var entry1 = CreateLogEntry(TestTimestamp.AddSeconds(-30), "Same message", LogLevel.Error);
        var entry2 = CreateLogEntry(TestTimestamp.AddSeconds(-20), "Same message", LogLevel.Warning);

        SetupPagedLogs(Direction.Descending, [entry2, entry1]);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_RespectsMaxEntries()
    {
        _settings.MaxSurroundingEntries = 2;

        var entries = Enumerable.Range(1, 5)
            .Select(i => CreateLogEntry(TestTimestamp.AddSeconds(-i * 10), $"Entry {i}", LogLevel.Information))
            .ToArray();

        SetupPagedLogs(Direction.Descending, entries);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_MapsFieldsCorrectly()
    {
        var entry = CreateLogEntry(TestTimestamp.AddSeconds(-10), "A message", LogLevel.Warning, "System.Exception: boom");

        SetupPagedLogs(Direction.Descending, [entry]);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(1);
        var mapped = result.Before[0];
        mapped.Level.Should().Be("Warning");
        mapped.Message.Should().Be("A message");
        mapped.Exception.Should().Be("System.Exception: boom");
        mapped.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_DeduplicatesAfterEntriesToo()
    {
        var entry1 = CreateLogEntry(TestTimestamp.AddSeconds(10), "Repeated", LogLevel.Information);
        var entry2 = CreateLogEntry(TestTimestamp.AddSeconds(20), "Repeated", LogLevel.Information);

        SetupPagedLogs(Direction.Descending, []);
        SetupPagedLogs(Direction.Ascending, [entry1, entry2]);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.After.Should().HaveCount(1);
        result.After[0].Count.Should().Be(2);
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_ThrowsWhenCancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_BeforeEntriesAreInChronologicalOrder()
    {
        // Descending result: newest first
        var entry1 = CreateLogEntry(TestTimestamp.AddSeconds(-10), "Newest before", LogLevel.Information);
        var entry2 = CreateLogEntry(TestTimestamp.AddSeconds(-20), "Middle before", LogLevel.Information);
        var entry3 = CreateLogEntry(TestTimestamp.AddSeconds(-30), "Oldest before", LogLevel.Information);

        SetupPagedLogs(Direction.Descending, [entry1, entry2, entry3]);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(3);
        result.Before[0].Message.Should().Be("Oldest before");
        result.Before[1].Message.Should().Be("Middle before");
        result.Before[2].Message.Should().Be("Newest before");
    }

    #endregion

    #region GetErrorFrequencyAsync

    [Fact]
    public async Task GetErrorFrequencyAsync_CountsMatchingMessages()
    {
        var entries = new[]
        {
            CreateLogEntry(TestTimestamp.AddMinutes(-5), TestMessage, LogLevel.Error),
            CreateLogEntry(TestTimestamp.AddMinutes(-10), TestMessage, LogLevel.Error),
            CreateLogEntry(TestTimestamp.AddMinutes(-15), "Different message", LogLevel.Error),
            CreateLogEntry(TestTimestamp.AddMinutes(-20), TestMessage, LogLevel.Error),
        };

        SetupFrequencyLogs(entries);

        var count = await _sut.GetErrorFrequencyAsync(TestTimestamp, TestMessage, CancellationToken.None);

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetErrorFrequencyAsync_ReturnsZero_WhenServiceFails()
    {
        SetupFailedPagedLogs(Direction.Descending);

        var count = await _sut.GetErrorFrequencyAsync(TestTimestamp, TestMessage, CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetErrorFrequencyAsync_ReturnsZero_WhenNoMatches()
    {
        var entries = new[]
        {
            CreateLogEntry(TestTimestamp.AddMinutes(-5), "Other message 1", LogLevel.Error),
            CreateLogEntry(TestTimestamp.AddMinutes(-10), "Other message 2", LogLevel.Error),
        };

        SetupFrequencyLogs(entries);

        var count = await _sut.GetErrorFrequencyAsync(TestTimestamp, TestMessage, CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetErrorFrequencyAsync_OnlyCountsExactMatches()
    {
        var entries = new[]
        {
            CreateLogEntry(TestTimestamp.AddMinutes(-5), TestMessage, LogLevel.Error),
            CreateLogEntry(TestTimestamp.AddMinutes(-10), TestMessage + " extra", LogLevel.Error),
            CreateLogEntry(TestTimestamp.AddMinutes(-15), "prefix " + TestMessage, LogLevel.Error),
        };

        SetupFrequencyLogs(entries);

        var count = await _sut.GetErrorFrequencyAsync(TestTimestamp, TestMessage, CancellationToken.None);

        count.Should().Be(1);
    }

    [Fact]
    public async Task GetErrorFrequencyAsync_IsCaseSensitive()
    {
        var entries = new[]
        {
            CreateLogEntry(TestTimestamp.AddMinutes(-5), TestMessage, LogLevel.Error),
            CreateLogEntry(TestTimestamp.AddMinutes(-10), TestMessage.ToUpperInvariant(), LogLevel.Error),
        };

        SetupFrequencyLogs(entries);

        var count = await _sut.GetErrorFrequencyAsync(TestTimestamp, TestMessage, CancellationToken.None);

        count.Should().Be(1);
    }

    [Fact]
    public async Task GetErrorFrequencyAsync_ThrowsWhenCancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _sut.GetErrorFrequencyAsync(TestTimestamp, TestMessage, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetErrorFrequencyAsync_UsesConfiguredFrequencyWindow()
    {
        _settings.FrequencyWindowMinutes = 30;

        SetupFrequencyLogs([]);

        await _sut.GetErrorFrequencyAsync(TestTimestamp, TestMessage, CancellationToken.None);

        var expectedStart = TestTimestamp.Subtract(TimeSpan.FromMinutes(30));
        await _logViewerService.Received(1).GetPagedLogsAsync(
            expectedStart, TestTimestamp,
            Arg.Any<int>(), Arg.Any<int>(),
            orderDirection: Direction.Descending);
    }

    [Fact]
    public async Task GetErrorFrequencyAsync_UsesConfiguredMaxScan()
    {
        _settings.FrequencyMaxScan = 100;

        SetupFrequencyLogs([]);

        await _sut.GetErrorFrequencyAsync(TestTimestamp, TestMessage, CancellationToken.None);

        await _logViewerService.Received(1).GetPagedLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            0, 100,
            orderDirection: Arg.Any<Direction>());
    }

    [Fact]
    public async Task GetErrorFrequencyAsync_ReturnsZero_WhenEmptyResults()
    {
        SetupFrequencyLogs([]);

        var count = await _sut.GetErrorFrequencyAsync(TestTimestamp, TestMessage, CancellationToken.None);

        count.Should().Be(0);
    }

    #endregion

    #region Deduplication Edge Cases

    [Fact]
    public async Task GetSurroundingLogsAsync_DoesNotDeduplicateNonConsecutiveSameMessage()
    {
        // Pattern: A, B, A — the two A's are NOT consecutive, so no dedup
        var entryA1 = CreateLogEntry(TestTimestamp.AddSeconds(-30), "Message A", LogLevel.Error);
        var entryB = CreateLogEntry(TestTimestamp.AddSeconds(-20), "Message B", LogLevel.Error);
        var entryA2 = CreateLogEntry(TestTimestamp.AddSeconds(-10), "Message A", LogLevel.Error);

        // Descending: newest first → A2, B, A1; reversed → A1, B, A2
        SetupPagedLogs(Direction.Descending, [entryA2, entryB, entryA1]);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(3);
        result.Before[0].Message.Should().Be("Message A");
        result.Before[0].Count.Should().Be(1);
        result.Before[1].Message.Should().Be("Message B");
        result.Before[2].Message.Should().Be("Message A");
        result.Before[2].Count.Should().Be(1);
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_DeduplicatesMixedPattern()
    {
        // Pattern after reversal: A, A, B, B, A → 3 groups with counts 2, 2, 1
        var entries = new[]
        {
            CreateLogEntry(TestTimestamp.AddSeconds(-10), "A", LogLevel.Error),  // newest
            CreateLogEntry(TestTimestamp.AddSeconds(-20), "B", LogLevel.Error),
            CreateLogEntry(TestTimestamp.AddSeconds(-30), "B", LogLevel.Error),
            CreateLogEntry(TestTimestamp.AddSeconds(-40), "A", LogLevel.Error),
            CreateLogEntry(TestTimestamp.AddSeconds(-50), "A", LogLevel.Error),  // oldest
        };

        SetupPagedLogs(Direction.Descending, entries);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(3);
        result.Before[0].Message.Should().Be("A");
        result.Before[0].Count.Should().Be(2);
        result.Before[1].Message.Should().Be("B");
        result.Before[1].Count.Should().Be(2);
        result.Before[2].Message.Should().Be("A");
        result.Before[2].Count.Should().Be(1);
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_SingleEntry_CountIsOne()
    {
        var entry = CreateLogEntry(TestTimestamp.AddSeconds(-10), "Only entry", LogLevel.Information);

        SetupPagedLogs(Direction.Descending, [entry]);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(1);
        result.Before[0].Count.Should().Be(1);
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_MapsNullExceptionCorrectly()
    {
        var entry = CreateLogEntry(TestTimestamp.AddSeconds(-10), "No exception", LogLevel.Information);

        SetupPagedLogs(Direction.Descending, [entry]);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before[0].Exception.Should().BeNull();
    }

    #endregion

    #region Asymmetric Failures

    [Fact]
    public async Task GetSurroundingLogsAsync_BeforeFails_AfterSucceeds()
    {
        SetupFailedPagedLogs(Direction.Descending);
        var afterEntry = CreateLogEntry(TestTimestamp.AddSeconds(10), "After entry", LogLevel.Information);
        SetupPagedLogs(Direction.Ascending, [afterEntry]);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().BeEmpty();
        result.After.Should().HaveCount(1);
        result.After[0].Message.Should().Be("After entry");
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_BeforeSucceeds_AfterFails()
    {
        var beforeEntry = CreateLogEntry(TestTimestamp.AddSeconds(-10), "Before entry", LogLevel.Information);
        SetupPagedLogs(Direction.Descending, [beforeEntry]);
        SetupFailedPagedLogs(Direction.Ascending);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(1);
        result.Before[0].Message.Should().Be("Before entry");
        result.After.Should().BeEmpty();
    }

    #endregion

    #region Service Call Parameters

    [Fact]
    public async Task GetSurroundingLogsAsync_RequestsTakePlusOneForCurrentEntryExclusion()
    {
        _settings.MaxSurroundingEntries = 5;

        SetupPagedLogs(Direction.Descending, []);
        SetupPagedLogs(Direction.Ascending, []);

        await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        // Should request take: 5 + 1 = 6 to account for excluding the current entry
        await _logViewerService.Received().GetPagedLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            0, 6,
            orderDirection: Arg.Any<Direction>());
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_UsesConfiguredSurroundingWindow()
    {
        _settings.SurroundingWindowMinutes = 15;

        SetupPagedLogs(Direction.Descending, []);
        SetupPagedLogs(Direction.Ascending, []);

        await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        var expectedBefore = TestTimestamp.Subtract(TimeSpan.FromMinutes(15));
        var expectedAfter = TestTimestamp.Add(TimeSpan.FromMinutes(15));

        await _logViewerService.Received(1).GetPagedLogsAsync(
            expectedBefore, TestTimestamp,
            Arg.Any<int>(), Arg.Any<int>(),
            orderDirection: Direction.Descending);

        await _logViewerService.Received(1).GetPagedLogsAsync(
            TestTimestamp, expectedAfter,
            Arg.Any<int>(), Arg.Any<int>(),
            orderDirection: Direction.Ascending);
    }

    #endregion

    #region Current Entry Exclusion Edge Cases

    [Fact]
    public async Task GetSurroundingLogsAsync_DoesNotExcludeEntryWithSameTimestampButDifferentMessage()
    {
        var sameTimeDifferentMsg = CreateLogEntry(TestTimestamp, "Different message at same time", LogLevel.Error);

        SetupPagedLogs(Direction.Descending, [sameTimeDifferentMsg]);
        SetupPagedLogs(Direction.Ascending, []);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.Before.Should().HaveCount(1);
        result.Before[0].Message.Should().Be("Different message at same time");
    }

    [Fact]
    public async Task GetSurroundingLogsAsync_DoesNotExcludeEntryWithSameMessageButDifferentTimestamp()
    {
        var differentTimeSameMsg = CreateLogEntry(TestTimestamp.AddMilliseconds(1), TestMessage, LogLevel.Error);

        SetupPagedLogs(Direction.Descending, []);
        SetupPagedLogs(Direction.Ascending, [differentTimeSameMsg]);

        var result = await _sut.GetSurroundingLogsAsync(TestTimestamp, TestMessage, CancellationToken.None);

        result.After.Should().HaveCount(1);
    }

    #endregion

    #region Helpers

    private static ILogEntry CreateLogEntry(DateTimeOffset timestamp, string message, LogLevel level, string? exception = null)
    {
        var entry = Substitute.For<ILogEntry>();
        entry.Timestamp.Returns(timestamp);
        entry.RenderedMessage.Returns(message);
        entry.Level.Returns(level);
        entry.Exception.Returns(exception);
        return entry;
    }

    private void SetupPagedLogs(Direction direction, ILogEntry[] entries)
    {
        var paged = new PagedModel<ILogEntry>(entries.Length, entries);
        _logViewerService.GetPagedLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<int>(),
            orderDirection: direction)
            .Returns(Attempt.SucceedWithStatus(LogViewerOperationStatus.Success, paged));
    }

    private void SetupFailedPagedLogs(Direction direction)
    {
        _logViewerService.GetPagedLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<int>(),
            orderDirection: direction)
            .Returns(Attempt.FailWithStatus(LogViewerOperationStatus.CancelledByLogsSizeValidation, new PagedModel<ILogEntry>(0, [])));
    }

    private void SetupNullResultPagedLogs(Direction direction)
    {
        _logViewerService.GetPagedLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<int>(),
            orderDirection: direction)
            .Returns(Attempt.FailWithStatus(LogViewerOperationStatus.CancelledByLogsSizeValidation, (PagedModel<ILogEntry>?)null));
    }

    private void SetupFrequencyLogs(ILogEntry[] entries)
    {
        var paged = new PagedModel<ILogEntry>(entries.Length, entries);
        _logViewerService.GetPagedLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<int>(),
            orderDirection: Direction.Descending)
            .Returns(Attempt.SucceedWithStatus(LogViewerOperationStatus.Success, paged));
    }

    #endregion
}

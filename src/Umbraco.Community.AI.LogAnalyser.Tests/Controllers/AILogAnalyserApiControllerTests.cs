using Umbraco.Community.AI.LogAnalyser.Controllers;
using Umbraco.Community.AI.LogAnalyser.Models;
using Umbraco.Community.AI.LogAnalyser.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Umbraco.AI.Core.Chat;
using Xunit;

namespace Umbraco.Community.AI.LogAnalyser.Tests.Controllers;

public class AILogAnalyserApiControllerTests
{
    private readonly IAIChatService _chatService;
    private readonly ISystemDiagnosticsProvider _diagnostics;
    private readonly ILogContextProvider _logContext;
    private readonly ILogger<AILogAnalyserApiController> _logger;
    private readonly AILogAnalyserApiController _sut;

    public AILogAnalyserApiControllerTests()
    {
        _chatService = Substitute.For<IAIChatService>();
        _diagnostics = Substitute.For<ISystemDiagnosticsProvider>();
        _logContext = Substitute.For<ILogContextProvider>();
        _logger = Substitute.For<ILogger<AILogAnalyserApiController>>();

        _diagnostics.GetContext().Returns("Umbraco: 17.2.2\n.NET: .NET 10.0\n");

        _sut = new AILogAnalyserApiController(_chatService, _diagnostics, _logContext, _logger);
    }

    #region Validation

    [Fact]
    public async Task Analyse_ReturnsBadRequest_WhenMessageIsEmpty()
    {
        var request = new LogAnalyserRequest { Level = "Error", Message = "" };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Analyse_ReturnsBadRequest_WhenMessageIsWhitespace()
    {
        var request = new LogAnalyserRequest { Level = "Error", Message = "   " };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Analyse_ReturnsBadRequest_WhenLevelIsEmpty()
    {
        var request = new LogAnalyserRequest { Level = "", Message = "Some error" };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Analyse_ReturnsBadRequest_WhenLevelIsWhitespace()
    {
        var request = new LogAnalyserRequest { Level = "  ", Message = "Some error" };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Analyse_ReturnsBadRequest_WhenBothEmpty()
    {
        var request = new LogAnalyserRequest { Level = "", Message = "" };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Successful Analysis

    [Fact]
    public async Task Analyse_ReturnsOk_WithAiSummary()
    {
        SetupSuccessfulChatResponse("## Summary\nTest analysis result");
        var request = CreateValidRequest();

        var result = await _sut.Analyse(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LogAnalyserResponse>().Subject;
        response.Summary.Should().Be("## Summary\nTest analysis result");
    }

    [Fact]
    public async Task Analyse_ReturnsEmptySummary_WhenAiReturnsNullText()
    {
        // ChatResponse.Text returns "" (not null) when constructed with null content,
        // so the controller's null-coalescing fallback doesn't trigger.
        SetupSuccessfulChatResponse(null);
        var request = CreateValidRequest();

        var result = await _sut.Analyse(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LogAnalyserResponse>().Subject;
        response.Summary.Should().BeEmpty();
    }

    [Fact]
    public async Task Analyse_WorksWithoutOptionalFields()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Something broke",
        };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Analyse_IncludesMessageTemplate_InPrompt()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Failed to load item 42",
            MessageTemplate = "Failed to load item {ItemId}",
            Timestamp = "2025-06-15T12:00:00Z",
        };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("Failed to load item {ItemId}"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_IncludesException_InPrompt()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Unhandled exception",
            Exception = "System.NullReferenceException: Object reference not set",
        };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("System.NullReferenceException"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_IncludesProperties_InPrompt()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error occurred",
            Properties = "{\"RequestId\": \"abc-123\"}",
        };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("abc-123"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region Truncation

    [Fact]
    public async Task Analyse_TruncatesLongException()
    {
        SetupSuccessfulChatResponse("Analysis");
        var longException = new string('x', 10000);
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error",
            Exception = longException,
        };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("[truncated]"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_TruncatesLongProperties()
    {
        SetupSuccessfulChatResponse("Analysis");
        var longProps = new string('y', 10000);
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error",
            Properties = longProps,
        };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("[truncated]"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_DoesNotTruncateShortException()
    {
        SetupSuccessfulChatResponse("Analysis");
        var shortException = "System.Exception: short";
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error",
            Exception = shortException,
        };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains(shortException) && !m.Text.Contains("[truncated]"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region Context and Frequency

    [Fact]
    public async Task Analyse_FetchesSurroundingContext_WhenTimestampIsValid()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult
            {
                Before = [new LogContextEntry { Level = "Information", Message = "Before log", Timestamp = DateTimeOffset.UtcNow.AddSeconds(-30) }],
                After = [],
            });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _logContext.Received(1).GetSurroundingLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_IncludesSurroundingContext_InPrompt()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult
            {
                Before = [new LogContextEntry { Level = "Information", Message = "Request started", Timestamp = DateTimeOffset.UtcNow.AddSeconds(-30) }],
                After = [new LogContextEntry { Level = "Error", Message = "Request failed", Timestamp = DateTimeOffset.UtcNow.AddSeconds(5) }],
            });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null
                    && m.Text.Contains("Request started")
                    && m.Text.Contains("Request failed")
                    && m.Text.Contains("Surrounding log entries"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_IncludesFrequencyNote_WhenCountGreaterThanOne()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult());
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("appeared 5 times"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_IncludesSystemicNote_WhenFrequencyGreaterThanTen()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult());
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(15);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("recurring/systemic issue"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_DoesNotIncludeFrequencyNote_WhenCountIsOne()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult());
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.All(m => m.Text == null || !m.Text.Contains("Error frequency"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_SkipsContextFetching_WhenTimestampIsMissing()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest { Level = "Error", Message = "Error occurred" };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _logContext.DidNotReceive().GetSurroundingLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_SkipsContextFetching_WhenTimestampIsInvalid()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error occurred",
            Timestamp = "not-a-date",
        };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _logContext.DidNotReceive().GetSurroundingLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_HandlesContextFetchFailure_Gracefully()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Log service unavailable"));
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Log service unavailable"));

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");

        var result = await _sut.Analyse(request, CancellationToken.None);

        // Should still succeed - context failure is logged but doesn't block analysis
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region System Diagnostics

    [Fact]
    public async Task Analyse_IncludesSystemContext_InPrompt()
    {
        _diagnostics.GetContext().Returns("Umbraco: 17.2.2\nDatabase provider: SQLite\n");
        SetupSuccessfulChatResponse("Analysis");
        var request = CreateValidRequest();

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("Umbraco: 17.2.2"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region AI Service Failures

    [Fact]
    public async Task Analyse_Returns502_WhenAiServiceThrows()
    {
        _chatService.GetChatResponseAsync(Arg.Any<IList<ChatMessage>>(), cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Provider timeout"));
        var request = CreateValidRequest();

        var result = await _sut.Analyse(request, CancellationToken.None);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Fact]
    public async Task Analyse_Returns502_WithHelpfulMessage()
    {
        _chatService.GetChatResponseAsync(Arg.Any<IList<ChatMessage>>(), cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Connection refused"));
        var request = CreateValidRequest();

        var result = await _sut.Analyse(request, CancellationToken.None);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.Value.Should().BeOfType<string>()
            .Which.Should().Contain("AI provider unavailable");
    }

    #endregion

    #region Prompt Structure

    [Fact]
    public async Task Analyse_SendsSystemMessage_WithUmbracoExpertRole()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = CreateValidRequest();

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs[0].Role == ChatRole.System
                && msgs[0].Text != null
                && msgs[0].Text.Contains("expert Umbraco CMS diagnostics assistant")),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_SendsUserMessage_WithLogEntry()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Failed to resolve content",
            Timestamp = "2025-06-15T12:00:00Z",
        };

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs[1].Role == ChatRole.User
                && msgs[1].Text != null
                && msgs[1].Text.Contains("Level: Error")
                && msgs[1].Text.Contains("Failed to resolve content")),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_PromptContainsRequiredHeadings()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = CreateValidRequest();

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null
                    && m.Text.Contains("## Summary")
                    && m.Text.Contains("## Cause")
                    && m.Text.Contains("## Recommended action"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_SendsExactlyTwoMessages()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = CreateValidRequest();

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs => msgs.Count == 2),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region FormatSurroundingLogs (tested through controller)

    [Fact]
    public async Task Analyse_FormatsBeforeEntries_WithOldestFirstLabel()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult
            {
                Before = [new LogContextEntry { Level = "Information", Message = "Setup done", Timestamp = DateTimeOffset.UtcNow.AddSeconds(-10) }],
                After = [],
            });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("entries before (oldest first)"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_FormatsAfterEntries_WithOldestFirstLabel()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult
            {
                Before = [],
                After = [new LogContextEntry { Level = "Warning", Message = "Cleanup started", Timestamp = DateTimeOffset.UtcNow.AddSeconds(10) }],
            });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("entries after (oldest first)"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_FormatsDeduplicatedEntries_WithRepeatCount()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult
            {
                Before = [new LogContextEntry { Level = "Error", Message = "Retry failed", Timestamp = DateTimeOffset.UtcNow, Count = 5 }],
                After = [],
            });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("(x5)"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_FormatsEntryException_FirstLineOnly()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult
            {
                Before = [new LogContextEntry
                {
                    Level = "Error",
                    Message = "Crash",
                    Timestamp = DateTimeOffset.UtcNow,
                    Exception = "System.Exception: boom\n   at Foo.Bar()\n   at Baz.Qux()",
                }],
                After = [],
            });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null
                    && m.Text.Contains("Exception: System.Exception: boom")
                    && !m.Text.Contains("at Foo.Bar()"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_NoSurroundingContext_WhenBothEmpty()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult { Before = [], After = [] });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.All(m => m.Text == null || !m.Text.Contains("Surrounding log entries"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_IncludesSelectedLogEntryMarker_InContext()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult
            {
                Before = [new LogContextEntry { Level = "Information", Message = "Before", Timestamp = DateTimeOffset.UtcNow }],
                After = [],
            });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("SELECTED LOG ENTRY"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_DoesNotShowRepeatCount_WhenCountIsOne()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult
            {
                Before = [new LogContextEntry { Level = "Error", Message = "Single", Timestamp = DateTimeOffset.UtcNow, Count = 1 }],
                After = [],
            });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("Single") && !m.Text.Contains("(x1)"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region Truncation Edge Cases

    [Fact]
    public async Task Analyse_DoesNotTruncate_AtExactMaxLength()
    {
        SetupSuccessfulChatResponse("Analysis");
        var exactLengthException = new string('z', 8192);
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error",
            Exception = exactLengthException,
        };

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && !m.Text.Contains("[truncated]"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_Truncates_AtMaxLengthPlusOne()
    {
        SetupSuccessfulChatResponse("Analysis");
        var overLengthException = new string('z', 8193);
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error",
            Exception = overLengthException,
        };

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("[truncated]"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region Frequency Boundary Cases

    [Fact]
    public async Task Analyse_DoesNotIncludeSystemicNote_WhenFrequencyExactlyTen()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult());
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(10);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null
                    && m.Text.Contains("appeared 10 times")
                    && !m.Text.Contains("recurring/systemic issue"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_IncludesSystemicNote_WhenFrequencyExactlyEleven()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult());
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(11);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("recurring/systemic issue"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_IncludesFrequencyNote_WhenCountExactlyTwo()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult());
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(2);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("appeared 2 times"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_DoesNotIncludeFrequencyNote_WhenCountIsZero()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult());
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.All(m => m.Text == null || !m.Text.Contains("Error frequency"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region Timestamp Edge Cases

    [Fact]
    public async Task Analyse_ParsesTimestampWithTimezoneOffset()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult());
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00+05:30");

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _logContext.Received(1).GetSurroundingLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_SkipsContextFetching_WhenTimestampIsWhitespace()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error occurred",
            Timestamp = "   ",
        };

        var result = await _sut.Analyse(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _logContext.DidNotReceive().GetSurroundingLogsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_IncludesTimestamp_InPromptWhenProvided()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Warning",
            Message = "Something happened",
            Timestamp = "2025-06-15T12:00:00Z",
        };

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("Timestamp: 2025-06-15T12:00:00Z"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_ExcludesTimestamp_FromPromptWhenEmpty()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Warning",
            Message = "Something happened",
            Timestamp = "",
        };

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.All(m => m.Text == null || !m.Text.Contains("Timestamp:"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region Whitespace-only Optional Fields

    [Fact]
    public async Task Analyse_ExcludesWhitespaceOnlyException_FromPrompt()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error",
            Exception = "   ",
        };

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.All(m => m.Text == null || !m.Text.Contains("Exception:"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_ExcludesWhitespaceOnlyProperties_FromPrompt()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error",
            Properties = "   ",
        };

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.All(m => m.Text == null || !m.Text.Contains("Properties:"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_ExcludesWhitespaceOnlyMessageTemplate_FromPrompt()
    {
        SetupSuccessfulChatResponse("Analysis");
        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Error",
            MessageTemplate = "   ",
        };

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.All(m => m.Text == null || !m.Text.Contains("Message template:"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region All Fields Populated

    [Fact]
    public async Task Analyse_IncludesAllFields_WhenAllProvided()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult
            {
                Before = [new LogContextEntry { Level = "Information", Message = "Setup", Timestamp = DateTimeOffset.UtcNow }],
                After = [],
            });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(3);

        var request = new LogAnalyserRequest
        {
            Level = "Error",
            Message = "Unhandled exception",
            Timestamp = "2025-06-15T12:00:00Z",
            MessageTemplate = "Unhandled exception in {Handler}",
            Exception = "System.NullReferenceException: Object reference not set",
            Properties = "Handler: ContentController",
        };

        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null
                    && m.Text.Contains("Level: Error")
                    && m.Text.Contains("Timestamp: 2025-06-15T12:00:00Z")
                    && m.Text.Contains("Message: Unhandled exception")
                    && m.Text.Contains("Message template: Unhandled exception in {Handler}")
                    && m.Text.Contains("Exception: System.NullReferenceException")
                    && m.Text.Contains("Properties: Handler: ContentController")
                    && m.Text.Contains("appeared 3 times")
                    && m.Text.Contains("Surrounding log entries"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region Prompt Context Instructions

    [Fact]
    public async Task Analyse_IncludesContextInstruction_WhenSurroundingLogsPresent()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult
            {
                Before = [new LogContextEntry { Level = "Information", Message = "Before", Timestamp = DateTimeOffset.UtcNow }],
                After = [],
            });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("Use the surrounding log entries to understand"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_ExcludesContextInstruction_WhenNoSurroundingLogs()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult { Before = [], After = [] });
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.All(m => m.Text == null || !m.Text.Contains("Use the surrounding log entries"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyse_IncludesFrequencyInstruction_WhenFrequencyPresent()
    {
        SetupSuccessfulChatResponse("Analysis");
        _logContext.GetSurroundingLogsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LogContextResult());
        _logContext.GetErrorFrequencyAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var request = CreateValidRequest(timestamp: "2025-06-15T12:00:00Z");
        await _sut.Analyse(request, CancellationToken.None);

        await _chatService.Received(1).GetChatResponseAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains("Consider the error frequency"))),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private static LogAnalyserRequest CreateValidRequest(string? timestamp = null) => new()
    {
        Level = "Error",
        Message = "An error occurred in the application",
        Timestamp = timestamp ?? "",
    };

    private void SetupSuccessfulChatResponse(string? text)
    {
        var assistantMessage = new ChatMessage(ChatRole.Assistant, text);
        var completion = new ChatResponse(assistantMessage);
        _chatService.GetChatResponseAsync(Arg.Any<IList<ChatMessage>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(completion);
    }

    #endregion
}

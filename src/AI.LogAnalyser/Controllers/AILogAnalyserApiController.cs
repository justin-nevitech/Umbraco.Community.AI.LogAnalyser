using System.Text;
using AI.LogAnalyser.Models;
using AI.LogAnalyser.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Chat;

namespace AI.LogAnalyser.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "AI.LogAnalyser")]
public class AILogAnalyserApiController : AILogAnalyserApiControllerBase
{
    private readonly IAIChatService _chatService;
    private readonly ISystemDiagnosticsProvider _diagnostics;
    private readonly ILogger<AILogAnalyserApiController> _logger;

    public AILogAnalyserApiController(
        IAIChatService chatService,
        ISystemDiagnosticsProvider diagnostics,
        ILogger<AILogAnalyserApiController> logger)
    {
        _chatService = chatService;
        _diagnostics = diagnostics;
        _logger = logger;
    }

    [HttpPost("analyse")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(LogAnalyserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Analyse([FromBody] LogAnalyserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        var logEntry = new StringBuilder();
        logEntry.AppendLine($"Level: {request.Level}");
        logEntry.AppendLine($"Timestamp: {request.Timestamp}");
        logEntry.AppendLine($"Message: {request.Message}");
        if (!string.IsNullOrWhiteSpace(request.MessageTemplate))
            logEntry.AppendLine($"Message template: {request.MessageTemplate}");
        if (!string.IsNullOrWhiteSpace(request.Exception))
            logEntry.AppendLine($"Exception: {request.Exception}");
        if (!string.IsNullOrWhiteSpace(request.Properties))
            logEntry.AppendLine($"Properties: {request.Properties}");

        var prompt = $"""
            Analyse the log entry below. Structure your response with these markdown headings:

            ## Summary
            A plain-language explanation of what happened. Do not restate the log message verbatim.

            ## Cause
            The likely root cause. If an exception with a stack trace is present, identify the originating method. For warnings and errors, consider common Umbraco issues (composition failures, content resolution, Examine indexing, middleware pipeline, dependency injection, database connectivity). For debug/verbose entries, keep this brief or omit.

            ## Recommended action
            A concrete next step the developer should take. Include relevant Umbraco documentation references, config settings, or code changes where applicable. For informational entries, this may simply be "No action required."

            Keep the entire response to 2-4 short paragraphs. Be direct and technical.

            ```
            {logEntry}
            ```

            System context:
            ```
            {_diagnostics.GetContext()}
            ```
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "You are an expert Umbraco CMS diagnostics assistant. You have deep knowledge of Umbraco's architecture, " +
                "common error patterns, Serilog structured logging, .NET middleware pipelines, and dependency injection. " +
                "Analyse log entries for Umbraco developers. Be concise, technical, and actionable. " +
                "Use the message template (if provided) to understand the structured logging intent behind the rendered message."),
            new(ChatRole.User, prompt)
        };

        try
        {
            var response = await _chatService.GetChatResponseAsync(messages, cancellationToken: ct);

            return Ok(new LogAnalyserResponse
            {
                Summary = response.Text ?? "No summary available."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI analysis failed");
            return StatusCode(StatusCodes.Status502BadGateway, "AI provider unavailable. Please check your AI provider configuration.");
        }
    }
}

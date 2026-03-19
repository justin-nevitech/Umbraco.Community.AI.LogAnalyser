using AI.LogAnalyser.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;

namespace AI.LogAnalyser.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "AI.LogAnalyser")]
public class AILogAnalyserApiController : AILogAnalyserApiControllerBase
{
    private readonly IAIChatService _chatService;
    private readonly ISystemDiagnosticsProvider _diagnostics;

    public AILogAnalyserApiController(IAIChatService chatService, ISystemDiagnosticsProvider diagnostics)
    {
        _chatService = chatService;
        _diagnostics = diagnostics;
    }

    [HttpPost("analyse")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(LogAnalyserRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Analyse([FromBody] LogAnalyserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        var prompt = $"""
            Please analyse this application log entry and provide:
            1. A plain-language summary of what happened
            2. The potential cause (if it's a warning or error)
            3. A suggested action or next step (if applicable)

            Keep your response concise and actionable. Use markdown formatting.

            --- System context ---
            {_diagnostics.GetContext()}
            --- Log entry ---
            Level: {request.Level}
            Timestamp: {request.Timestamp}
            Message: {request.Message}
            {(string.IsNullOrWhiteSpace(request.Exception) ? "" : $"Exception: {request.Exception}")}
            {(string.IsNullOrWhiteSpace(request.Properties) ? "" : $"Properties: {request.Properties}")}
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "You are a helpful assistant that analyses application log messages for Umbraco CMS developers. " +
                "Be concise, technical where appropriate, and focus on actionable insights."),
            new(ChatRole.User, prompt)
        };

        var response = await _chatService.GetChatResponseAsync(messages, cancellationToken: ct);

        return Ok(new LogAnalyserResponse
        {
            Summary = response.Text ?? "No summary available."
        });
    }
}

public class LogAnalyserRequest
{
    public string Level { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Properties { get; set; }
}

public class LogAnalyserResponse
{
    public string Summary { get; set; } = string.Empty;
}

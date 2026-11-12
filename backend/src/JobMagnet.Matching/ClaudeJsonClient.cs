using System.Text.Json;

using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobMagnet.Matching;

public partial class ClaudeJsonClient(AnthropicClient client, IOptions<ClaudeOptions> options, ILogger<ClaudeJsonClient> logger)
    : IClaudeJsonClient
{
    private const string RefusalStopReason = "refusal";

    [LoggerMessage(Level = LogLevel.Warning, Message = "Claude declined the request: {Category}")]
    private static partial void LogRefusal(ILogger logger, object? category);

    public async Task<string?> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyDictionary<string, JsonElement> schema,
        CancellationToken cancellationToken)
    {
        var response = await client.Messages.Create(
            new MessageCreateParams
            {
                Model = options.Value.Model,
                MaxTokens = options.Value.MaxTokens,
                System = systemPrompt,
                Messages = [new UserMessageParam { Content = userPrompt }],
                OutputConfig = new OutputConfig
                {
                    Effort = Effort.Low,
                    Format = new JsonOutputFormat { Schema = schema.ToDictionary() }
                }
            },
            cancellationToken).ConfigureAwait(false);

        if (response.StopReason == RefusalStopReason)
        {
            LogRefusal(logger, response.StopDetails?.Category);
            return null;
        }

        var text = response.Content
            .Select(block => block.TryPickText(out var textBlock) ? textBlock.Text : null)
            .FirstOrDefault(t => t is not null);

        return text;
    }
}

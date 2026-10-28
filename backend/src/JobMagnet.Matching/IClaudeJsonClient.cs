using System.Text.Json;

namespace JobMagnet.Matching;

public interface IClaudeJsonClient
{
    // Returns null when Claude declines the request, so callers can skip instead of failing.
    Task<string?> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyDictionary<string, JsonElement> schema,
        CancellationToken cancellationToken);
}

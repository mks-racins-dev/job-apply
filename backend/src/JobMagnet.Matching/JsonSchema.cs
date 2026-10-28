using System.Text.Json;

namespace JobMagnet.Matching;

public static class JsonSchema
{
    public static IReadOnlyDictionary<string, JsonElement> Parse(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson);
        return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
    }
}

namespace JobMagnet.Matching;

public class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string Model { get; set; } = "claude-opus-5-5";

    public int MaxTokens { get; set; } = 16000;
}

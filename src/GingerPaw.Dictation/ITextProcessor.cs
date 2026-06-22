namespace GingerPaw.Dictation;

/// <summary>Mirrors the Mac app's TextProcessor protocol — the LLM cleanup seam (Phase D, v2).</summary>
public interface ITextProcessor
{
    Task<string> FormatAsync(string text);
}

using System.Text;
using GingerPaw.Dictation;
using LLama;
using LLama.Common;
using LLama.Sampling;

namespace GingerPaw.TextProcessing;

/// <summary>
/// Port of the Mac app's MLXTextProcessor: reformats a raw transcript with a local
/// Qwen2.5-0.5B-Instruct model. Uses LLamaSharp's StatelessExecutor instead of Mac's
/// ChatSession — each call here is independent, so there's no conversation state to keep
/// around between dictations, per plan.md's design note.
/// </summary>
public sealed class LLamaSharpTextProcessor : ITextProcessor, IDisposable
{
    // Mirrors InferenceParams.AntiPrompts below. "<|im_end|>" is Qwen's own trained
    // end-of-turn marker for the ChatML format PromptBuilder now uses — the model should
    // stop there naturally. "<|im_start|>" guards against it instead starting a fake new
    // turn. AntiPrompt detection in LLamaSharp's streaming InferAsync can still leak part of
    // a match through before it stops, so this is a defensive belt-and-suspenders
    // truncation on top of the antiprompt stop list, not a replacement for it.
    private static readonly string[] StopMarkers = { "<|im_end|>", "<|im_start|>" };

    private readonly string _modelsDirectory;
    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private StatelessExecutor? _executor;

    public LLamaSharpTextProcessor(string modelsDirectory)
    {
        _modelsDirectory = modelsDirectory;
    }

    public async Task<string> FormatAsync(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return text;
        }

        var executor = await EnsureExecutorAsync();
        var prompt = PromptBuilder.Build(trimmed);

        var inferenceParams = new InferenceParams
        {
            MaxTokens = 400,
            AntiPrompts = new List<string>(StopMarkers),
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = 0.2f,
                // Without a repetition penalty this 0.5B model reliably loops the same
                // block forever once it reaches the end of a real answer — confirmed by a
                // pasted result that repeated 15+ times until MaxTokens cut it off.
                RepeatPenalty = 1.15f,
                PenaltyCount = 256
            }
        };

        var raw = new StringBuilder();
        await foreach (var token in executor.InferAsync(prompt, inferenceParams))
        {
            raw.Append(token);
        }

        var truncated = TruncateAtFirstStopMarker(raw.ToString());
        var cleaned = OutputCleaning.Clean(truncated, fallback: trimmed);

        // Belt-and-suspenders: if the model's output still doesn't look like a genuine
        // reformat of what was said (e.g. it leaked its own rule text), prefer pasting the
        // exact transcript over pasting something that doesn't reflect what was said.
        return OutputValidation.LooksValid(cleaned, trimmed) ? cleaned : trimmed;
    }

    private static string TruncateAtFirstStopMarker(string text)
    {
        var earliest = text.Length;
        foreach (var marker in StopMarkers)
        {
            var index = text.IndexOf(marker, StringComparison.Ordinal);
            if (index >= 0 && index < earliest)
            {
                earliest = index;
            }
        }
        return text[..earliest];
    }

    private async Task<StatelessExecutor> EnsureExecutorAsync()
    {
        if (_executor is not null)
        {
            return _executor;
        }

        var modelPath = await QwenModelProvisioner.EnsureModelAsync(_modelsDirectory);
        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 4096,
            GpuLayerCount = 0
        };

        _weights = LLamaWeights.LoadFromFile(parameters);
        _context = _weights.CreateContext(parameters);
        _executor = new StatelessExecutor(_weights, parameters);
        return _executor;
    }

    public void Dispose()
    {
        _context?.Dispose();
        _weights?.Dispose();
    }
}

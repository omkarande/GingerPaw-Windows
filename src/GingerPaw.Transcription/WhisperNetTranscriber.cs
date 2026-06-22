using Whisper.net;

namespace GingerPaw.Transcription;

/// <summary>
/// Transcribes a 16kHz mono WAV file with whisper.cpp (via Whisper.net) over a GGML model.
/// Parity with the Mac app's default model: whisper "base".
/// </summary>
public sealed class WhisperNetTranscriber : ISpeechTranscriber, IDisposable
{
    private readonly string _modelPath;
    private WhisperFactory? _factory;

    public WhisperNetTranscriber(string modelPath)
    {
        _modelPath = modelPath;
    }

    public async Task<string> TranscribeAsync(string audioPath)
    {
        _factory ??= WhisperFactory.FromPath(_modelPath);

        using var processor = _factory.CreateBuilder()
            .WithLanguage("auto")
            .Build();

        using var audioStream = File.OpenRead(audioPath);
        var segments = new List<string>();
        await foreach (var segment in processor.ProcessAsync(audioStream))
        {
            segments.Add(segment.Text);
        }

        return string.Join(" ", segments).Trim();
    }

    public void Dispose() => _factory?.Dispose();
}

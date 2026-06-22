using Whisper.net.Ggml;

namespace GingerPaw.Transcription;

/// <summary>Downloads the GGML Whisper model into modelsDirectory on first run; no-ops if already present.</summary>
public static class WhisperModelProvisioner
{
    public static async Task<string> EnsureBaseModelAsync(string modelsDirectory)
    {
        Directory.CreateDirectory(modelsDirectory);
        var path = Path.Combine(modelsDirectory, "ggml-base.bin");
        if (!File.Exists(path))
        {
            using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Base);
            using var fileStream = File.Create(path);
            await modelStream.CopyToAsync(fileStream);
        }
        return path;
    }
}

namespace GingerPaw.TextProcessing;

/// <summary>
/// Downloads the GGUF Qwen2.5-0.5B-Instruct model into modelsDirectory on first use; no-ops
/// if already present. Mirrors GingerPaw.Transcription.WhisperModelProvisioner's pattern,
/// but unlike that one this is only ever called lazily from inside LLamaSharpTextProcessor's
/// first real FormatAsync call — never at app startup — since AI formatting defaults off and
/// most users never enable it (same "ship-with-app or download on first enable" split plan.md
/// calls for, just without the bundling half implemented yet).
/// </summary>
public static class QwenModelProvisioner
{
    private const string FileName = "Qwen2.5-0.5B-Instruct-Q4_K_M.gguf";
    private const string DownloadUrl =
        "https://huggingface.co/bartowski/Qwen2.5-0.5B-Instruct-GGUF/resolve/main/" + FileName;

    public static async Task<string> EnsureModelAsync(string modelsDirectory)
    {
        Directory.CreateDirectory(modelsDirectory);
        var path = Path.Combine(modelsDirectory, FileName);
        if (!File.Exists(path))
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using var modelStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(path);
            await modelStream.CopyToAsync(fileStream);
        }
        return path;
    }
}

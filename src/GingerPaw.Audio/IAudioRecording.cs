namespace GingerPaw.Audio;

/// <summary>
/// Mirrors the Mac app's AudioRecording protocol: start/stop/cancel a mic capture,
/// stop returns the path to a finished, fully-flushed WAV file ready to hand to a transcriber.
/// </summary>
public interface IAudioRecording
{
    void Start();
    string Stop();
    void Cancel();
}

public sealed class AudioRecordingException(string message) : Exception(message);

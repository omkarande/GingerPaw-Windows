namespace GingerPaw.Transcription;

/// <summary>Mirrors the Mac app's SpeechTranscriber protocol: one WAV file in, one transcript out.</summary>
public interface ISpeechTranscriber
{
    Task<string> TranscribeAsync(string audioPath);
}

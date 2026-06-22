using GingerPaw.TextInsertion;

namespace GingerPaw.Dictation;

/// <summary>
/// Port of the Mac app's DictationState enum. A record hierarchy is the closest C# analog
/// to Swift's associated-value enum (Recording carries StartedAt, Failed carries a message).
/// </summary>
public abstract record DictationState
{
    public bool IsBusy => this is Recording or Processing or Inserting;
    public bool CanStartRecording => !IsBusy;

    public sealed record Idle : DictationState;
    public sealed record Recording(DateTimeOffset StartedAt) : DictationState;
    public sealed record Processing : DictationState;
    public sealed record Inserting : DictationState;
    public sealed record Copied : DictationState;
    public sealed record Failed(string Message) : DictationState;
}

public sealed record DictationResult(
    string Transcript,
    TimeSpan Duration,
    string ModelId,
    InsertionOutcome InsertionOutcome);

/// <summary>Port of the Mac app's DictationError.emptyTranscript.</summary>
public sealed class EmptyTranscriptException() : Exception("Transcript was empty.");

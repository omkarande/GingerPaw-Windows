using GingerPaw.Audio;
using GingerPaw.Settings;
using GingerPaw.TextInsertion;
using GingerPaw.Transcription;

namespace GingerPaw.Dictation;

/// <summary>
/// Port of the Mac app's DictationCoordinator — the state machine driving every dictation.
/// Same state graph, same method names, same callback shape (OnStateChange), so the wiring
/// in the composition root reads the same way it does in AppRuntime.swift.
/// </summary>
public sealed class DictationCoordinator
{
    private readonly IAudioRecording _recorder;
    private readonly ISpeechTranscriber _transcriber;
    private readonly ITextInserter _inserter;
    private readonly ITextProcessor? _processor;
    private readonly GingerPawSettings _settings;
    private DateTimeOffset? _startedAt;

    public DictationState State { get; private set; } = new DictationState.Idle();
    public DictationResult? LastResult { get; private set; }
    public string? LastError { get; private set; }
    public Action<DictationState>? OnStateChange { get; set; }

    // TEMPORARY DIAGNOSTIC — exposes the raw Whisper transcript (before any LLM
    // reformatting) so it can be compared against LastResult.Transcript while diagnosing
    // whether a mismatch comes from Whisper mistranscribing or the LLM hallucinating on
    // top of a correct transcript. Delete alongside the other TEMPORARY DIAGNOSTIC markers
    // once Phase D's output quality is settled.
    public string? LastRawTranscript { get; private set; }

    public DictationCoordinator(
        IAudioRecording recorder,
        ISpeechTranscriber transcriber,
        ITextInserter inserter,
        GingerPawSettings settings,
        ITextProcessor? processor = null)
    {
        _recorder = recorder;
        _transcriber = transcriber;
        _inserter = inserter;
        _settings = settings;
        _processor = processor;
    }

    public void StartRecording()
    {
        if (!State.CanStartRecording) return;
        try
        {
            var now = DateTimeOffset.UtcNow;
            _startedAt = now;
            _recorder.Start();
            LastError = null;
            SetState(new DictationState.Recording(now));
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    /// <summary>
    /// Fire-and-forget by design, mirroring the Mac coordinator's detached Task{} — callers
    /// (the hotkey release handler) call this synchronously and don't await it. Every
    /// exception inside is caught and routed to Fail(), so nothing escapes unobserved.
    /// </summary>
    public async void StopRecordingAndProcess()
    {
        if (State is not DictationState.Recording) return;
        SetState(new DictationState.Processing());

        try
        {
            var audioPath = _recorder.Stop();
            var transcript = (await _transcriber.TranscribeAsync(audioPath)).Trim();
            LastRawTranscript = transcript;
            if (string.IsNullOrEmpty(transcript))
            {
                throw new EmptyTranscriptException();
            }

            var finalText = await Structure(transcript);

            SetState(new DictationState.Inserting());
            var outcome = await Insert(finalText);
            var duration = _startedAt.HasValue ? DateTimeOffset.UtcNow - _startedAt.Value : TimeSpan.Zero;
            LastResult = new DictationResult(finalText, duration, _settings.ModelId, outcome);

            SetState(outcome == InsertionOutcome.Pasted ? new DictationState.Idle() : new DictationState.Copied());
            if (outcome != InsertionOutcome.Pasted)
            {
                ResetIdleSoon();
            }
            _startedAt = null;
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    public void CancelRecording()
    {
        _recorder.Cancel();
        _startedAt = null;
        SetState(new DictationState.Idle());
    }

    private async Task<string> Structure(string transcript)
    {
        if (!_settings.FormatEnabled || _processor is null) return transcript;
        try
        {
            return await _processor.FormatAsync(transcript);
        }
        catch
        {
            return transcript;
        }
    }

    private async Task<InsertionOutcome> Insert(string transcript)
    {
        if (!_settings.AutoPaste)
        {
            return await _inserter.CopyAsync(transcript);
        }
        return await _inserter.InsertAsync(transcript, _settings.RestoreClipboard);
    }

    private void Fail(Exception ex)
    {
        _recorder.Cancel();
        LastError = ex.Message;
        _startedAt = null;
        SetState(new DictationState.Failed(ex.Message));
        ResetIdleSoon();
    }

    private void SetState(DictationState next)
    {
        State = next;
        OnStateChange?.Invoke(next);
    }

    private void ResetIdleSoon() => _ = ResetIdleSoonAsync();

    private async Task ResetIdleSoonAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1.4));
        if (State is DictationState.Copied or DictationState.Failed)
        {
            SetState(new DictationState.Idle());
        }
    }
}

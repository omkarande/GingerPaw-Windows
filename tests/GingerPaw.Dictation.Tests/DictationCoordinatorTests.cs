using System.IO;
using GingerPaw.Audio;
using GingerPaw.Dictation;
using GingerPaw.Settings;
using GingerPaw.TextInsertion;
using GingerPaw.Transcription;

namespace GingerPaw.Dictation.Tests;

/// <summary>Port of DictationCoordinatorTests.swift's four scenarios, same stub substitution pattern.</summary>
public class DictationCoordinatorTests
{
    [Fact]
    public void PressStartsRecording()
    {
        var coordinator = MakeCoordinator();
        coordinator.StartRecording();

        Assert.IsType<DictationState.Recording>(coordinator.State);
    }

    [Fact]
    public async Task ReleaseProcessesAndPastesTranscript()
    {
        var coordinator = MakeCoordinator(transcript: "hello world", insertionOutcome: InsertionOutcome.Pasted);
        coordinator.StartRecording();
        coordinator.StopRecordingAndProcess();

        await WaitUntil(() => coordinator.State is DictationState.Idle);
        Assert.Equal("hello world", coordinator.LastResult?.Transcript);
        Assert.Equal(InsertionOutcome.Pasted, coordinator.LastResult?.InsertionOutcome);
    }

    [Fact]
    public async Task TranscriptionFailureReturnsFailedState()
    {
        var coordinator = MakeCoordinator(error: new InvalidOperationException("failed"));
        coordinator.StartRecording();
        coordinator.StopRecordingAndProcess();

        await WaitUntil(() => coordinator.State is DictationState.Failed);
    }

    [Fact]
    public async Task CopyFallbackIsRecorded()
    {
        var coordinator = MakeCoordinator(transcript: "fallback", insertionOutcome: InsertionOutcome.Copied);
        coordinator.StartRecording();
        coordinator.StopRecordingAndProcess();

        await WaitUntil(() => coordinator.State is DictationState.Copied);
        Assert.Equal(InsertionOutcome.Copied, coordinator.LastResult?.InsertionOutcome);
    }

    private static DictationCoordinator MakeCoordinator(
        string transcript = "ok",
        InsertionOutcome insertionOutcome = InsertionOutcome.Pasted,
        Exception? error = null)
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), $"gingerpaw-tests-{Guid.NewGuid():N}.json");
        return new DictationCoordinator(
            recorder: new StubRecorder(),
            transcriber: new StubTranscriber(transcript, error),
            inserter: new StubInserter(insertionOutcome),
            settings: new GingerPawSettings(settingsPath));
    }

    private static async Task WaitUntil(Func<bool> predicate)
    {
        for (var i = 0; i < 50; i++)
        {
            if (predicate()) return;
            await Task.Delay(20);
        }
        Assert.Fail("Condition was not met within the timeout.");
    }

    private sealed class StubRecorder : IAudioRecording
    {
        public void Start() { }
        public string Stop() => "C:\\temp\\audio.wav";
        public void Cancel() { }
    }

    private sealed class StubTranscriber(string transcript, Exception? error) : ISpeechTranscriber
    {
        public Task<string> TranscribeAsync(string audioPath)
        {
            if (error is not null) throw error;
            return Task.FromResult(transcript);
        }
    }

    private sealed class StubInserter(InsertionOutcome outcome) : ITextInserter
    {
        public Task<InsertionOutcome> InsertAsync(string text, bool restoreClipboard) => Task.FromResult(outcome);
        public Task<InsertionOutcome> CopyAsync(string text) => Task.FromResult(InsertionOutcome.Copied);
    }
}

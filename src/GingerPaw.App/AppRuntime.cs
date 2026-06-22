using System.Windows;
using System.Windows.Threading;

namespace GingerPaw.App;

/// <summary>
/// Port of the Mac app's AppRuntime: wires hotkeyMonitor.OnPress/OnRelease to the
/// coordinator, and coordinator state changes to the tray icon.
///
/// Hotkey callbacks fire on the hook's dedicated background thread (see
/// RightCtrlHotkeyMonitor), but the coordinator's insertion path eventually needs
/// System.Windows.Clipboard, which requires the STA UI thread. Dispatcher.BeginInvoke
/// marshals each callback onto that thread — once there, every `await` inside
/// DictationCoordinator resumes back on it automatically via the Dispatcher's
/// SynchronizationContext, unlike the console Phase A harness which had no such context.
/// </summary>
public sealed class AppRuntime
{
    private readonly AppServices _services;
    private readonly Dispatcher _dispatcher;
    private bool _started;

    public AppRuntime(AppServices services)
    {
        _services = services;
        _dispatcher = Application.Current.Dispatcher;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;

        _services.Coordinator.OnStateChange = state =>
        {
            _services.Tray.Update(state);
            _services.Overlay.Update(state, _services.Settings.ShowPill);
            App.Log($"[state] {Describe(state)}");
        };
        _services.Tray.Update(_services.Coordinator.State);

        _services.HotkeyMonitor.OnPress = () =>
        {
            App.Log("[hotkey] Right Ctrl down -> StartRecording");
            _dispatcher.BeginInvoke(() => _services.Coordinator.StartRecording());
        };
        _services.HotkeyMonitor.OnRelease = () =>
        {
            App.Log("[hotkey] Right Ctrl up -> StopRecordingAndProcess");
            _dispatcher.BeginInvoke(() => _services.Coordinator.StopRecordingAndProcess());
        };

        _services.HotkeyMonitor.Start();

        // Unknown is left alone (Windows hasn't asked yet / first run isn't actionable);
        // only Denied is worth interrupting the user about.
        if (_services.Permissions.MicrophoneStatus == GingerPaw.Permissions.MicrophoneAuthorization.Denied)
        {
            _services.Tray.ShowMicrophoneDeniedNotification();
        }
    }

    public void Stop() => _services.HotkeyMonitor.Stop();

    // TEMPORARY DIAGNOSTIC — see App.Log; delete alongside it.
    private string Describe(GingerPaw.Dictation.DictationState state)
    {
        var coordinator = _services.Coordinator;
        return state switch
        {
            GingerPaw.Dictation.DictationState.Idle when coordinator.LastResult is { } r =>
                $"Idle (raw transcript: \"{coordinator.LastRawTranscript}\", formatted: \"{r.Transcript}\", outcome={r.InsertionOutcome}, took {r.Duration.TotalMilliseconds:F0}ms)",
            GingerPaw.Dictation.DictationState.Idle => "Idle",
            GingerPaw.Dictation.DictationState.Recording => "Recording...",
            GingerPaw.Dictation.DictationState.Processing => "Processing (transcribing)...",
            GingerPaw.Dictation.DictationState.Inserting => "Inserting (pasting)...",
            GingerPaw.Dictation.DictationState.Copied => "Copied to clipboard",
            GingerPaw.Dictation.DictationState.Failed f => $"Failed: {f.Message}",
            _ => state.ToString() ?? "?"
        };
    }
}

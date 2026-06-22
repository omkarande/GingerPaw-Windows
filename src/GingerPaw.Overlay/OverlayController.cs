using GingerPaw.Dictation;

namespace GingerPaw.Overlay;

/// <summary>
/// Port of the Mac app's DictationOverlayController: shows/hides and updates the floating
/// pill based on dictation state. Diverges from the Mac source in one place — Failed is
/// shown (briefly, in red) rather than hidden, since a visible failure cue is more useful
/// while this port is still being hardened. Everything else matches Mac's visibility guard
/// (`state.isBusy || state == .copied`) and visual mapping (icon/title/color per state).
/// </summary>
public sealed class OverlayController
{
    private PillOverlayWindow? _window;

    public void Update(DictationState state, bool visible)
    {
        var shouldShow = visible && (state.IsBusy || state is DictationState.Copied or DictationState.Failed);
        if (!shouldShow)
        {
            _window?.Hide();
            return;
        }

        var window = _window ??= new PillOverlayWindow();
        Apply(window, state);
        window.Show();
    }

    private static void Apply(PillOverlayWindow window, DictationState state)
    {
        window.StopPurr();
        switch (state)
        {
            case DictationState.Recording:
                window.Apply("Recording", PillIcon.Paw, danger: true);
                window.StartPurr();
                break;
            case DictationState.Processing:
                window.Apply("Transcribing", PillIcon.Bars, danger: false);
                break;
            case DictationState.Inserting:
                window.Apply("Pasting", PillIcon.Download, danger: false);
                break;
            case DictationState.Copied:
                window.Apply("Copied", PillIcon.Checkmark, danger: false);
                window.PlayBounce();
                break;
            case DictationState.Failed:
                window.Apply("Failed", PillIcon.Warning, danger: true);
                window.PlayBounce();
                break;
            default:
                window.Apply("Ready", PillIcon.Mic, danger: false);
                break;
        }
    }
}

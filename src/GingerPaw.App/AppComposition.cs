using System.IO;
using GingerPaw.Audio;
using GingerPaw.Dictation;
using GingerPaw.Hotkeys;
using GingerPaw.Overlay;
using GingerPaw.Permissions;
using GingerPaw.Settings;
using GingerPaw.TextInsertion;
using GingerPaw.TextProcessing;
using GingerPaw.Transcription;

namespace GingerPaw.App;

/// <summary>Bag of every constructed service, mirroring the Mac app's AppServices struct.</summary>
public sealed class AppServices
{
    public required GingerPawSettings Settings { get; init; }
    public required PermissionCenter Permissions { get; init; }
    public required DictationCoordinator Coordinator { get; init; }
    public required RightCtrlHotkeyMonitor HotkeyMonitor { get; init; }
    public required TrayController Tray { get; init; }
    public required OverlayController Overlay { get; init; }
}

/// <summary>
/// Port of the Mac app's AppComposition.make(): constructs every concrete implementation
/// and injects it into DictationCoordinator via its constructor-injected interfaces.
/// Async because ensuring the Whisper model is present may need to download it.
/// </summary>
public static class AppComposition
{
    public static async Task<AppServices> MakeAsync()
    {
        var settings = new GingerPawSettings();
        var modelPath = await WhisperModelProvisioner.EnsureBaseModelAsync(ResolveModelsDirectory("whisper"));

        var coordinator = new DictationCoordinator(
            recorder: new NAudioRecorder(),
            transcriber: new WhisperNetTranscriber(modelPath),
            inserter: new ClipboardTextInserter(),
            settings: settings,
            processor: new LLamaSharpTextProcessor(ResolveModelsDirectory("qwen")));

        var permissions = new PermissionCenter();

        return new AppServices
        {
            Settings = settings,
            Permissions = permissions,
            Coordinator = coordinator,
            HotkeyMonitor = new RightCtrlHotkeyMonitor(),
            Tray = new TrayController(coordinator, settings, permissions),
            Overlay = new OverlayController()
        };
    }

    private static string ResolveModelsDirectory(string leaf)
    {
        // bin/Debug/net8.0-windows -> walk up to the repo root, then into models/<leaf>.
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "models", leaf);
    }
}

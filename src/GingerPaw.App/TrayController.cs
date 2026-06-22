using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GingerPaw.Dictation;
using GingerPaw.Permissions;
using GingerPaw.Settings;
using H.NotifyIcon;
using H.NotifyIcon.Core;

namespace GingerPaw.App;

/// <summary>
/// Windows tray equivalent of the Mac app's StatusBarController: same menu shape (live
/// status, hotkey/model info, quit). The Mac version also has "Enable Hotkey" / "Request
/// Accessibility" items that request macOS-only TCC permissions — there's nothing to ask
/// for on Windows (see GingerPaw.Permissions), so those items are simply omitted here
/// rather than wired to a fake no-op.
/// </summary>
public sealed class TrayController : IDisposable
{
    private readonly TaskbarIcon _icon;
    private readonly MenuItem _statusMenuItem;
    private readonly GingerPawSettings _settings;
    private readonly PermissionCenter _permissions;
    private SettingsWindow? _settingsWindow;

    public TrayController(DictationCoordinator coordinator, GingerPawSettings settings, PermissionCenter permissions)
    {
        _settings = settings;
        _permissions = permissions;
        _statusMenuItem = new MenuItem { Header = "GingerPaw Ready", IsEnabled = false };

        var menu = new ContextMenu();
        menu.Items.Add(_statusMenuItem);
        menu.Items.Add(new Separator());

        // Mirrors StatusBarController.swift's order: the primary action item sits right
        // below the status line, ahead of the static Hotkey/Model info rows. "Settings…" is
        // this app's equivalent of the Mac's "Open GingerPaw" — there's no separate main
        // window here (see AppCore composition notes in CLAUDE.md), so Settings is the one
        // surface to open.
        var settingsItem = new MenuItem { Header = "Settings…" };
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new MenuItem { Header = "Hotkey: Right Ctrl", IsEnabled = false });
        menu.Items.Add(new MenuItem { Header = $"Model: {settings.ModelId}", IsEnabled = false });
        menu.Items.Add(new Separator());

        var quitItem = new MenuItem { Header = "Quit GingerPaw" };
        quitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(quitItem);

        _icon = new TaskbarIcon
        {
            // IconSource (WIC-based) rather than the legacy Icon (classic GDI+ System.Drawing.Icon)
            // property — GDI+'s Icon loader can't reliably decode the PNG-compressed .ico frames
            // AppIcon.ico uses, confirmed directly while building that file; WIC handles them fine.
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/AppIcon.ico")),
            ToolTipText = "GingerPaw",
            ContextMenu = menu,
            Visibility = Visibility.Visible
        };
        _icon.ForceCreate(enablesEfficiencyMode: false);

        Update(coordinator.State);
    }

    private void OpenSettings()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(_settings, _permissions);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    public void ShowMicrophoneDeniedNotification() => _icon.ShowNotification(
        "GingerPaw can't access your microphone",
        "Open Settings → Permissions to grant microphone access, or dictation won't work.",
        NotificationIcon.Warning);

    /// <summary>Temporary diagnostic — confirms whether Shell_NotifyIcon actually registered.</summary>
    public bool IsCreated => _icon.IsCreated;

    public void Update(DictationState state)
    {
        var title = StatusTitle(state);
        _icon.ToolTipText = title;
        _statusMenuItem.Header = title;
    }

    public void Dispose() => _icon.Dispose();

    private static string StatusTitle(DictationState state) => state switch
    {
        DictationState.Idle => "GingerPaw Ready",
        DictationState.Recording => "GingerPaw Recording",
        DictationState.Processing => "GingerPaw Processing",
        DictationState.Inserting => "GingerPaw Pasting",
        DictationState.Copied => "GingerPaw Copied",
        DictationState.Failed failed => $"GingerPaw Failed: {failed.Message}",
        _ => "GingerPaw"
    };
}

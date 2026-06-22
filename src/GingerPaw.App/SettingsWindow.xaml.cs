using System.Windows;
using System.Windows.Media;
using GingerPaw.Permissions;
using GingerPaw.Settings;

namespace GingerPaw.App;

/// <summary>
/// Settings + Permissions UI, opened from the tray menu. Plain code-behind reading/writing
/// GingerPawSettings properties directly — this codebase has no MVVM/binding infrastructure
/// anywhere else (TrayController wires events the same way), so this stays consistent with
/// that rather than introducing a new pattern just for this window.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly GingerPawSettings _settings;
    private readonly PermissionCenter _permissions;
    private bool _loaded;

    public SettingsWindow(GingerPawSettings settings, PermissionCenter permissions)
    {
        InitializeComponent();
        _settings = settings;
        _permissions = permissions;
        LoadCurrentValues();
    }

    private void LoadCurrentValues()
    {
        AutoPasteCheckBox.IsChecked = _settings.AutoPaste;
        RestoreClipboardCheckBox.IsChecked = _settings.RestoreClipboard;
        RestoreClipboardCheckBox.IsEnabled = _settings.AutoPaste;
        FormatEnabledCheckBox.IsChecked = _settings.FormatEnabled;
        ShowPillCheckBox.IsChecked = _settings.ShowPill;
        LaunchAtStartupCheckBox.IsChecked = AutostartManager.IsEnabled;

        var (text, color) = _permissions.MicrophoneStatus switch
        {
            MicrophoneAuthorization.Allowed => ("Allowed", Color.FromRgb(0x34, 0xC7, 0x59)),
            MicrophoneAuthorization.Denied => ("Denied — GingerPaw can't record without this", Color.FromRgb(0xFF, 0x3B, 0x30)),
            _ => ("Unknown — Windows hasn't asked yet", Color.FromRgb(0xFF, 0x95, 0x00))
        };
        MicrophoneStatusText.Text = text;
        MicrophoneStatusText.Foreground = new SolidColorBrush(color);
        MicrophoneStatusPill.Background = new SolidColorBrush(Color.FromArgb(0x26, color.R, color.G, color.B));

        // Setting IsChecked above already raised Checked/Unchecked for each box; only start
        // persisting once the form is actually populated, not while we're populating it.
        _loaded = true;
    }

    private void SettingsNavButton_Checked(object sender, RoutedEventArgs e)
    {
        // IsChecked="True" in XAML fires this Checked event mid-parse, before InitializeComponent
        // has finished assigning the fields below — bail out on that first synthetic firing;
        // the XAML defaults (SettingsPage visible, PermissionsPage collapsed) already match.
        if (SettingsPage is null || PermissionsPage is null || HeaderTitle is null || HeaderSubtitle is null) return;
        SettingsPage.Visibility = Visibility.Visible;
        PermissionsPage.Visibility = Visibility.Collapsed;
        HeaderTitle.Text = "Settings";
        HeaderSubtitle.Text = "Tune how dictation behaves and lands.";
    }

    private void PermissionsNavButton_Checked(object sender, RoutedEventArgs e)
    {
        if (SettingsPage is null || PermissionsPage is null || HeaderTitle is null || HeaderSubtitle is null) return;
        SettingsPage.Visibility = Visibility.Collapsed;
        PermissionsPage.Visibility = Visibility.Visible;
        HeaderTitle.Text = "Permissions";
        HeaderSubtitle.Text = "Manage the access GingerPaw needs to work.";
    }

    private void AutoPasteCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        _settings.AutoPaste = AutoPasteCheckBox.IsChecked == true;
        RestoreClipboardCheckBox.IsEnabled = _settings.AutoPaste;
    }

    private void RestoreClipboardCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        _settings.RestoreClipboard = RestoreClipboardCheckBox.IsChecked == true;
    }

    private void FormatEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        _settings.FormatEnabled = FormatEnabledCheckBox.IsChecked == true;
    }

    private void ShowPillCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        _settings.ShowPill = ShowPillCheckBox.IsChecked == true;
    }

    private void LaunchAtStartupCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        AutostartManager.SetEnabled(LaunchAtStartupCheckBox.IsChecked == true);
    }

    private void OpenMicSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _permissions.OpenMicrophoneSettings();
    }
}

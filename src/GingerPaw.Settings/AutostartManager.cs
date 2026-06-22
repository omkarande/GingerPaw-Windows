using System.Diagnostics;
using Microsoft.Win32;

namespace GingerPaw.Settings;

/// <summary>
/// Launch-at-login toggle, per plan.md's Phase E "LaunchAtStartup" item. Registry-based
/// (HKCU\...\Run) rather than a Startup-folder .lnk shortcut — simpler, no COM/WshShell
/// dependency needed. The registry is the single source of truth (no settings.json field to
/// keep in sync): callers should read IsEnabled fresh rather than cache it.
/// </summary>
public static class AutostartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "GingerPaw";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is not null;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            var exePath = Process.GetCurrentProcess().MainModule!.FileName;
            key.SetValue(ValueName, exePath);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}

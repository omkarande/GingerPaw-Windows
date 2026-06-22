using System.Diagnostics;
using Microsoft.Win32;

namespace GingerPaw.Permissions;

public enum MicrophoneAuthorization
{
    Allowed,
    Denied,
    Unknown
}

/// <summary>
/// Mic privacy check only — mirrors the Mac app's PermissionCenter, but Windows has no
/// equivalent of Input Monitoring (CGPreflightListenEventAccess) or Accessibility
/// (AXIsProcessTrusted): the keyboard hook and SendInput need no such grant on Windows.
/// UI built on this should say so plainly rather than show fake toggles for them.
/// </summary>
public sealed class PermissionCenter
{
    private const string ConsentStoreKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone";

    public MicrophoneAuthorization MicrophoneStatus
    {
        get
        {
            // Non-packaged desktop apps (we are one) are gated by the "Let desktop apps
            // access your microphone" toggle, stored under \NonPackaged; fall back to the
            // top-level key (the master "Microphone access" toggle) if that subkey is absent.
            return ReadConsent(ConsentStoreKey + @"\NonPackaged")
                ?? ReadConsent(ConsentStoreKey)
                ?? MicrophoneAuthorization.Unknown;
        }
    }

    /// <summary>
    /// Windows has no programmatic consent prompt for unpackaged desktop apps (unlike
    /// AVCaptureDevice.requestAccess on macOS) — the only way to grant access is through
    /// Settings, so this opens the microphone privacy page instead of returning a result.
    /// </summary>
    public void RequestMicrophone() => OpenMicrophoneSettings();

    public void OpenMicrophoneSettings()
    {
        Process.Start(new ProcessStartInfo("ms-settings:privacy-microphone") { UseShellExecute = true });
    }

    private static MicrophoneAuthorization? ReadConsent(string subKeyPath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(subKeyPath);
        var value = key?.GetValue("Value") as string;
        return value switch
        {
            "Allow" => MicrophoneAuthorization.Allowed,
            "Deny" => MicrophoneAuthorization.Denied,
            _ => null
        };
    }
}

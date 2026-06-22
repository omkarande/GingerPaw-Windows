using System.Runtime.InteropServices;
using System.Windows;

namespace GingerPaw.TextInsertion;

/// <summary>
/// Sets the clipboard and posts a synthetic Ctrl+V via SendInput. Mirrors the Mac app's
/// ClipboardTextInserter: snapshot clipboard, set text, paste, optionally restore.
/// Must be called from an STA thread (WPF's dispatcher thread, or a console Main marked [STAThread]) —
/// System.Windows.Clipboard requires it.
/// </summary>
public sealed class ClipboardTextInserter : ITextInserter
{
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private readonly TimeSpan _pasteDelay;

    public ClipboardTextInserter(TimeSpan? pasteDelay = null)
    {
        _pasteDelay = pasteDelay ?? TimeSpan.FromMilliseconds(150);
    }

    public async Task<InsertionOutcome> InsertAsync(string text, bool restoreClipboard)
    {
        var previous = restoreClipboard ? SnapshotClipboard() : null;

        SetClipboard(text);
        var pasted = SendPaste();

        if (restoreClipboard)
        {
            await Task.Delay(_pasteDelay);
            RestoreClipboard(previous);
        }

        return pasted ? InsertionOutcome.Pasted : InsertionOutcome.Copied;
    }

    public Task<InsertionOutcome> CopyAsync(string text)
    {
        SetClipboard(text);
        return Task.FromResult(InsertionOutcome.Copied);
    }

    private static string? SnapshotClipboard() =>
        Clipboard.ContainsText() ? Clipboard.GetText() : null;

    private static void RestoreClipboard(string? previous)
    {
        Clipboard.Clear();
        if (previous is not null)
        {
            Clipboard.SetText(previous);
        }
    }

    private static void SetClipboard(string text)
    {
        Clipboard.Clear();
        Clipboard.SetText(text);
    }

    /// <summary>Sends the 4-event Ctrl+V sequence (down/down/up/up) — SendInput has no
    /// combined-modifier-flag concept like macOS's CGEvent.flags, so each key is its own event.</summary>
    private static bool SendPaste()
    {
        var inputs = new INPUT[4];
        inputs[0] = KeyInput(VK_CONTROL, down: true);
        inputs[1] = KeyInput(VK_V, down: true);
        inputs[2] = KeyInput(VK_V, down: false);
        inputs[3] = KeyInput(VK_CONTROL, down: false);

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != inputs.Length)
        {
            var error = Marshal.GetLastWin32Error();
            Console.Error.WriteLine($"[paste] SendInput sent {sent}/{inputs.Length} events, GetLastError={error}");
        }
        return sent == inputs.Length;
    }

    private static INPUT KeyInput(ushort vk, bool down) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = down ? 0 : KEYEVENTF_KEYUP,
                time = 0,
                dwExtraInfo = nint.Zero
            }
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    // The union must be sized to fit its largest member (MOUSEINPUT), matching the real
    // Win32 INPUT struct ABI — a union with only KEYBDINPUT undersizes the struct (28 vs.
    // 32 bytes on x64), so SendInput rejects cbSize and the call silently fails (returns 0),
    // which is exactly what produced the "Copied" instead of "Pasted" fallback.
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}

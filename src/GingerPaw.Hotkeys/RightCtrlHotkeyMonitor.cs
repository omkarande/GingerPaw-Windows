using System.Runtime.InteropServices;

namespace GingerPaw.Hotkeys;

/// <summary>
/// Global low-level keyboard hook that fires <see cref="OnPress"/>/<see cref="OnRelease"/>
/// when Right Ctrl is held/released. Mirrors the Mac app's RightOptionHotkeyMonitor:
/// same public shape (onPress/onRelease, Start/Stop), different OS primitive
/// (WH_KEYBOARD_LL instead of CGEventTap; no Input Monitoring permission to wait on).
/// </summary>
public sealed class RightCtrlHotkeyMonitor : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_QUIT = 0x0012;
    private const int VK_RCONTROL = 0xA3;

    public Action? OnPress { get; set; }
    public Action? OnRelease { get; set; }

    private LowLevelKeyboardProc? _proc;
    private Thread? _hookThread;
    private uint _hookThreadId;
    private nint _hookHandle;
    private bool _isPressed;

    public void Start()
    {
        if (_hookThread is not null) return;

        _hookThread = new Thread(RunHookThread)
        {
            IsBackground = true,
            Name = "GingerPaw.HotkeyHook"
        };
        _hookThread.Start();
    }

    public void Stop()
    {
        if (_hookThread is null) return;

        PostThreadMessage(_hookThreadId, WM_QUIT, nint.Zero, nint.Zero);
        _hookThread.Join();
        _hookThread = null;
        _isPressed = false;
    }

    private void RunHookThread()
    {
        _hookThreadId = GetCurrentThreadId();

        // Keep a strong reference for the lifetime of the hook so the GC can't collect
        // the delegate while native code still holds a pointer to it.
        _proc = HookCallback;
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, nint.Zero, 0);

        var msg = new MSG();
        while (GetMessage(ref msg, nint.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        if (_hookHandle != nint.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = nint.Zero;
        }
        _proc = null;
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (data.vkCode == VK_RCONTROL)
            {
                var wp = wParam.ToInt64();
                if (wp == WM_KEYDOWN || wp == WM_SYSKEYDOWN)
                {
                    if (!_isPressed)
                    {
                        _isPressed = true;
                        OnPress?.Invoke();
                    }
                }
                else if (wp == WM_KEYUP || wp == WM_SYSKEYUP)
                {
                    if (_isPressed)
                    {
                        _isPressed = false;
                        OnRelease?.Invoke();
                    }
                }
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();

    private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(ref MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint msg, nint wParam, nint lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}

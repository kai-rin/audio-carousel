using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AudioCarousel.Hotkey;

public enum HotkeyRegisterResult
{
    Ok,
    // Another application (or another hotkey of ours) already owns the combination.
    InUse,
    Failed,
}

/// <summary>Outcome of test-registering the hotkeys (Ok for an unset one).</summary>
public readonly record struct HotkeyProbeResult(
    HotkeyRegisterResult Next, HotkeyRegisterResult Previous, HotkeyRegisterResult Input);

public sealed partial class HotkeyHost : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;
    // Suppress keyboard auto-repeat: holding the hotkey down must fire once,
    // not cycle devices repeatedly.
    private const uint MOD_NOREPEAT = 0x4000;
    private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

    private readonly MessageOnlyWindow _window;
    private bool _registered;
    private Action? _onHotkey;

    public HotkeyHost()
    {
        _window = new MessageOnlyWindow(OnMessage);
    }

    public HotkeyRegisterResult TryRegister(HotkeySpec spec, Action onHotkey)
    {
        Unregister();
        _onHotkey = onHotkey;
        bool ok = RegisterHotKey(_window.Handle, HOTKEY_ID, (uint)spec.Modifiers | MOD_NOREPEAT, (uint)spec.Key);
        _registered = ok;
        if (ok) return HotkeyRegisterResult.Ok;
        return Marshal.GetLastPInvokeError() == ERROR_HOTKEY_ALREADY_REGISTERED
            ? HotkeyRegisterResult.InUse
            : HotkeyRegisterResult.Failed;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(_window.Handle, HOTKEY_ID);
            _registered = false;
        }
        _onHotkey = null;
    }

    private void OnMessage(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            _onHotkey?.Invoke();
    }

    public void Dispose()
    {
        Unregister();
        _window.DestroyHandle();
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    private sealed class MessageOnlyWindow : NativeWindow
    {
        public delegate void MessageHandler(ref Message m);
        private readonly MessageHandler _handler;

        public MessageOnlyWindow(MessageHandler handler)
        {
            _handler = handler;
            CreateHandle(new CreateParams { Caption = "AudioCarousel.Hotkey", Parent = (IntPtr)(-3) });
        }

        protected override void WndProc(ref Message m)
        {
            _handler(ref m);
            base.WndProc(ref m);
        }
    }
}

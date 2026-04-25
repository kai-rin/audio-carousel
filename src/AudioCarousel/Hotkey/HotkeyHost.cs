using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AudioCarousel.Hotkey;

public sealed class HotkeyHost : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;

    private readonly MessageOnlyWindow _window;
    private bool _registered;
    private Action? _onHotkey;

    public HotkeyHost()
    {
        _window = new MessageOnlyWindow(OnMessage);
    }

    public bool TryRegister(HotkeySpec spec, Action onHotkey)
    {
        Unregister();
        _onHotkey = onHotkey;
        bool ok = RegisterHotKey(_window.Handle, HOTKEY_ID, (uint)spec.Modifiers, (uint)spec.Key);
        _registered = ok;
        return ok;
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

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

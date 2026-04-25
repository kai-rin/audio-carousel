using System.ComponentModel;
using System.Windows.Forms;
using AudioCarousel.Hotkey;
using AudioCarousel.I18n;

namespace AudioCarousel.UI;

public sealed class HotkeyTextBox : TextBox
{
    private HotkeySpec? _spec;
    private bool _capturing;

    public HotkeyTextBox()
    {
        ReadOnly = true;
        Cursor = Cursors.Default;
        TabStop = true;
        Render();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HotkeySpec? Value
    {
        get => _spec;
        set { _spec = value; Render(); }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _capturing = true;
        Text = Strings.Get("settings.hotkeyCapturing");
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _capturing = false;
        Render();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_capturing) return base.ProcessCmdKey(ref msg, keyData);

        var key = keyData & Keys.KeyCode;
        if (key == Keys.None || key == Keys.Escape)
        {
            _capturing = false;
            Render();
            FindForm()?.SelectNextControl(this, true, true, true, true);
            return true;
        }
        if (IsModifierOnly(key)) return true; // wait for non-modifier key

        var mods = HotkeyModifier.None;
        if ((keyData & Keys.Control) != 0) mods |= HotkeyModifier.Control;
        if ((keyData & Keys.Alt) != 0) mods |= HotkeyModifier.Alt;
        if ((keyData & Keys.Shift) != 0) mods |= HotkeyModifier.Shift;
        // Win key isn't reported via keyData — handled by raw WM if needed; skip for v1.

        _spec = new HotkeySpec(mods, key);
        _capturing = false;
        Render();
        FindForm()?.SelectNextControl(this, true, true, true, true);
        return true;
    }

    private static bool IsModifierOnly(Keys key) =>
        key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.LWin or Keys.RWin;

    public void Render()
    {
        Text = _spec is null
            ? Strings.Get("settings.hotkeyEmpty")
            : HotkeyParser.Format(_spec.Value);
    }
}

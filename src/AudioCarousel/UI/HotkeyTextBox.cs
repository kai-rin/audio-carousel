using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AudioCarousel.Hotkey;
using AudioCarousel.I18n;

namespace AudioCarousel.UI;

public sealed partial class HotkeyTextBox : TextBox
{
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    private HotkeySpec? _spec;
    private bool _capturing;

    public event EventHandler? ValueChanged;

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
        set
        {
            bool changed = _spec != value;
            _spec = value;
            Render();
            if (changed) ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // Capture starts only on an explicit request (click, or Enter/Space/F2 while
    // focused) — never on plain focus, so the dialog's initial focus or tabbing
    // through never records Enter/Tab as the global hotkey.
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!_capturing)
        {
            Focus();
            StartCapture();
        }
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        EndCapture();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_capturing)
        {
            if (keyData is Keys.Enter or Keys.Space or Keys.F2)
            {
                StartCapture();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        var key = keyData & Keys.KeyCode;
        var modifierBits = keyData & Keys.Modifiers;

        if (key == Keys.Escape)
        {
            // Handled here so the dialog's CancelButton never sees it.
            EndCapture();
            return true;
        }
        if (key == Keys.Tab && (modifierBits == Keys.None || modifierBits == Keys.Shift))
        {
            // End capture and let the dialog move focus as usual.
            EndCapture();
            return base.ProcessCmdKey(ref msg, keyData);
        }
        if (modifierBits == Keys.None && (key is Keys.Back or Keys.Delete))
        {
            _capturing = false;
            Value = null;
            return true;
        }
        if (key == Keys.None || HotkeyParser.IsModifierKey(key)) return true; // wait for non-modifier key

        var mods = HotkeyModifier.None;
        if ((keyData & Keys.Control) != 0) mods |= HotkeyModifier.Control;
        if ((keyData & Keys.Alt) != 0) mods |= HotkeyModifier.Alt;
        if ((keyData & Keys.Shift) != 0) mods |= HotkeyModifier.Shift;
        // keyData never carries the Win key; read its state directly.
        if (GetKeyState(VK_LWIN) < 0 || GetKeyState(VK_RWIN) < 0) mods |= HotkeyModifier.Win;

        var spec = new HotkeySpec(mods, key);
        switch (HotkeyParser.Validate(spec))
        {
            case HotkeyValidation.Ok:
                _capturing = false;
                Value = spec;
                break;
            case HotkeyValidation.NeedsModifier:
                Text = Strings.Get("settings.hotkeyNeedsModifier");
                break;
            case HotkeyValidation.Reserved:
                break;
        }
        return true;
    }

    private void StartCapture()
    {
        _capturing = true;
        Render();
    }

    private void EndCapture()
    {
        _capturing = false;
        Render();
    }

    public void Render()
    {
        if (_capturing)
            Text = Strings.Get("settings.hotkeyCapturing");
        else
            Text = _spec is null
                ? Strings.Get("settings.hotkeyEmpty")
                : HotkeyParser.FormatForDisplay(_spec.Value);
    }

    [LibraryImport("user32.dll")]
    private static partial short GetKeyState(int nVirtKey);
}

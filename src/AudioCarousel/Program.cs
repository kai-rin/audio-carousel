using System.Runtime.InteropServices;
using System.Windows.Forms;
using AudioCarousel.I18n;

namespace AudioCarousel;

internal static partial class Program
{
    // Local\ = per sign-in session: another user on the same PC (fast user
    // switching) runs their own instance, and can't block ours.
    private const string MutexName = @"Local\AudioCarousel.SingleInstance";
    private const string ShowSettingsEventName = @"Local\AudioCarousel.ShowSettings";

    [STAThread]
    private static void Main()
    {
        // DPI mode and visual styles come from the csproj (ApplicationHighDpiMode).
        ApplicationConfiguration.Initialize();

        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Launching again (e.g. double-clicking the exe) opens the running
            // instance's settings instead of a dead-end message.
            if (EventWaitHandle.TryOpenExisting(ShowSettingsEventName, out var showSettings))
            {
                using (showSettings)
                {
                    // We own the foreground right (the user just launched us);
                    // hand it on so the settings window can come to the front.
                    AllowSetForegroundWindow(ASFW_ANY);
                    showSettings.Set();
                }
                return;
            }

            Strings.SetLanguage(Strings.ResolveLanguage("auto", Strings.GetCurrentUiCultureName()));
            MessageBox.Show(Strings.Get("error.alreadyRunning"),
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"{Strings.Get("error.unhandled")}\n\n{ex}",
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        Application.ThreadException += (_, args) =>
        {
            MessageBox.Show($"{Strings.Get("error.unhandled")}\n\n{args.Exception}",
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        using var showSettingsSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        using var ctx = new TrayApplicationContext(showSettingsSignal);
        Application.Run(ctx);
    }

    private const int ASFW_ANY = -1;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllowSetForegroundWindow(int dwProcessId);
}

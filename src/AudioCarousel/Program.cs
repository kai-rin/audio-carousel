using System.Reflection;
using System.Windows.Forms;
using AudioCarousel.I18n;

namespace AudioCarousel;

internal static class Program
{
    private const string MutexName = @"Global\AudioCarousel.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Resolve language for the message box.
            Strings.SetLanguage(Strings.ResolveLanguage("auto", Strings.GetCurrentUiCultureName()));
            MessageBox.Show(Strings.Get("error.alreadyRunning"),
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

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

        using var ctx = new TrayApplicationContext();
        Application.Run(ctx);
    }
}

using System.Diagnostics;

namespace BrandAssets;

/// <summary>
/// Renders an HTML file to PNG with Microsoft Edge in headless mode. Edge
/// ships with Windows 10/11, so no browser download or npm toolchain is needed.
/// </summary>
public static class EdgeRenderer
{
    private static readonly string[] Candidates =
    {
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    };

    public static void Screenshot(string htmlPath, string pngPath, int width, int height, bool transparent)
    {
        string edge = Candidates.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("Microsoft Edge (msedge.exe) not found.");
        // Headless Edge silently skips tiny windows; keep a sane minimum.
        width = Math.Max(width, 200);
        height = Math.Max(height, 200);

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            File.Delete(pngPath);
            // A fresh profile each run: reusing one that a previous headless
            // instance still holds makes Edge exit without writing the file.
            string profile = Path.Combine(Path.GetTempPath(), "BrandAssets-edge-" + Guid.NewGuid().ToString("N"));
            var psi = new ProcessStartInfo(edge)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            // Some hosts (e.g. an editor's integrated terminal) export
            // __COMPAT_LAYER=RunAsInvoker; Edge inherits it and exits without rendering.
            psi.Environment.Remove("__COMPAT_LAYER");
            foreach (string arg in new[]
            {
                "--headless=new",
                "--disable-gpu",
                "--hide-scrollbars",
                "--no-first-run",
                "--force-device-scale-factor=1",
                $"--user-data-dir={profile}",
                $"--window-size={width},{height}",
                $"--screenshot={pngPath}",
                transparent ? "--default-background-color=00000000" : "--default-background-color=FFFFFFFF",
                new Uri(Path.GetFullPath(htmlPath)).AbsoluteUri,
            })
            {
                psi.ArgumentList.Add(arg);
            }

            using (var process = Process.Start(psi)!)
            {
                if (!process.WaitForExit(120_000)) process.Kill(entireProcessTree: true);
            }
            TryDelete(profile);
            if (File.Exists(pngPath)) return;
        }
        throw new InvalidOperationException($"Edge did not produce {pngPath}.");
    }

    // Edge's helper processes keep profile files open for a few seconds after
    // the main process exits; retry so ~20 MB profiles don't pile up in %TEMP%.
    private static void TryDelete(string dir)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
                return;
            }
            catch (DirectoryNotFoundException) { return; }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            Thread.Sleep(500);
        }
        Console.Error.WriteLine($"note: could not delete temporary Edge profile {dir}");
    }
}

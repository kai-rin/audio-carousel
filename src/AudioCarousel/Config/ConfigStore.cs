using System.Text.Json;

namespace AudioCarousel.Config;

public sealed class ConfigStore
{
    private readonly string _path;
    private readonly object _lock = new();

    public ConfigStore(string path)
    {
        _path = path;
    }

    public (ConfigSchema config, bool freshlyCreated, bool wasCorrupted) Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_path))
            {
                var defaults = new ConfigSchema();
                SaveInternal(defaults);
                return (defaults, true, false);
            }

            try
            {
                string json = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.ConfigSchema);
                if (loaded is null) throw new InvalidDataException("config deserialized to null");
                ClampCurrentIndex(loaded);
                return (loaded, false, false);
            }
            catch (Exception)
            {
                string backup = _path + ".bak";
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(_path, backup);
                var defaults = new ConfigSchema();
                SaveInternal(defaults);
                return (defaults, true, true);
            }
        }
    }

    public void Save(ConfigSchema config)
    {
        lock (_lock)
        {
            ClampCurrentIndex(config);
            SaveInternal(config);
        }
    }

    private void SaveInternal(ConfigSchema config)
    {
        string tmp = _path + ".tmp";
        string json = JsonSerializer.Serialize(config, ConfigJsonContext.Default.ConfigSchema);
        try
        {
            File.WriteAllText(tmp, json);
            ReplaceWithRetry(tmp, _path);
        }
        catch
        {
            try { File.Delete(tmp); } catch { /* best-effort cleanup */ }
            throw;
        }
    }

    // File.Replace can transiently fail with IOException on Windows when an
    // antivirus, search indexer, or backup tool briefly holds the destination
    // file open right after our previous write. Retry a few times with a tiny
    // backoff before giving up.
    private static void ReplaceWithRetry(string source, string destination)
    {
        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (File.Exists(destination))
                    File.Replace(source, destination, destinationBackupFileName: null);
                else
                    File.Move(source, destination);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(40 * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(40 * attempt);
            }
        }
    }

    private static void ClampCurrentIndex(ConfigSchema config)
    {
        if (config.Devices.Count == 0)
        {
            config.CurrentIndex = 0;
            return;
        }
        if (config.CurrentIndex < 0 || config.CurrentIndex >= config.Devices.Count)
            config.CurrentIndex = 0;
    }
}

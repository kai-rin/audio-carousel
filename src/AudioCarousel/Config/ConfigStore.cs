using System.Text.Json;

namespace AudioCarousel.Config;

public sealed class ConfigStore
{
    private readonly string _path;
    private readonly object _lock = new();
    private bool _saveBlocked;

    public ConfigStore(string path)
    {
        _path = path;
    }

    public ConfigLoadResult Load()
    {
        lock (_lock)
        {
            _saveBlocked = false;

            if (!File.Exists(_path))
            {
                var defaults = ConfigSchema.NewInstallDefaults();
                TrySaveInternal(defaults);
                return new ConfigLoadResult(defaults, FreshlyCreated: true, WasCorrupted: false, WasUnreadable: false);
            }

            string json;
            try
            {
                json = ReadWithRetry(_path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Not corruption: leave the file alone and refuse to save the
                // in-memory defaults over it.
                _saveBlocked = true;
                return new ConfigLoadResult(new ConfigSchema(), FreshlyCreated: false, WasCorrupted: false, WasUnreadable: true);
            }

            try
            {
                var loaded = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.ConfigSchema);
                if (loaded is null) throw new InvalidDataException("config deserialized to null");
                Normalize(loaded);
                return new ConfigLoadResult(loaded, FreshlyCreated: false, WasCorrupted: false, WasUnreadable: false);
            }
            catch (Exception)
            {
                try
                {
                    string backup = _path + ".bak";
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Move(_path, backup);
                }
                catch { /* best-effort backup; still recover with defaults */ }
                var defaults = ConfigSchema.NewInstallDefaults();
                TrySaveInternal(defaults);
                return new ConfigLoadResult(defaults, FreshlyCreated: true, WasCorrupted: true, WasUnreadable: false);
            }
        }
    }

    public void Save(ConfigSchema config)
    {
        lock (_lock)
        {
            if (_saveBlocked)
                throw new InvalidOperationException(
                    $"Config file '{_path}' could not be read at startup; saving is blocked to avoid overwriting it with defaults.");
            ClampCurrentIndex(config);
            SaveInternal(config);
        }
    }

    // Load-time saves are best-effort: an unwritable directory (e.g. the exe
    // dropped under Program Files without admin rights) must not prevent the
    // app from starting with an in-memory config. Explicit user saves still go
    // through Save(), which propagates failures.
    private void TrySaveInternal(ConfigSchema config)
    {
        try { SaveInternal(config); } catch { /* run with in-memory config */ }
    }

    private void SaveInternal(ConfigSchema config)
    {
        string tmp = _path + ".tmp";
        string json = JsonSerializer.Serialize(config, ConfigJsonContext.Default.ConfigSchema);
        try
        {
            // Flush to disk before the replace so a power loss cannot leave a
            // zero-length config behind.
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(fs))
            {
                writer.Write(json);
                writer.Flush();
                fs.Flush(flushToDisk: true);
            }
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

    // Same transient-lock tolerance as ReplaceWithRetry, for reads.
    private static string ReadWithRetry(string path)
    {
        const int maxAttempts = 5;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return File.ReadAllText(path);
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

    // System.Text.Json does not enforce non-null annotations, so a hand-edited
    // file can carry nulls anywhere. Repair them in place.
    public static void Normalize(ConfigSchema config)
    {
        if (string.IsNullOrWhiteSpace(config.Language))
            config.Language = "auto";

        config.Devices = NormalizeDevices(config.Devices);
        config.InputDevices = NormalizeDevices(config.InputDevices);

        config.Hotkey = NormalizeHotkey(config.Hotkey);
        config.HotkeyPrevious = NormalizeHotkey(config.HotkeyPrevious);
        config.HotkeyInput = NormalizeHotkey(config.HotkeyInput);

        ClampCurrentIndex(config);
    }

    private static List<DeviceEntry> NormalizeDevices(List<DeviceEntry>? source)
    {
        var devices = new List<DeviceEntry>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var device in source ?? new List<DeviceEntry>())
        {
            if (device is null) continue;
            device.EndpointId ??= "";
            device.DisplayName ??= "";
            if (device.EndpointId.Length == 0 && device.DisplayName.Length == 0) continue;
            if (device.EndpointId.Length > 0 && !seenIds.Add(device.EndpointId)) continue;
            devices.Add(device);
        }
        return devices;
    }

    private static HotkeyEntry? NormalizeHotkey(HotkeyEntry? hotkey)
    {
        if (hotkey is null) return null;
        if (hotkey.Key is null || hotkey.Modifiers is null) return null;
        hotkey.Modifiers.RemoveAll(m => m is null);
        return hotkey;
    }

    private static void ClampCurrentIndex(ConfigSchema config)
    {
        config.CurrentIndex = Clamp(config.CurrentIndex, config.Devices.Count);
        config.InputCurrentIndex = Clamp(config.InputCurrentIndex, config.InputDevices.Count);
    }

    private static int Clamp(int index, int count) =>
        index < 0 || index >= count ? 0 : index;
}

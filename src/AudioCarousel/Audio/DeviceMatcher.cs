using AudioCarousel.Config;

namespace AudioCarousel.Audio;

public static class DeviceMatcher
{
    /// <summary>
    /// Re-binds config entries whose EndpointId matches no active device by falling
    /// back to DisplayName matching. NVIDIA HDA DisplayPort/HDMI endpoints get a new
    /// endpoint GUID across reboots / display re-enumeration, so a persisted ID can
    /// go stale while the friendly name stays stable. Mutates entry.EndpointId in
    /// place on a successful match. Returns true if any entry was healed (the caller
    /// should persist the config).
    /// </summary>
    public static bool HealEndpointIds(
        IReadOnlyList<DeviceEntry> entries,
        IReadOnlyList<AudioDevice> activeDevices)
    {
        // Pass 1: live devices claimed by an exact ID match cannot be taken by name.
        var liveIds = new HashSet<string>(
            activeDevices.Select(d => d.EndpointId), StringComparer.Ordinal);
        var claimed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (liveIds.Contains(entry.EndpointId))
                claimed.Add(entry.EndpointId);
        }

        // Pass 2: stale entries take the first unclaimed live device with the same name.
        bool healedAny = false;
        foreach (var entry in entries)
        {
            if (liveIds.Contains(entry.EndpointId)) continue;
            if (entry.DisplayName.Length == 0) continue;

            foreach (var device in activeDevices)
            {
                if (claimed.Contains(device.EndpointId)) continue;
                if (!string.Equals(device.DisplayName, entry.DisplayName, StringComparison.Ordinal))
                    continue;

                entry.EndpointId = device.EndpointId;
                claimed.Add(device.EndpointId);
                healedAny = true;
                break;
            }
        }
        return healedAny;
    }
}

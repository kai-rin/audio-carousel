using AudioCarousel.Audio;
using AudioCarousel.Config;
using Xunit;

namespace AudioCarousel.Tests;

public class DeviceMatcherTests
{
    private static DeviceEntry Entry(string id, string name) =>
        new() { EndpointId = id, DisplayName = name };

    [Fact]
    public void Heal_AllIdsLive_ReturnsFalse_NoMutation()
    {
        var entries = new List<DeviceEntry> { Entry("a", "A"), Entry("b", "B") };
        var live = new List<AudioDevice> { new("a", "A"), new("b", "B") };

        bool healed = DeviceMatcher.HealEndpointIds(entries, live);

        Assert.False(healed);
        Assert.Equal("a", entries[0].EndpointId);
        Assert.Equal("b", entries[1].EndpointId);
    }

    [Fact]
    public void Heal_StaleId_NameMatches_RewritesIdAndReturnsTrue()
    {
        var entries = new List<DeviceEntry> { Entry("stale-lg", "LG ULTRAGEAR+") };
        var live = new List<AudioDevice> { new("live-lg", "LG ULTRAGEAR+") };

        bool healed = DeviceMatcher.HealEndpointIds(entries, live);

        Assert.True(healed);
        Assert.Equal("live-lg", entries[0].EndpointId);
    }

    [Fact]
    public void Heal_StaleId_NoNameMatch_LeavesEntryAndReturnsFalse()
    {
        var entries = new List<DeviceEntry> { Entry("stale", "LG ULTRAGEAR+") };
        var live = new List<AudioDevice> { new("x", "Other Device") };

        bool healed = DeviceMatcher.HealEndpointIds(entries, live);

        Assert.False(healed);
        Assert.Equal("stale", entries[0].EndpointId);
    }

    [Fact]
    public void Heal_ExactMatchClaimsFirst_NameFallbackCannotStealIt()
    {
        // Live device's ID matches entry A exactly; its name equals stale entry B's name.
        var entries = new List<DeviceEntry> { Entry("stale-b", "Speaker"), Entry("a", "Speaker") };
        var live = new List<AudioDevice> { new("a", "Speaker") };

        bool healed = DeviceMatcher.HealEndpointIds(entries, live);

        Assert.False(healed);
        Assert.Equal("stale-b", entries[0].EndpointId);
        Assert.Equal("a", entries[1].EndpointId);
    }

    [Fact]
    public void Heal_DuplicateNames_EntryOrderMapsToLiveEnumerationOrder()
    {
        var entries = new List<DeviceEntry> { Entry("stale-1", "Monitor"), Entry("stale-2", "Monitor") };
        var live = new List<AudioDevice> { new("live-1", "Monitor"), new("live-2", "Monitor") };

        bool healed = DeviceMatcher.HealEndpointIds(entries, live);

        Assert.True(healed);
        Assert.Equal("live-1", entries[0].EndpointId);
        Assert.Equal("live-2", entries[1].EndpointId);
    }

    [Fact]
    public void Heal_TwoStaleSameNameEntries_OneLiveDevice_OnlyFirstHeals()
    {
        var entries = new List<DeviceEntry> { Entry("stale-1", "Monitor"), Entry("stale-2", "Monitor") };
        var live = new List<AudioDevice> { new("live-1", "Monitor") };

        bool healed = DeviceMatcher.HealEndpointIds(entries, live);

        Assert.True(healed);
        Assert.Equal("live-1", entries[0].EndpointId);
        Assert.Equal("stale-2", entries[1].EndpointId);
    }

    [Fact]
    public void Heal_EmptyEntriesOrEmptyLiveList_ReturnsFalse()
    {
        Assert.False(DeviceMatcher.HealEndpointIds(
            new List<DeviceEntry>(), new List<AudioDevice> { new("a", "A") }));

        var entries = new List<DeviceEntry> { Entry("stale", "A") };
        Assert.False(DeviceMatcher.HealEndpointIds(entries, new List<AudioDevice>()));
        Assert.Equal("stale", entries[0].EndpointId);
    }

    [Fact]
    public void Heal_EmptyDisplayName_NeverNameMatches()
    {
        var entries = new List<DeviceEntry> { Entry("stale", "") };
        var live = new List<AudioDevice> { new("live", "") };

        bool healed = DeviceMatcher.HealEndpointIds(entries, live);

        Assert.False(healed);
        Assert.Equal("stale", entries[0].EndpointId);
    }
}

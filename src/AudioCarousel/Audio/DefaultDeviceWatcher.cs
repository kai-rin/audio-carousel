using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AudioCarousel.Audio;

/// <summary>
/// Raises <see cref="Changed"/> on the captured synchronization context when
/// the default render device changes or a render device appears, disappears
/// or changes state — including changes made by other apps or Windows itself.
/// Bursts of notifications are coalesced into one callback.
/// </summary>
public sealed class DefaultDeviceWatcher : IMMNotificationClient, IDisposable
{
    private readonly SynchronizationContext _context;
    private readonly MMDeviceEnumerator _enumerator;
    private int _pending;
    private bool _disposed;

    public event Action? Changed;

    public DefaultDeviceWatcher(SynchronizationContext context)
    {
        _context = context;
        _enumerator = new MMDeviceEnumerator();
        _enumerator.RegisterEndpointNotificationCallback(this);
    }

    // Callbacks arrive on an MMDevice worker thread. Calling back into the
    // audio APIs from here can deadlock, so only post to the UI thread.
    private void Signal()
    {
        if (Interlocked.Exchange(ref _pending, 1) == 1) return;
        _context.Post(_ =>
        {
            Interlocked.Exchange(ref _pending, 0);
            if (!_disposed) Changed?.Invoke();
        }, null);
    }

    void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && role == Role.Multimedia) Signal();
    }

    void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState) => Signal();
    void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId) => Signal();
    void IMMNotificationClient.OnDeviceRemoved(string deviceId) => Signal();
    void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _enumerator.UnregisterEndpointNotificationCallback(this); }
        catch (Exception) { /* shutting down; nothing to recover */ }
        _enumerator.Dispose();
    }
}

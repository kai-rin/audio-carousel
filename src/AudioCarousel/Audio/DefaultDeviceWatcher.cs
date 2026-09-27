using NAudio.CoreAudioApi;

namespace AudioCarousel.Audio;

/// <summary>
/// Raises <see cref="Changed"/> on the captured synchronization context when
/// the default render device changes or a render device appears, disappears
/// or changes state — including changes made by other apps or Windows itself.
/// Bursts of notifications are coalesced into one callback.
/// </summary>
public sealed class DefaultDeviceWatcher : IDisposable
{
    private readonly SynchronizationContext _context;
    private readonly MMDeviceEnumerator _enumerator;
    private readonly MMDeviceNotificationClient _client;
    private int _pending;
    private bool _disposed;

    public event Action? Changed;

    public DefaultDeviceWatcher(SynchronizationContext context)
    {
        _context = context;
        _enumerator = new MMDeviceEnumerator();
        // Raw worker-thread events: we coalesce and post ourselves, so a burst
        // (e.g. a dock reconnecting several endpoints) costs one refresh.
        _client = _enumerator.CreateNotificationClient(useSynchronizationContext: false);
        _client.DefaultDeviceChanged += (_, e) =>
        {
            if (e.Flow == DataFlow.Render && e.Role == Role.Multimedia) Signal();
        };
        _client.DeviceAdded += (_, _) => Signal();
        _client.DeviceRemoved += (_, _) => Signal();
        _client.DeviceStateChanged += (_, _) => Signal();
    }

    // Called on the audio worker thread, which must not block or call back
    // into the audio stack — only post to the UI thread.
    private void Signal()
    {
        if (Interlocked.Exchange(ref _pending, 1) == 1) return;
        _context.Post(_ =>
        {
            Interlocked.Exchange(ref _pending, 0);
            if (!_disposed) Changed?.Invoke();
        }, null);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _client.Dispose();
        _enumerator.Dispose();
    }
}

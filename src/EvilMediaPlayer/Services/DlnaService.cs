using LibVLCSharp.Shared;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Provides discovery and management of DLNA devices on the local network using LibVLC.
/// </summary>
public class DlnaService : IDlnaService
{
    private readonly LibVLC _libVlc;
    private MediaDiscoverer _mediaDiscoverer;

    /// <summary>
    ///     Occurs when a new DLNA device is discovered on the local network.
    /// </summary>
    public event Action<Media> DeviceAdded;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="libVlc"></param>
    public DlnaService(LibVLC libVlc)
    {
        _libVlc = libVlc;
    }

    /// <summary>
    ///     Start discovering DLNA devices on the local network.
    /// </summary>
    /// <remarks>
    ///     Discovery relies on native MediaDiscoverer implementations exposed by LibVLC. We probe available
    ///     discoverers and then start the UPnP discoverer to find devices; logging what discoverers are
    ///     available helps diagnose platform-specific issues when devices aren't found.
    /// </remarks>
    public void StartDiscovery()
    {
        if (_mediaDiscoverer != null)
        {
            return;
        }

        foreach (var discoverer in _libVlc.MediaDiscoverers(MediaDiscovererCategory.Devices))
        {
            System.Diagnostics.Debug.WriteLine($"DLNA: Available discoverer (Devices): {discoverer.Name} ({discoverer.LongName})");
        }

        foreach (var discoverer in _libVlc.MediaDiscoverers(MediaDiscovererCategory.Lan))
        {
            System.Diagnostics.Debug.WriteLine($"DLNA: Available discoverer (Lan): {discoverer.Name} ({discoverer.LongName})");
        }

        System.Diagnostics.Debug.WriteLine("DLNA: Starting discovery with 'upnp'...");
        _mediaDiscoverer = new(_libVlc, "upnp");

        if (_mediaDiscoverer?.MediaList == null)
        {
            return;
        }

        _mediaDiscoverer.MediaList.ItemAdded += (_, e) =>
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"DLNA: Found item: {e.Media.Mrl}");
                                                    DeviceAdded?.Invoke(e.Media);
                                                };
        var started = _mediaDiscoverer.Start();
        System.Diagnostics.Debug.WriteLine($"DLNA: Discovery started: {started}");
    }

    /// <summary>
    ///     Stop discovery and release native resources.
    /// </summary>
    /// <remarks>
    ///     Properly stopping and disposing the MediaDiscoverer releases native handles and avoids leaks that
    ///     can persist across app runs or when restarting discovery during runtime.
    /// </remarks>
    public void StopDiscovery()
    {
        _mediaDiscoverer?.Stop();
        _mediaDiscoverer?.Dispose();
        _mediaDiscoverer = null;
    }
}
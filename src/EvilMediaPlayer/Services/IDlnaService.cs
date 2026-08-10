using LibVLCSharp.Shared;

namespace EvilMediaPlayer.Services;

/// <summary>
///     Provides discovery and management of DLNA devices on the local network using LibVLC.
/// </summary>
public interface IDlnaService
{
    /// <summary>
    ///     Occurs when a new DLNA device is discovered on the local network.
    /// </summary>
    event Action<Media> DeviceAdded;

    /// <summary>
    ///     Start discovering DLNA devices on the local network.
    /// </summary>
    /// <remarks>
    ///     Discovery relies on native MediaDiscoverer implementations exposed by LibVLC. We probe available
    ///     discoverers and then start the UPnP discoverer to find devices; logging what discoverers are
    ///     available helps diagnose platform-specific issues when devices aren't found.
    /// </remarks>
    void StartDiscovery();

    /// <summary>
    ///     Stop discovery and release native resources.
    /// </summary>
    /// <remarks>
    ///     Properly stopping and disposing the MediaDiscoverer releases native handles and avoids leaks that
    ///     can persist across app runs or when restarting discovery during runtime.
    /// </remarks>
    void StopDiscovery();
}
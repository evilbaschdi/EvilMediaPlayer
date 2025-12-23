using System;
using LibVLCSharp.Shared;

namespace EvilMediaPlayer.Services
{
    public class DlnaService
    {
        private readonly LibVLC _libVlc;
        private MediaDiscoverer _mediaDiscoverer;

        public event Action<Media> DeviceAdded;

        public DlnaService(LibVLC libVlc)
        {
            _libVlc = libVlc;
        }

        public void StartDiscovery()
        {
            if (_mediaDiscoverer != null) return;

            foreach (var discoverer in _libVlc.MediaDiscoverers(MediaDiscovererCategory.Devices))
            {
                System.Diagnostics.Debug.WriteLine($"DLNA: Available discoverer (Devices): {discoverer.Name} ({discoverer.LongName})");
            }
            foreach (var discoverer in _libVlc.MediaDiscoverers(MediaDiscovererCategory.Lan))
            {
                System.Diagnostics.Debug.WriteLine($"DLNA: Available discoverer (Lan): {discoverer.Name} ({discoverer.LongName})");
            }

            System.Diagnostics.Debug.WriteLine("DLNA: Starting discovery with 'upnp'...");
            _mediaDiscoverer = new MediaDiscoverer(_libVlc, "upnp");
            _mediaDiscoverer.MediaList.ItemAdded += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"DLNA: Found item: {e.Media.Mrl}");
                DeviceAdded?.Invoke(e.Media);
            };
            bool started = _mediaDiscoverer.Start();
            System.Diagnostics.Debug.WriteLine($"DLNA: Discovery started: {started}");
        }

        public void StopDiscovery()
        {
            _mediaDiscoverer?.Stop();
            _mediaDiscoverer?.Dispose();
            _mediaDiscoverer = null;
        }
    }
}
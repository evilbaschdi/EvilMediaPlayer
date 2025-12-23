using System;
using System.Windows.Input;
using Avalonia.Controls;
using LibVLCSharp.Shared;
using ReactiveUI;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace EvilMediaPlayer.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly LibVLC _libVlc;

    public MediaPlayer MediaPlayer { get; }
    public MediaBrowserViewModel MediaBrowserViewModel { get; }

    private string _currentTrackName;
    public string CurrentTrackName
    {
        get => _currentTrackName;
        set => this.RaiseAndSetIfChanged(ref _currentTrackName, value);
    }

    private long _duration;
    public long Duration
    {
        get => _duration;
        set => this.RaiseAndSetIfChanged(ref _duration, value);
    }

    private long _position;
    public long Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                this.RaiseAndSetIfChanged(ref _position, value);
                
                // Only seek if the difference is significant (e.g., more than 1 second) 
                // to avoid feedback loops from TimeChanged event.
                if (Math.Abs(MediaPlayer.Time - value) > 1000)
                {
                    MediaPlayer.Time = value;
                }
            }
        }
    }

    private string _durationText;
    public string DurationText
    {
        get => _durationText;
        set => this.RaiseAndSetIfChanged(ref _durationText, value);
    }

    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    public ICommand PlayPauseCommand { get; }

    public MainWindowViewModel(LibVLC libVlc, MediaBrowserViewModel mediaBrowserViewModel)
    {
        _libVlc = libVlc ?? throw new ArgumentNullException(nameof(libVlc));
        MediaBrowserViewModel = mediaBrowserViewModel ?? throw new ArgumentNullException(nameof(mediaBrowserViewModel));

        _libVlc.Log += (s, e) => System.Diagnostics.Debug.WriteLine($"VLC: {e.Level} {e.Message}");
        MediaPlayer = new MediaPlayer(_libVlc);
        MediaPlayer.Volume = 100;

        MediaPlayer.Playing += (s, e) => IsPlaying = true;
        MediaPlayer.Paused += (s, e) => IsPlaying = false;
        MediaPlayer.Stopped += (s, e) => IsPlaying = false;
        MediaPlayer.EndReached += (s, e) => IsPlaying = false;

        MediaPlayer.LengthChanged += (s, e) => 
        {
            Duration = e.Length;
            DurationText = TimeSpan.FromMilliseconds(e.Length).ToString(@"hh\:mm\:ss");
        };

        MediaPlayer.TimeChanged += (s, e) =>
        {
            _position = e.Time;
            this.RaisePropertyChanged(nameof(Position));
        };

        MediaBrowserViewModel.MediaSelected += PlayMedia;

        PlayPauseCommand = new RelayCommand(_ => PlayPause());
    }

    private void PlayPause()
    {
        if (MediaPlayer.IsPlaying)
            MediaPlayer.Pause();
        else
            MediaPlayer.Play();
    }

    public void PlayMedia(Models.FileSystemItem item)
    {
        if (item == null || item.IsDirectory)
            return;

        CurrentTrackName = item.Name;
        string path = item.Path;
        if (!path.StartsWith("http") && !path.StartsWith("upnp") && !path.StartsWith("file"))
        {
            try 
            {
                path = new Uri(path).AbsoluteUri;
            }
            catch { }
        }

        System.Diagnostics.Debug.WriteLine($"Playing normalized: {path}");
        var media = new Media(_libVlc, path, FromType.FromLocation);
        MediaPlayer.Media = media;
        bool playResult = MediaPlayer.Play();
        System.Diagnostics.Debug.WriteLine($"Play result: {playResult}");
    }


    public void Play()
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        PlayMedia(new Models.FileSystemItem { Path = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" });
    }


    public void Stop()
    {
        MediaPlayer.Stop();
    }

    public void Dispose()
    {
        MediaBrowserViewModel?.Dispose();
        MediaPlayer?.Dispose();
        _libVlc?.Dispose();
    }
}
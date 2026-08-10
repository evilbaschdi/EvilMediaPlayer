using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using EvilBaschdi.About.Avalonia;
using EvilMediaPlayer.Models;
using EvilMediaPlayer.Services;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace EvilMediaPlayer.ViewModels;

/// <summary>
///     Main window view-model that coordinates media playback and UI state.
/// </summary>
/// <remarks>
///     This view-model is responsible for orchestrating playback and presenting UI state.
///     It delegates domain logic to specialized services (artwork loading, path normalization, metadata parsing)
///     to keep concerns separated and improve testability (SOLID principles).
/// </remarks>
public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly IArtworkService _artworkService;
    private readonly IMediaPathNormalizer _pathNormalizer;
    private readonly IMediaMetadataService _metadataService;

    /// <summary>
    ///     The LibVLC-backed media player instance used by the view-model.
    /// </summary>
    /// <remarks>
    ///     Exposed so the view-model can manage playback lifecycle and so unit tests or other components
    ///     can interact with the underlying player when necessary; exposing the native object avoids
    ///     duplicating playback integration logic across the codebase.
    /// </remarks>
    public MediaPlayer MediaPlayer { get; }

    /// <summary>
    ///     The nested view-model responsible for browsing media sources.
    /// </summary>
    /// <remarks>
    ///     Exposing the media-browser view-model allows the main window to compose smaller view-models
    ///     and keeps browsing concerns encapsulated while making them available for binding in the view.
    /// </remarks>
    public MediaBrowserViewModel MediaBrowserViewModel { get; }

    private string _currentTrackName;

    /// <summary>
    ///     The current track display name shown in the UI.
    /// </summary>
    /// <remarks>
    ///     Kept as a simple string to present rich, user-facing metadata assembled from tags so views can
    ///     render a human-friendly title without needing to format multiple fields themselves.
    /// </remarks>
    public string CurrentTrackName
    {
        get => _currentTrackName;
        set => this.RaiseAndSetIfChanged(ref _currentTrackName, value);
    }

    private Bitmap _coverArt;

    /// <summary>
    ///     Cover artwork for the currently playing track.
    /// </summary>
    /// <remarks>
    ///     The UI binds directly to a Bitmap so images can be updated asynchronously and rendered
    ///     without extra transformation logic in the view. Null indicates no artwork is available.
    /// </remarks>
    public Bitmap CoverArt
    {
        get => _coverArt;
        set => this.RaiseAndSetIfChanged(ref _coverArt, value);
    }

    private bool _isAudioFile;

    /// <summary>
    ///     Indicates whether the currently loaded media is audio-only.
    /// </summary>
    /// <remarks>
    ///     Views use this flag to toggle UI elements (e.g., show cover art or video controls). Determining
    ///     this at runtime prevents incorrect control presentation when media metadata changes after parsing.
    /// </remarks>
    public bool IsAudioFile
    {
        get => _isAudioFile;
        set => this.RaiseAndSetIfChanged(ref _isAudioFile, value);
    }

    private long _duration;

    /// <summary>
    ///     Media duration in milliseconds as reported by LibVLC.
    /// </summary>
    /// <remarks>
    ///     Exposed as a numeric value so consumers can perform calculations (progress bars, remaining time)
    ///     while the formatted textual representation is provided separately for display.
    /// </remarks>
    public long Duration
    {
        get => _duration;
        set => this.RaiseAndSetIfChanged(ref _duration, value);
    }

    private long _position;

    /// <summary>
    ///     Current playback position in milliseconds.
    /// </summary>
    /// <remarks>
    ///     Setting the position seeks the player; the setter guards against tiny updates to avoid feedback
    ///     loops with the player's TimeChanged event. This keeps the UI responsive while preventing jitter.
    /// </remarks>
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

    /// <summary>
    ///     Human-readable duration string used for display (e.g., "hh:mm:ss").
    /// </summary>
    /// <remarks>
    ///     Keeping a separate textual representation avoids repeated formatting work on the UI thread and
    ///     ensures a consistent, locale-invariant presentation for time values.
    /// </remarks>
    public string DurationText
    {
        get => _durationText;
        set => this.RaiseAndSetIfChanged(ref _durationText, value);
    }

    private bool _isPlaying;

    /// <summary>
    ///     Whether media playback is currently active.
    /// </summary>
    /// <remarks>
    ///     Exposed so controls (play/pause buttons, indicators) can react to playback state changes without
    ///     subscribing to native events; the view-model bridges native callbacks to this simple boolean.
    /// </remarks>
    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    /// <summary>
    ///     Play/Pause command exposed to the view.
    /// </summary>
    /// <remarks>
    ///     Exposed as a ReactiveCommand created from an async method so that any asynchronous work
    ///     triggered by the command (e.g., UI updates or awaiting lib calls) is handled consistently and
    ///     command execution state can be observed by the UI framework.
    /// </remarks>
    public ReactiveCommand<RxVoid, RxVoid> PlayPauseCommand { get; set; }

    /// <summary>
    ///     Stop command exposed to the view.
    /// </summary>
    /// <remarks>
    ///     Implemented as an async command for the same reasons as PlayPause: uniform async execution model
    ///     and easier integration with ReactiveCommand's throttling and UI binding semantics.
    /// </remarks>
    public ReactiveCommand<RxVoid, RxVoid> StopCommand { get; set; }

    /// <summary>
    ///     Command that opens the About window.
    /// </summary>
    /// <remarks>
    ///     Keeping a command property (instead of directly opening windows from the view) preserves testability
    ///     and keeps UI-triggered actions in the view-model where lifetimes and DI resolution are handled.
    /// </remarks>
    public ReactiveCommand<RxVoid, RxVoid> AboutWindowCommand { get; set; }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="libVlc">LibVLC instance for media playback</param>
    /// <param name="mediaBrowserViewModel">Media browser view model</param>
    /// <param name="artworkService">Service for loading artwork</param>
    /// <param name="pathNormalizer">Service for normalizing media paths</param>
    /// <param name="metadataService">Service for extracting media metadata</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MainWindowViewModel(
        LibVLC libVlc,
        MediaBrowserViewModel mediaBrowserViewModel,
        IArtworkService artworkService,
        IMediaPathNormalizer pathNormalizer,
        IMediaMetadataService metadataService)
    {
        _libVlc = libVlc ?? throw new ArgumentNullException(nameof(libVlc));
        MediaBrowserViewModel = mediaBrowserViewModel ?? throw new ArgumentNullException(nameof(mediaBrowserViewModel));
        _artworkService = artworkService ?? throw new ArgumentNullException(nameof(artworkService));
        _pathNormalizer = pathNormalizer ?? throw new ArgumentNullException(nameof(pathNormalizer));
        _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));

        // Log from LibVLC to help diagnose native/runtime issues during development.
        _libVlc.Log += (_, e) => Debug.WriteLine($"VLC: {e.Level} {e.Message}");
        MediaPlayer = new(_libVlc);
        MediaPlayer.Volume = 100;

        // Map LibVLC playback events to a simple IsPlaying flag so views can react to playback state without
        // subscribing to native events directly.
        MediaPlayer.Playing += (_, _) => IsPlaying = true;
        MediaPlayer.Paused += (_, _) => IsPlaying = false;
        MediaPlayer.Stopped += (_, _) => IsPlaying = false;
        MediaPlayer.EndReached += (_, _) => IsPlaying = false;

        // Update duration and its textual representation when LibVLC reports a length change. This keeps UI display
        // in sync with the underlying media without requiring polling.
        MediaPlayer.LengthChanged += (_, e) =>
                                     {
                                         Duration = e.Length;
                                         DurationText = TimeSpan.FromMilliseconds(e.Length).ToString(@"hh\:mm\:ss");
                                     };

        // LibVLC raises time updates from native threads; update the position property and notify bindings.
        MediaPlayer.TimeChanged += (_, e) =>
                                   {
                                       _position = e.Time;
                                       this.RaisePropertyChanged(nameof(Position));
                                   };

        // When the media browser selects an item, start playback.
        MediaBrowserViewModel.MediaSelected += PlayMedia;

        PlayPauseCommand = ReactiveCommand.CreateFromTask(PlayPause);
        StopCommand = ReactiveCommand.CreateFromTask(Stop);
        AboutWindowCommand = ReactiveCommand.CreateFromTask(AboutWindowCommandAction);
    }

    private async Task PlayPause()
    {
        if (MediaPlayer.IsPlaying)
        {
            MediaPlayer.Pause();
        }
        else
        {
            MediaPlayer.Play();
        }

        await Task.CompletedTask;
    }

    private async Task Stop()
    {
        if (MediaPlayer.IsPlaying)
        {
            MediaPlayer.Stop();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    ///     Play the selected media item.
    /// </summary>
    /// <remarks>
    ///     This method is intentionally declared `async void` because it is used as an event handler
    ///     (MediaSelected) and needs to start asynchronous work without returning a Task to the caller.
    ///     It delegates to specialized services for artwork loading and path normalization.
    /// </remarks>
    /// <param name="item"></param>
    public async void PlayMedia(FileSystemItem item)
    {
        if (item == null || item.IsDirectory)
        {
            return;
        }

        CurrentTrackName = $"{item.TrackNumber} {item.DisplayName} | {item.Artist} | {item.Album} ({item.Year})";
        CoverArt = null;

        await InitialArtworkLoadAsync(item);

        var path = _pathNormalizer.NormalizePath(item.Path);
        Debug.WriteLine($"Playing normalized: {path}");

        var media = new Media(_libVlc, path, FromType.FromLocation);
        media.ParsedChanged += (_, e) => OnMediaParsed(e, media);

        var parsedStatus = await media.Parse(MediaParseOptions.ParseNetwork | MediaParseOptions.ParseLocal);

        if (parsedStatus == MediaParsedStatus.Done)
        {
            MediaPlayer.Media = media;
            var playResult = MediaPlayer.Play();
            Debug.WriteLine($"Play result: {playResult}");
        }
    }

    /// <summary>
    ///     Load artwork from the item's pre-loaded URL or extract from metadata.
    /// </summary>
    private async Task InitialArtworkLoadAsync(FileSystemItem item)
    {
        // Check if item has pre-loaded artwork (e.g. from DLNA)
        if (!string.IsNullOrEmpty(item.ArtworkUrl))
        {
            var art = await Dispatcher.UIThread.InvokeAsync(() => _artworkService.LoadCoverArtAsync(item.ArtworkUrl));
            if (art != null)
            {
                CoverArt = art;
                return;
            }
        }

        // Check if this is an audio file and try to extract cover art (fallback)
        IsAudioFile = _artworkService.IsAudioFile(item.Path);
        if (IsAudioFile)
        {
            var art = await _artworkService.ExtractCoverArtAsync(item.Path);
            if (art != null)
            {
                CoverArt = art;
            }
        }
    }

    /// <summary>
    ///     Handle completion of media metadata parsing.
    /// </summary>
    private async void OnMediaParsed(MediaParsedChangedEventArgs e, Media media)
    {
        if (e.ParsedStatus != MediaParsedStatus.Done)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(async () =>
                                              {
                                                  // Update IsAudioFile based on actual tracks
                                                  IsAudioFile = !_metadataService.HasVideoTracks(media);

                                                  // Try to get artwork from LibVLC
                                                  var artworkUrl = _metadataService.GetArtworkUrl(media);
                                                  if (!string.IsNullOrEmpty(artworkUrl) && CoverArt == null)
                                                  {
                                                      CoverArt = await _artworkService.LoadCoverArtAsync(artworkUrl);
                                                  }
                                              });
    }

    /// <summary>
    ///     Dispose native and managed resources in correct order.
    /// </summary>
    /// <remarks>
    ///     LibVLC and MediaPlayer hold native handles; disposing them deterministically prevents resource
    ///     leaks and avoids native libraries remaining locked after the application exits.
    /// </remarks>
    public void Dispose()
    {
        MediaBrowserViewModel?.Dispose();
        MediaPlayer?.Dispose();
        _libVlc?.Dispose();
    }

    /// <summary>
    ///     Command that shows the about window.
    /// </summary>
    /// <remarks>
    ///     The command resolves the AboutWindow from the DI container instead of constructing it here so
    ///     the window dependencies and lifetime are managed consistently with the rest of the app.
    /// </remarks>
    private async Task AboutWindowCommandAction()
    {
        var aboutWindow = DependencyInjectedApplication.ServiceProvider.GetRequiredService<AboutWindow>();
        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        if (mainWindow != null)
        {
            await aboutWindow.ShowDialog(mainWindow);
        }
    }
}
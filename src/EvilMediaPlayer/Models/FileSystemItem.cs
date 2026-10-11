using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EvilMediaPlayer.Models;

/// <summary>
///     Represents a file system or media item shown in the UI.
/// </summary>
/// <remarks>
///     This class intentionally implements a lightweight <see cref="INotifyPropertyChanged" /> pattern so
///     it can be used directly by the UI. Exposing an <see cref="OnExpanded" /> callback allows the view-model
///     to lazily load children when the UI expands a node without coupling the model to view types.
///     The <see cref="DisplayName" /> property prefers embedded metadata (Title) over the filesystem name
///     so the UI shows more meaningful text for media files.
/// </remarks>
public class FileSystemItem : INotifyPropertyChanged
{
    /// <summary>
    ///     The file or directory name.
    /// </summary>
    /// <remarks>
    ///     When Name changes, DisplayName is also notified because it computes its value from Title or Name;
    ///     this ensures the UI always sees consistent display values without manual updates.
    /// </remarks>
    public string Name
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // When the underlying name changes we also notify that the computed display name changed.
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    /// <summary>
    ///     Computed display name: prefer embedded Title if present to show richer metadata.
    /// </summary>
    /// <remarks>
    ///     This computed property returns Title if available (usually from embedded tags), otherwise falls back
    ///     to the filesystem Name. This lets media files display their metadata title instead of raw filenames.
    /// </remarks>
    public string DisplayName => !string.IsNullOrEmpty(Title) ? Title : Name;

    /// <summary>
    ///     Full path to the file or directory.
    /// </summary>
    /// <remarks>
    ///     This may be a local filesystem path, a UPnP URL, or an HTTP URL depending on the source
    ///     (file system, DLNA device, etc.). It is used by the view-model to open and play media.
    /// </remarks>
    public string Path
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    ///     URL or path to cover artwork for this media item.
    /// </summary>
    /// <remarks>
    ///     Set from metadata (DLNA, embedded tags, or extracted files) so the UI can display cover art
    ///     without needing to extract or fetch it separately during presentation.
    /// </remarks>
    public string ArtworkUrl
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    ///     The artist or performer name extracted from media metadata.
    /// </summary>
    /// <remarks>
    ///     Extracted from embedded tags (ID3, Vorbis, etc.) or DLNA metadata so the UI can display
    ///     rich media information without parsing tags during binding.
    /// </remarks>
    public string Artist
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    ///     The album name extracted from media metadata.
    /// </summary>
    /// <remarks>
    ///     Populated from embedded tags or DLNA metadata to enrich the UI display and help users
    ///     identify albums and collections at a glance.
    /// </remarks>
    public string Album
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    ///     The release year extracted from media metadata.
    /// </summary>
    /// <remarks>
    ///     Stored as a string so it can be displayed as-is without parsing; extracted from the date
    ///     field in tags or DLNA metadata (typically the first 4 characters).
    /// </remarks>
    public string Year
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    ///     The track number extracted from media metadata.
    /// </summary>
    /// <remarks>
    ///     Formatted as a string for display (e.g., "01", "1.") so the UI can render it without
    ///     type conversion; derived from embedded tags or DLNA metadata.
    /// </remarks>
    public string TrackNumber
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    ///     The media title extracted from metadata (track, episode, content name, etc.).
    /// </summary>
    /// <remarks>
    ///     When Title changes, DisplayName is also notified because it uses Title as the primary display value;
    ///     this ensures the UI sees consistent, up-to-date display names when metadata is refreshed.
    /// </remarks>
    public string Title
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // Title affects DisplayName, so notify consumers when it changes.
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    /// <summary>
    ///     Human-readable duration string (e.g., "mm:ss" or "hh:mm:ss").
    /// </summary>
    /// <remarks>
    ///     Pre-formatted for display so the UI doesn't need to parse or convert milliseconds;
    ///     this avoids repeated formatting work and ensures consistent time representation.
    /// </remarks>
    public string DurationText
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    ///     Indicates whether this item represents a directory or a file.
    /// </summary>
    /// <remarks>
    ///     Used by the UI to decide whether to show expand/collapse icons and which icon to display
    ///     (folder vs. file). Set during enumeration and may be updated as metadata becomes available.
    /// </remarks>
    public bool IsDirectory
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    ///     Children collection is null for files and non-null for directories to make UI templates simpler.
    /// </summary>
    /// <remarks>
    ///     The collection is populated lazily when the item is expanded by the view-model. Using null
    ///     for non-directories allows the UI to easily distinguish directories from files without
    ///     checking both IsDirectory and Children.Count.
    /// </remarks>
    public ObservableCollection<FileSystemItem> Children
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    ///     Whether the directory is currently expanded in the UI tree.
    /// </summary>
    /// <remarks>
    ///     When set to true, the OnExpanded event fires so the view-model can asynchronously populate
    ///     the Children collection. This pattern enables lazy-loading and keeps the UI responsive.
    /// </remarks>
    public bool IsExpanded
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value)
            {
                // Fire expansion callback so consumers (e.g. view-model) can perform lazy-loading.
                OnExpanded?.Invoke(this);
            }
        }
    }

    /// <summary>
    ///     Invoked when the UI expands a directory. Consumers should load children on demand.
    /// </summary>
    /// <remarks>
    ///     This event allows the view-model to handle directory expansion without the model needing
    ///     to know about loading logic; it keeps concerns separated and makes the pattern testable.
    /// </remarks>
    public event Action<FileSystemItem> OnExpanded;

    /// <summary>
    ///     Raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     Raises the PropertyChanged event for the specified property name.
    /// </summary>
    /// <remarks>
    ///     Using CallerMemberName allows properties to raise change notifications without duplicating
    ///     the property name string, reducing the risk of typos and keeping property change logic concise.
    /// </remarks>
    /// <param name="propertyName">The name of the property that changed (auto-populated by CallerMemberName).</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    /// <summary>
    ///     Updates a property's backing field and raises PropertyChanged if the value actually changed.
    /// </summary>
    /// <remarks>
    ///     This helper method implements the standard MVVM pattern: it avoids raising PropertyChanged
    ///     when the new value equals the old value, reducing unnecessary UI updates. Using CallerMemberName
    ///     keeps the property names in sync without manual string duplication.
    /// </remarks>
    /// <typeparam name="T">The property value type.</typeparam>
    /// <param name="storage">The backing field reference.</param>
    /// <param name="value">The new value to assign.</param>
    /// <param name="propertyName">The property name (auto-populated by CallerMemberName).</param>
    /// <returns>True if the value changed and PropertyChanged was raised; false if no change occurred.</returns>
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
using Avalonia.Controls;

namespace EvilMediaPlayer.Views;

/// <inheritdoc />
public partial class MediaPlayerControl : UserControl
{
    /// <summary>
    ///     Minimal view constructor. Keep initialization trivial so visual designer and DI can
    ///     instantiate the view without side-effects; view-specific logic belongs in view-models.
    /// </summary>
    public MediaPlayerControl()
    {
        InitializeComponent();
    }
}
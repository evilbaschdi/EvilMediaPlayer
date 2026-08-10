using FluentAvalonia.UI.Windowing;

namespace EvilMediaPlayer.Views;

/// <inheritdoc />
public partial class MainWindow : FAAppWindow
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <remarks>
    ///     Attaching developer tools in DEBUG keeps them out of production builds while allowing
    ///     rapid UI inspection during development. The constructor remains minimal to keep view
    ///     initialization deterministic and delegate application logic to view-models.
    /// </remarks>
    public MainWindow()
    {
        InitializeComponent();
    }
}
using Avalonia.Markup.Xaml;

namespace EvilMediaPlayer;

/// <inheritdoc />
public class App : DependencyInjectedApplication
{
    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        // Register third-party helpers first so application-level customizations can be applied to views.

        InitDependencyInjectedMainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
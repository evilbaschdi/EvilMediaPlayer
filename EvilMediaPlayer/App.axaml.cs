using Avalonia.Markup.Xaml;
using EvilBaschdi.About.Avalonia.DependencyInjection;
using EvilMediaPlayer.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

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
        ServiceCollection.AddAboutServices();
        ServiceCollection.AddWindowsAndViewModels();

        ServiceProvider = ServiceCollection.BuildServiceProvider();

        InitDependencyInjectedMainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
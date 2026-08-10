using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using EvilBaschdi.Core.Avalonia.Themes;
using EvilMediaPlayer.ViewModels;
using EvilMediaPlayer.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EvilMediaPlayer;

/// <summary>
///     Application class with dependency injection support.
/// </summary>
public class DependencyInjectedApplication : Application
{
    /// <summary>
    ///     ServiceProvider for DependencyInjection
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    ///     Initializes the application using dependency injection, configuring the main window and its view model from the
    ///     service provider.
    /// </summary>
    /// <remarks>
    ///     This method sets up the main window for classic desktop-style application lifetimes,
    ///     resolving dependencies such as the view model and optional styling helpers from the dependency injection
    ///     container. If optional styling helpers are unavailable or fail, the application startup continues without
    ///     interruption. This method should be called during application startup to ensure that dependencies are properly
    ///     injected and lifetimes are managed consistently.
    /// </remarks>
    protected void InitDependencyInjectedMainWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        // Create the main window and resolve its view-model from the DI container so lifetimes are consistent.
        var mainWindow = new MainWindow
                         {
                             DataContext = ServiceProvider?.GetRequiredService<MainWindowViewModel>()
                         };

        // Try to apply optional styling helpers provided by EvilBaschdi.Core.Avalonia; failures are non-fatal so
        // we catch and log them to preserve application startup.
        try
        {
            ThemeEngine.Initialize(this);

            ThemeEngine.ApplyThemeToWindow(mainWindow, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EvilBaschdi.Core.Avalonia helpers failed: {ex.Message}");
        }

        desktop.MainWindow = mainWindow;
    }
}
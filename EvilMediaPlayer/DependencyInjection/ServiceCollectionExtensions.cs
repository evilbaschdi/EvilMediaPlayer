using EvilMediaPlayer.ViewModels;
using EvilMediaPlayer.Views;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace EvilMediaPlayer.DependencyInjection;

/// <summary>
///     Provides extension methods for registering application windows and view models
///     into the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers application windows and view models into the DI container.
    /// </summary>
    /// <remarks>
    ///     Centralizing registrations in an extension keeps composition decisions in one place so the
    ///     application's startup code stays concise. LibVLC is registered as a singleton because it
    ///     manages native resources and must be shared; view models are singletons to keep state stable
    ///     across the UI. The main window is transient to ensure a fresh view instance can be created by
    ///     the framework when needed without coupling view lifecycle to DI-scoped services.
    /// </remarks>
    public static void AddWindowsAndViewModels(this IServiceCollection serviceCollection)
    {
        // LibVLC wraps native resources; sharing a single LibVLC instance prevents duplicate native
        // initializations and simplifies lifecycle management.
        serviceCollection.AddSingleton(_ => new LibVLC());

        // View models are application-level state holders in this design; registering them as singletons
        // avoids recreating their state when views are recreated.
        serviceCollection.AddSingleton<MainWindowViewModel>();
        serviceCollection.AddSingleton<MediaBrowserViewModel>();

        // Register the view as transient so the DI container can provide fresh view instances while still
        // allowing view models to be shared.
        serviceCollection.AddTransient<MainWindow>();
    }
}
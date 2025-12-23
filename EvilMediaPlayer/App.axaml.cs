using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EvilMediaPlayer.DependencyInjection;
using EvilMediaPlayer.ViewModels;
using EvilMediaPlayer.Views;
using Microsoft.Extensions.DependencyInjection;
using EvilBaschdi.Core.Avalonia;

namespace EvilMediaPlayer;

public class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        
        // Add EvilBaschdi Core Avalonia services if available via extension
        // serviceCollection.AddAvaloniaServices(); 
        
        serviceCollection.AddWindowsAndViewModels();

        ServiceProvider = serviceCollection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>();

            // Apply EvilBaschdi styling helpers if they exist in the library
            try
            {
                var handleOsDependentTitleBar = ServiceProvider.GetService<IHandleOsDependentTitleBar>();
                handleOsDependentTitleBar?.RunFor(mainWindow);

                var applicationLayout = ServiceProvider.GetService<IApplicationLayout>();
                applicationLayout?.RunFor((mainWindow, true, true));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EvilBaschdi.Core.Avalonia helpers failed: {ex.Message}");
            }

            desktop.MainWindow = mainWindow;
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        var vm = ServiceProvider?.GetService<MainWindowViewModel>();
        vm?.Dispose();
    }
}

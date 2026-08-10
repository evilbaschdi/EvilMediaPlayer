using Avalonia;
using EvilBaschdi.About.Avalonia.DependencyInjection;
using EvilMediaPlayer.DependencyInjection;
using LibVLCSharp.Shared;
using ReactiveUI.Avalonia.Splat;

namespace EvilMediaPlayer;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] args)
    {
        // Determine native libvlc path based on process architecture so native libraries are loaded
        // from a known location. We allow null to let LibVLC fallback to its platform defaults which
        // simplifies distribution across platforms.
        string arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        string libVlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", $"win-{arch}");

        if (!Directory.Exists(libVlcPath))
        {
            // If native arch binaries are missing, but we are on ARM64, we might be able to use x64 binaries if running under emulation.
            // However, a native ARM64 process CANNOT load x64 DLLs.
            // If the folder is missing, we let Core.Initialize() try its default logic or throw a better error.
            libVlcPath = null;
        }

        try
        {
            Core.Initialize(libVlcPath);
        }
        catch (VLCException)
        {
            // Fallback to default search paths if explicit path failed. This makes startup robust when the
            // application is installed in different layout or when native search paths work better.
            Core.Initialize();
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
                     .UsePlatformDetect()
                     .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
                     .LogToTrace()
                     .UseSkia()
                     .UseReactiveUIWithMicrosoftDependencyResolver(
                         serviceCollection =>
                         {
                             serviceCollection.AddAboutServices();
                             serviceCollection.AddWindowsAndViewModels();
                         },
                         sp => { DependencyInjectedApplication.ServiceProvider = sp; });
}
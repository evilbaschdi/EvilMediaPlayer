using System;
using Avalonia;
using ReactiveUI.Avalonia;
using LibVLCSharp.Shared;

namespace EvilMediaPlayer;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] args)
    {
        string arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        string libVlcPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", $"win-{arch}");

        if (!System.IO.Directory.Exists(libVlcPath))
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
            // Fallback to default search paths if explicit path failed
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
                     .UseReactiveUI();
}
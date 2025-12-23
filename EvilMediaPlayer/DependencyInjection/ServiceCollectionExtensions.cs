using Microsoft.Extensions.DependencyInjection;
using EvilMediaPlayer.ViewModels;
using EvilMediaPlayer.Views;
using EvilMediaPlayer.Services;
using LibVLCSharp.Shared;
using EvilBaschdi.Core.Avalonia;

namespace EvilMediaPlayer.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddWindowsAndViewModels(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<LibVLC>(_ => new LibVLC());
            serviceCollection.AddSingleton<MainWindowViewModel>();
            serviceCollection.AddSingleton<MediaBrowserViewModel>();
            serviceCollection.AddTransient<MainWindow>();
        }
    }
}

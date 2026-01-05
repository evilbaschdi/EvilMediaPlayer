using ReactiveUI;

namespace EvilMediaPlayer.ViewModels;

/// <summary>
///     Base class for view-models providing ReactiveUI plumbing.
/// </summary>
/// <remarks>
///     Keeping a small dedicated base class ensures all view-models have a single point for
///     cross-cutting behaviors (notification, common services) if needed in the future. For now
///     it serves as a clear marker type used by the view-locator and simplifies testing.
/// </remarks>
public class ViewModelBase : ReactiveObject
{
}
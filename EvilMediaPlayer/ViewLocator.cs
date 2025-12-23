using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using EvilMediaPlayer.ViewModels;

namespace EvilMediaPlayer;

/// <summary>
///     Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control Build([NotNull] object data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var name = data.GetType().FullName?.Replace("ViewModel", "View");

        if (name == null)
        {
            return new TextBlock { Text = "View Not Found" };
        }

        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <inheritdoc />
    public bool Match(object data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return data is ViewModelBase;
    }
}
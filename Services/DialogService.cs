using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace PSTFuze.Services;

public class DialogService : IDialogService
{
    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        var window = GetMainWindow();
        if (window is null) return Array.Empty<IStorageFile>();

        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel is null) return Array.Empty<IStorageFile>();

        return await topLevel.StorageProvider.OpenFilePickerAsync(options);
    }

    public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        var window = GetMainWindow();
        if (window is null) return null;

        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel is null) return null;

        return await topLevel.StorageProvider.SaveFilePickerAsync(options);
    }

    public async Task ShowMessageAsync(string message, string title)
    {
        var window = GetMainWindow();
        if (window is null) return;

        var dialogWindow = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 400,
            MinWidth = 300,
            MaxWidth = 500
        };

        var content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 20
        };

        content.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            Command = new Command(() => dialogWindow.Close())
        };

        content.Children.Add(okButton);
        dialogWindow.Content = content;

        await dialogWindow.ShowDialog(window);
    }
}
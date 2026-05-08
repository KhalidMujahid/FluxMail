using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.IO;

namespace FluxMail.Desktop.Services;

public class AvaloniaFilePickerService : IFilePickerService
{
    public async Task<string?> ReadTextAsync(string title, IReadOnlyList<string> extensions)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var patterns = extensions.Select(e => $"*.{e}").ToArray();
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(title) { Patterns = patterns }]
        });

        if (files.Count == 0) return null;

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task<(byte[] Bytes, string Extension)?> ReadBinaryAsync(string title, IReadOnlyList<string> extensions)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var patterns = extensions.Select(e => $"*.{e}").ToArray();
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(title) { Patterns = patterns }]
        });

        if (files.Count == 0) return null;

        var ext = Path.GetExtension(files[0].Name).TrimStart('.').ToLowerInvariant();
        await using var stream = await files[0].OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return (ms.ToArray(), ext);
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return TopLevel.GetTopLevel(desktop.MainWindow);
        return null;
    }
}

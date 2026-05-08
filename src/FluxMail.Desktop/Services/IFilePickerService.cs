namespace FluxMail.Desktop.Services;

public interface IFilePickerService
{
    Task<string?> ReadTextAsync(string title, IReadOnlyList<string> extensions);
    Task<(byte[] Bytes, string Extension)?> ReadBinaryAsync(string title, IReadOnlyList<string> extensions);
}

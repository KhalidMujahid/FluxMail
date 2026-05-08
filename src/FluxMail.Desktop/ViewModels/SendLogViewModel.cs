using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluxMail.Desktop.ViewModels;

public class LogEntry
{
    public string Text { get; set; } = "";
    public string Foreground { get; set; } = "#CDD6F4";
}

public partial class SendLogViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<LogEntry> _entries = [];
    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private string _title = "Send Log";
    [ObservableProperty] private string _summary = "";

    private void Post(string text, string color) =>
        Dispatcher.UIThread.Post(() => Entries.Add(new LogEntry { Text = text, Foreground = color }));

    public void Header(string text)   => Post($"\n  ── {text} ──", "#CBA6F7");
    public void Info(string text)     => Post($"     {text}", "#89B4FA");
    public void Success(string text)  => Post($"  ✓  {text}", "#A6E3A1");
    public void Fail(string text)     => Post($"  ✗  {text}", "#F38BA8");
    public void Warn(string text)     => Post($"  ⚠  {text}", "#FAB387");
    public void Separator()           => Post("     " + new string('─', 50), "#45475A");
    public void Blank()               => Post("", "#CDD6F4");

    public void Complete(string summaryText = "")
    {
        Dispatcher.UIThread.Post(() =>
        {
            Summary = summaryText;
            IsComplete = true;
        });
    }
}

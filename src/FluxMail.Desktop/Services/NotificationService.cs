using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace FluxMail.Desktop.Services;

public enum ToastType { Info, Success, Error, Warning }

public class ToastItem
{
    public string Message { get; set; } = "";
    public ToastType Type { get; set; }

    public string Background => Type switch
    {
        ToastType.Success => "#A6E3A1",
        ToastType.Error   => "#F38BA8",
        ToastType.Warning => "#FAB387",
        _                 => "#89B4FA"
    };

    public string Icon => Type switch
    {
        ToastType.Success => "✓",
        ToastType.Error   => "✗",
        ToastType.Warning => "⚠",
        _                 => "ℹ"
    };
}

public class NotificationService
{
    public ObservableCollection<ToastItem> Items { get; } = [];

    public void Show(string message, ToastType type = ToastType.Info)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var item = new ToastItem { Message = message, Type = type };
            Items.Add(item);
            Task.Delay(4500).ContinueWith(_ =>
                Dispatcher.UIThread.Post(() => Items.Remove(item)));
        });
    }

    public void Success(string msg) => Show(msg, ToastType.Success);
    public void Error(string msg)   => Show(msg, ToastType.Error);
    public void Info(string msg)    => Show(msg, ToastType.Info);
    public void Warn(string msg)    => Show(msg, ToastType.Warning);
}

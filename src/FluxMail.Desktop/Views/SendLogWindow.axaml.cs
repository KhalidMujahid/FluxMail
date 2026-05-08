using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FluxMail.Desktop.ViewModels;

namespace FluxMail.Desktop.Views;

public partial class SendLogWindow : Window
{
    public SendLogWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is SendLogViewModel vm)
        {
            vm.Entries.CollectionChanged += (_, _) =>
                Dispatcher.UIThread.Post(() =>
                    this.FindControl<ScrollViewer>("LogScroll")?.ScrollToEnd());
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}

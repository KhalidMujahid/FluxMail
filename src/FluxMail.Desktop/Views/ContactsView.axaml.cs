using Avalonia.Controls;
using Avalonia.Interactivity;
using FluxMail.Desktop.ViewModels;

namespace FluxMail.Desktop.Views;

public partial class ContactsView : UserControl
{
    public ContactsView()
    {
        InitializeComponent();
    }

    private void OnNewListClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ContactsViewModel vm)
            vm.IsCreatingList = true;
    }

    private void OnCancelListClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ContactsViewModel vm)
            vm.IsCreatingList = false;
    }
}

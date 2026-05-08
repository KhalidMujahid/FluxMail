using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Core.Services;

namespace FluxMail.Desktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IUserProfileRepository _repo;

    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<UserProfile>? LoginSuccessful;

    public LoginViewModel(IUserProfileRepository repo)
    {
        _repo = repo;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = "";
        IsBusy = true;

        var profile = await _repo.GetAsync();

        if (profile is null ||
            !profile.Email.Equals(Email.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !PasswordHasher.Verify(Password, profile.PasswordHash))
        {
            ErrorMessage = "Invalid email or password.";
            IsBusy = false;
            return;
        }

        IsBusy = false;
        LoginSuccessful?.Invoke(this, profile);
    }
}

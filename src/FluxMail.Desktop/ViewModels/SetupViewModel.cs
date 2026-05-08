using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Core.Services;

namespace FluxMail.Desktop.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    private readonly IUserProfileRepository _repo;

    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _confirmPassword = "";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<UserProfile>? AccountCreated;

    public SetupViewModel(IUserProfileRepository repo)
    {
        _repo = repo;
    }

    [RelayCommand]
    private async Task CreateAccountAsync()
    {
        ErrorMessage = "";

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "Full name is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            ErrorMessage = "A valid email is required.";
            return;
        }
        if (Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters.";
            return;
        }
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        IsBusy = true;
        var profile = new UserProfile
        {
            FullName = FullName.Trim(),
            Email = Email.Trim().ToLowerInvariant(),
            PasswordHash = PasswordHasher.Hash(Password),
            CreatedAt = DateTime.UtcNow
        };
        await _repo.CreateAsync(profile);
        IsBusy = false;

        AccountCreated?.Invoke(this, profile);
    }
}

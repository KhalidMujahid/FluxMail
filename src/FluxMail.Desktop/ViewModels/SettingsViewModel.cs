using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Services;
using FluxMail.Desktop.Services;

namespace FluxMail.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase, IAsyncLoadable
{
    private readonly IUserProfileRepository _repo;
    private readonly CurrentUserService _currentUser;

    // Profile fields
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _email = "";

    // Password change fields
    [ObservableProperty] private string _currentPassword = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _confirmNewPassword = "";

    public string UserInitials => _currentUser.Initials;

    // Status
    [ObservableProperty] private string _profileStatus = "";
    [ObservableProperty] private bool _profileStatusIsError;
    [ObservableProperty] private string _passwordStatus = "";
    [ObservableProperty] private bool _passwordStatusIsError;

    public SettingsViewModel(IUserProfileRepository repo, CurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var profile = await _repo.GetAsync();
        if (profile is null) return;
        FullName = profile.FullName;
        Email = profile.Email;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        ProfileStatus = "";

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ProfileStatus = "Full name is required.";
            ProfileStatusIsError = true;
            return;
        }
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            ProfileStatus = "A valid email is required.";
            ProfileStatusIsError = true;
            return;
        }

        var profile = await _repo.GetAsync();
        if (profile is null) return;

        profile.FullName = FullName.Trim();
        profile.Email = Email.Trim().ToLowerInvariant();
        await _repo.UpdateAsync(profile);

        _currentUser.Profile = profile;
        OnPropertyChanged(nameof(UserInitials));
        ProfileStatus = "Profile updated.";
        ProfileStatusIsError = false;
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        PasswordStatus = "";

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            PasswordStatus = "Current password is required.";
            PasswordStatusIsError = true;
            return;
        }
        if (NewPassword.Length < 6)
        {
            PasswordStatus = "New password must be at least 6 characters.";
            PasswordStatusIsError = true;
            return;
        }
        if (NewPassword != ConfirmNewPassword)
        {
            PasswordStatus = "New passwords do not match.";
            PasswordStatusIsError = true;
            return;
        }

        var profile = await _repo.GetAsync();
        if (profile is null) return;

        if (!PasswordHasher.Verify(CurrentPassword, profile.PasswordHash))
        {
            PasswordStatus = "Current password is incorrect.";
            PasswordStatusIsError = true;
            return;
        }

        profile.PasswordHash = PasswordHasher.Hash(NewPassword);
        await _repo.UpdateAsync(profile);

        CurrentPassword = NewPassword = ConfirmNewPassword = "";
        PasswordStatus = "Password changed.";
        PasswordStatusIsError = false;
    }
}

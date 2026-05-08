using CommunityToolkit.Mvvm.ComponentModel;
using FluxMail.Core.Models;

namespace FluxMail.Desktop.Services;

public partial class CurrentUserService : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyPropertyChangedFor(nameof(Email))]
    [NotifyPropertyChangedFor(nameof(Initials))]
    private UserProfile? _profile;

    public string DisplayName => Profile?.FullName ?? "User";
    public string Email => Profile?.Email ?? "";

    public string Initials
    {
        get
        {
            var name = Profile?.FullName ?? "";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpper(),
                _ => $"{parts[0][0]}{parts[^1][0]}".ToUpper()
            };
        }
    }
}

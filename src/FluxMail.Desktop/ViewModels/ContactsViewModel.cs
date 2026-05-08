using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;

namespace FluxMail.Desktop.ViewModels;

public partial class ContactsViewModel : ViewModelBase, IAsyncLoadable
{
    private readonly IContactRepository _repo;

    [ObservableProperty] private ObservableCollection<Contact> _contacts = [];
    [ObservableProperty] private ObservableCollection<ContactList> _lists = [];
    [ObservableProperty] private ContactList? _selectedList;
    [ObservableProperty] private Contact? _selectedContact;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isLoading;

    // Form fields
    [ObservableProperty] private string _formName = "";
    [ObservableProperty] private string _formEmail = "";
    [ObservableProperty] private string _formCompany = "";

    // New list form
    [ObservableProperty] private string _newListName = "";
    [ObservableProperty] private bool _isCreatingList;

    public ContactsViewModel(IContactRepository repo)
    {
        _repo = repo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        var contacts = SelectedList is null
            ? await _repo.GetAllAsync()
            : await _repo.GetByListIdAsync(SelectedList.Id);
        Contacts = new ObservableCollection<Contact>(contacts);

        var lists = await _repo.GetAllListsAsync();
        Lists = new ObservableCollection<ContactList>(lists);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task SelectListAsync(ContactList? list)
    {
        SelectedList = list;
        await LoadAsync();
    }

    [RelayCommand]
    private void NewContact()
    {
        SelectedContact = null;
        FormName = FormEmail = FormCompany = "";
        IsEditing = true;
    }

    [RelayCommand]
    private void EditContact(Contact contact)
    {
        SelectedContact = contact;
        FormName = contact.Name;
        FormEmail = contact.Email;
        FormCompany = contact.Company ?? "";
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveContactAsync()
    {
        if (string.IsNullOrWhiteSpace(FormEmail))
        {
            StatusMessage = "Email is required.";
            return;
        }

        var contact = SelectedContact ?? new Contact();
        contact.Name = FormName;
        contact.Email = FormEmail;
        contact.Company = string.IsNullOrWhiteSpace(FormCompany) ? null : FormCompany;

        if (contact.Id == 0)
            await _repo.AddAsync(contact);
        else
            await _repo.UpdateAsync(contact);

        await LoadAsync();
        IsEditing = false;
        StatusMessage = "Contact saved.";
    }

    [RelayCommand]
    private async Task DeleteContactAsync(Contact contact)
    {
        await _repo.DeleteAsync(contact.Id);
        await LoadAsync();
        StatusMessage = "Contact deleted.";
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private async Task CreateListAsync()
    {
        if (string.IsNullOrWhiteSpace(NewListName)) return;
        await _repo.AddListAsync(new ContactList { Name = NewListName });
        NewListName = "";
        IsCreatingList = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteListAsync(ContactList list)
    {
        await _repo.DeleteListAsync(list.Id);
        if (SelectedList?.Id == list.Id) SelectedList = null;
        await LoadAsync();
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxMail.Core.Models;
using FluxMail.Core.Services;

namespace FluxMail.Desktop.ViewModels;

public partial class TemplatesViewModel : ViewModelBase, IAsyncLoadable
{
    private readonly TemplateService _service;

    [ObservableProperty] private ObservableCollection<EmailTemplate> _templates = [];
    [ObservableProperty] private EmailTemplate? _selectedTemplate;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isLoading;

    // Form fields
    [ObservableProperty] private string _formName = "";
    [ObservableProperty] private string _formSubject = "";
    [ObservableProperty] private string _formHtmlBody = "";
    [ObservableProperty] private string _formPlainText = "";

    public TemplatesViewModel(TemplateService service)
    {
        _service = service;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        var list = await _service.GetAllAsync();
        Templates = new ObservableCollection<EmailTemplate>(list);
        IsLoading = false;
    }

    [RelayCommand]
    private void NewTemplate()
    {
        SelectedTemplate = null;
        FormName = FormSubject = FormHtmlBody = FormPlainText = "";
        IsEditing = true;
    }

    [RelayCommand]
    private void EditTemplate(EmailTemplate template)
    {
        SelectedTemplate = template;
        FormName = template.Name;
        FormSubject = template.Subject;
        FormHtmlBody = template.HtmlBody;
        FormPlainText = template.PlainTextBody ?? "";
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName) || string.IsNullOrWhiteSpace(FormSubject))
        {
            StatusMessage = "Name and subject are required.";
            return;
        }

        var template = SelectedTemplate ?? new EmailTemplate();
        template.Name = FormName;
        template.Subject = FormSubject;
        template.HtmlBody = FormHtmlBody;
        template.PlainTextBody = string.IsNullOrWhiteSpace(FormPlainText) ? null : FormPlainText;

        if (template.Id == 0)
            await _service.CreateAsync(template);
        else
            await _service.UpdateAsync(template);

        await LoadAsync();
        IsEditing = false;
        StatusMessage = "Template saved.";
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync(EmailTemplate template)
    {
        await _service.DeleteAsync(template.Id);
        await LoadAsync();
        StatusMessage = "Template deleted.";
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }
}

using System;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;

namespace KeyPocket.UI.ViewModels;

public partial class KeyItemViewModel : ObservableObject
{
    private readonly ApiKey _apiKey;
    private readonly ProviderService _providerService;

    [ObservableProperty] public partial string DisplayKey { get; set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTagDisplayVisible))]
    [NotifyPropertyChangedFor(nameof(IsTagRowVisible))]
    public partial bool IsEditingTag { get; set; }
    [ObservableProperty] public partial string MaskedKey { get; set; } = "Loading...";
    private string? _originalTag;
    [ObservableProperty] public partial string? ProviderIcon { get; set; }

    public KeyItemViewModel(ApiKey apiKey, string providerName, string? providerIcon, ProviderService providerService)
    {
        _apiKey = apiKey;
        ProviderName = providerName;
        ProviderIcon = providerIcon;
        _providerService = providerService;

        Initialize();

        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (r, m) =>
        {
            OnPropertyChanged(nameof(IconUri));
        });
    }

    public Guid Id => _apiKey.Id;
    public Guid ProviderId => _apiKey.ProviderId;
    public string ProviderName { get; }

    public DateTime CreatedAt => _apiKey.CreatedAt;

    public string? Tag
    {
        get => _apiKey.Tag;
        set
        {
            if (_apiKey.Tag != value)
            {
                OnPropertyChanging();
                _providerService.UpdateApiKeyTag(ProviderId, Id, value);
                _apiKey.Tag = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTag));
                OnPropertyChanged(nameof(IsTagDisplayVisible));
                OnPropertyChanged(nameof(IsTagRowVisible));
            }
        }
    }

    public bool HasTag => !string.IsNullOrEmpty(Tag);

    public bool IsTagDisplayVisible => HasTag && !IsEditingTag;

    public bool IsTagRowVisible => HasTag || IsEditingTag;

    public bool IsCustomIcon => ProviderIconHelper.HasCustomIcon(ProviderIcon);
    public string DefaultGlyph => ProviderIconHelper.DefaultIconGlyph;
    public Uri? IconUri => ProviderIconHelper.GetIconUri(ProviderIcon, ThemeHelper.IsDarkTheme());

    public bool IsFavorite
    {
        get => _apiKey.IsFavorite;
        set
        {
            if (_apiKey.IsFavorite != value)
            {
                OnPropertyChanging();
                _apiKey.IsFavorite = value;
                _providerService.ToggleFavoriteApiKey(ProviderId, Id);
                OnPropertyChanged();
            }
        }
    }

    public bool IsDisabled
    {
        get => _apiKey.IsDisabled;
        set
        {
            if (_apiKey.IsDisabled != value)
            {
                OnPropertyChanging();
                _apiKey.IsDisabled = value;
                ToggleDisableServiceCall();
                OnPropertyChanged();
            }
        }
    }

    private void Initialize()
    {
        // Decrypt to generate mask
        var rawKey = _providerService.GetDecryptedApiKey(_apiKey.ProviderId, _apiKey.Id);
        if (string.IsNullOrEmpty(rawKey))
        {
            MaskedKey = "Error: Cannot Decrypt";
            DisplayKey = MaskedKey; // Ensure DisplayKey is set
            return;
        }

        MaskedKey = GenerateMask(rawKey);
        DisplayKey = MaskedKey;
    }

    private string GenerateMask(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (key.Length <= 8) return new string('*', key.Length);

        string prefix;
        string suffix;
        const string mask = "............"; // 12 dots

        if (key.StartsWith("sk-"))
        {
            // First 7 chars (sk- + 4 chars)
            var take = Math.Min(7, key.Length);
            prefix = key.Substring(0, take);
        }
        else
        {
            // First 4 chars
            var take = Math.Min(4, key.Length);
            prefix = key.Substring(0, take);
        }

        // Last 4 chars
        suffix = key.Substring(key.Length - 4);

        return $"{prefix}{mask}{suffix}";
    }

    [RelayCommand]
    private void StartEditTag()
    {
        _originalTag = Tag;
        IsEditingTag = true;
    }

    [RelayCommand]
    private void CommitEditTag()
    {
        IsEditingTag = false;
        _originalTag = null;
    }

    [RelayCommand]
    private void CancelEditTag()
    {
        Tag = _originalTag;
        IsEditingTag = false;
        _originalTag = null;
    }

    [RelayCommand]
    private void UpdateTag(string newTag)
    {
        Tag = newTag;
        OnPropertyChanged(nameof(Tag));
        OnPropertyChanged(nameof(HasTag));
    }

    private void ToggleDisableServiceCall()
    {
        _providerService.ToggleDisableApiKey(ProviderId, Id);
    }

    [RelayCommand]
    private void ToggleDisabled()
    {
        IsDisabled = !IsDisabled;
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
    }

    [RelayCommand]
    private void CopyKey()
    {
        var rawKey = _providerService.GetDecryptedApiKey(ProviderId, Id);
        if (!string.IsNullOrEmpty(rawKey))
        {
            var package = new DataPackage();
            package.SetText(rawKey);
            Clipboard.SetContent(package);

            // Explicitly clear local var if possible (though string is immutable, GC handles it)
            // The requirement was to not keep it in memory as a field. 
            // We are using a local var 'rawKey' which will be collected.
        }
    }
}

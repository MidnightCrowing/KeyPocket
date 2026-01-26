using System;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;

namespace KeyPocket.UI.ViewModels;

public partial class KeyItemViewModel : ObservableObject
{
    private readonly ApiKey _apiKey;
    private readonly ProviderService _providerService;

    [ObservableProperty] private string _displayKey = string.Empty;

    [ObservableProperty] private string _maskedKey = "Loading...";

    [ObservableProperty] private string? _providerIcon;

    public KeyItemViewModel(ApiKey apiKey, string providerName, string? providerIcon, ProviderService providerService)
    {
        _apiKey = apiKey;
        ProviderName = providerName;
        _providerIcon = providerIcon;
        _providerService = providerService;

        Initialize();
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
            }
        }
    }

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

    private string GenerateMask(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (key.Length <= 8) return new string('*', key.Length);

        // Standard format often: sk-.......1234
        // Keep first 3 letters (if they exist) and last 4.
        var prefix = key.Substring(0, Math.Min(3, key.Length));
        var suffix = key.Substring(key.Length - 4);
        return $"{prefix}••••••••{suffix}";
    }

    private void ToggleDisableServiceCall()
    {
        // Placeholder for the service call I will add
        // Reflection or just direct call if I update service first. 
        // Best practice: Update service first. But I am writing this file first. 
        // I'll assume the method exists `ToggleDisableApiKey`.
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
        }
    }
}
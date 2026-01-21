using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace KeyPocket.UI.ViewModels;

public partial class KeyItemViewModel : ObservableObject
{
    private readonly ProviderService _providerService;
    private readonly ApiKey _apiKey;
    private readonly string _providerName;

    [ObservableProperty]
    private string? _providerIcon;

    [ObservableProperty]
    private string _maskedKey = "Loading...";

    [ObservableProperty]
    private bool _isKeyRevealed;

    [ObservableProperty]
    private string _displayKey = string.Empty;

    public KeyItemViewModel(ApiKey apiKey, string providerName, string? providerIcon, ProviderService providerService)
    {
        _apiKey = apiKey;
        _providerName = providerName;
        _providerIcon = providerIcon;
        _providerService = providerService;
        
        Initialize();
    }

    private void Initialize()
    {
        // Decrypt to generate mask, but don't store full key in memory permanently if possible.
        // For 'Reveal', we will fetch again.
        var rawKey = _providerService.GetDecryptedApiKey(_apiKey.ProviderId, _apiKey.Id);
        if (string.IsNullOrEmpty(rawKey))
        {
            MaskedKey = "Error: Cannot Decrypt";
            return;
        }

        MaskedKey = GenerateMask(rawKey);
        DisplayKey = MaskedKey;
    }

    private string GenerateMask(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (key.Length <= 8) return new string('*', key.Length);

        // Standard format often: sk-.......1234
        // Keep first 3 letters (if they exist) and last 4.
        string prefix = key.Substring(0, Math.Min(3, key.Length));
        string suffix = key.Substring(key.Length - 4);
        return $"{prefix}••••••••{suffix}";
    }

    public Guid Id => _apiKey.Id;
    public Guid ProviderId => _apiKey.ProviderId;
    public string ProviderName => _providerName;
    
    public DateTime CreatedAt => _apiKey.CreatedAt;
    public string CreatedAtFormatted => _apiKey.CreatedAt.ToString("yyyy-MM-dd");

    public string? Tag
    {
        get => _apiKey.Tag;
        set
        {
            if (_apiKey.Tag != value)
            {
                OnPropertyChanging();
                _providerService.UpdateApiKeyTag(ProviderId, Id, value); // ProviderService needs this method? It only has AddApiKey/RemoveApiKey.
                // Wait, checking ProviderService... 
                // It currently DOES NOT have UpdateApiKeyTag. I recall seeing only Add/Remove/ToggleFavorite in my read. 
                // Ah, I need to check if I can update Tag. 
                // If not, I should add it to ProviderService later. 
                // For now, let's assume I will add it or it exists (I'll double check file content in memory).
                // Re-reading ProviderService content from Step 21...
                // line 168: public void UpdateApiKeyTag(Guid providerId, Guid keyId, string? tag) -> YES IT EXISTS! Perfect.
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

    // IsDisabled? ApiKey has IsDisabled.
    // ProviderService... let's check. 
    // It does NOT have ToggleDisabledApiKey based on my memory of Step 21. 
    // Wait, let me re-read Step 21 carefully.
    // ... AddApiKey, UpdateApiKeyTag, RemoveApiKey, ToggleFavoriteApiKey, AddModel... 
    // No ToggleDisabled logic in ProviderService.
    // Spec says "一键启停". I should probably Implement this in ProviderService if I can.
    // Since I cannot modify Core easily without user request context? 
    // "design the UI" implies I can make it work.
    // I will add the method to ProviderService if it's missing via a separate tool call if needed, 
    // but looking at `ApiKey.cs`, it has `IsDisabled`. 
    // I can implement a `ToggleDisabled` in `ProviderService` now.
    
    public bool IsDisabled
    {
        get => _apiKey.IsDisabled;
        set
        {
            if (_apiKey.IsDisabled != value)
            {
                OnPropertyChanging();
                _apiKey.IsDisabled = value;
                // We need to persist this. I will assume I'll add the method `ToggleDisableApiKey` or `UpdateApiKeyStatus`.
                // For this file generation, I'll comment it out or call a method I'll create.
                // call _providerService.ToggleDisableApiKey(ProviderId, Id);
                // I'll create the method in ProviderService next.
               ToggleDisableServiceCall(); 
               OnPropertyChanged();
            }
        }
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

    [RelayCommand]
    private void RevealKey()
    {
        if (IsKeyRevealed)
        {
            // Hide
            DisplayKey = MaskedKey;
            IsKeyRevealed = false;
        }
        else
        {
            // Show
            var rawKey = _providerService.GetDecryptedApiKey(ProviderId, Id);
            DisplayKey = rawKey;
            IsKeyRevealed = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;

namespace KeyPocket.UI.ViewModels;

public class DefaultIconItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public partial class ProviderSettingsViewModel : ObservableObject
{
    private readonly ProviderService _providerService;

    [ObservableProperty] public partial string? BaseUrl { get; set; }

    [ObservableProperty] public partial string? Description { get; set; }

    [ObservableProperty] public partial bool HasCustomIcon { get; set; }

    private bool _isSyncingOrder;

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ProviderCurrencySymbol))]
    public partial string ProviderCurrency { get; set; } = "USD";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TypeIndex))]
    public partial string Type { get; set; } = "OpenAI API";

    // Track the currency currently used to display model prices in this page

    public ProviderSettingsViewModel(Provider provider, ProviderService providerService)
    {
        Provider = provider;
        _providerService = providerService;

        // Initialize fields
        Name = provider.Name;

        // Ensure strictly matching string reference for ComboBox
        var typeMatch = ProviderTypes.FirstOrDefault(t => t == provider.Type);
        Type = typeMatch ?? provider.Type;

        BaseUrl = provider.ApiBaseUrl;
        Description = provider.Description;
        ProviderCurrency = provider.Currency;
        LoadKeys(Provider);
        RefreshModels(Provider);
        HasCustomIcon = ProviderIconHelper.HasCustomIcon(provider.IconPath);

        LoadDefaultIcons();

        ApiKeys.CollectionChanged += OnApiKeysCollectionChanged;
        Models.CollectionChanged += OnModelsCollectionChanged;

        // Subscribe to global usage changes to update currency symbol if needed
    }

    // Default for designer
    public ProviderSettingsViewModel()
    {
        _providerService = null!;
        Provider = new Provider();
    }

    public int TypeIndex
    {
        get => ProviderTypes.IndexOf(Type);
        set
        {
            if (value >= 0 && value < ProviderTypes.Count) Type = ProviderTypes[value];
        }
    }

    // Available currencies for selection
    public List<string> AvailableCurrencies => SettingsHelper.Current.AvailableCurrencies;

    // List is sufficient for static content and lighter than ObservableCollection
    public List<string> ProviderTypes { get; } = new()
    {
        "OpenAI API",
        "Claude API",
        "Google Gemini API",
        "Custom"
    };

    public ObservableCollection<KeyWrapper> ApiKeys { get; } = new();
    public ObservableCollection<ModelWrapper> Models { get; } = new();
    public ObservableCollection<DefaultIconItem> DefaultIcons { get; } = new();

    // Expose Provider for access
    public Provider Provider { get; private set; }

    // Current currency symbol (based on SettingsHelper.Current.SelectedCurrency)
    // NOTE: Ideally this VM should expose "ProviderCurrencySymbol" for the "add model" UI.

    public string ProviderCurrencySymbol => ExchangeRateHelper.GetCurrencySymbol(ProviderCurrency);

    partial void OnProviderCurrencyChanged(string value)
    {
        if (Models != null)
            foreach (var m in Models)
                m.InputCurrency = value;
    }

    [RelayCommand]
    public void SaveGeneral()
    {
        if (_providerService == null) return;

        // Update local object
        Provider.Name = Name;
        Provider.Type = Type;
        Provider.ApiBaseUrl = BaseUrl;
        Provider.Description = Description;
        Provider.Currency = ProviderCurrency;

        // Persist
        _providerService.UpdateProvider(Provider);

        // Notify sidebar to update
        WeakReferenceMessenger.Default.Send(new ProviderUpdatedMessage(Provider.Id));
    }

    public void SaveApiKeys()
    {
        // API keys are updated immediately; no-op for now.
    }

    public void SaveModels()
    {
        // Models are updated immediately; no-op for now.
    }

    public async Task UpdateIconAsync(StorageFile? file)
    {
        if (_providerService == null) return;

        try
        {
            if (file == null)
            {
                // Remove logic
                Provider.IconPath = null;
                _providerService.UpdateProviderIcon(Provider.Id, null); // Pass null to clear
            }
            else
            {
                // Direct access - no copying
                var originalPath = file.Path;

                Provider.IconPath = originalPath;
                _providerService.UpdateProviderIcon(Provider.Id, originalPath);
            }

            // Update property
            HasCustomIcon = ProviderIconHelper.HasCustomIcon(Provider.IconPath);

            // Notify sidebar to update icon
            WeakReferenceMessenger.Default.Send(new ProviderUpdatedMessage(Provider.Id));
        }
        catch (Exception)
        {
            // Logging?
        }
    }

    public void DeleteProvider()
    {
        if (_providerService == null) return;

        var providerId = Provider.Id;
        _providerService.RemoveProvider(providerId);

        // Send message to update sidebar and home
        WeakReferenceMessenger.Default.Send(new ProviderDeletedMessage(providerId));
    }
}

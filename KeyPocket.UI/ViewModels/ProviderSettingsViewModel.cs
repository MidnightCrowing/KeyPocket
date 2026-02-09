using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;
using Microsoft.UI.Dispatching;
using WinRT.Interop;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

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

    [ObservableProperty] private string? _baseUrl;

    [ObservableProperty] private string? _description;

    [ObservableProperty] private bool _hasCustomIcon;

    private bool _isSyncingOrder;

    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ProviderCurrencySymbol))]
    private string _providerCurrency = "USD";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TypeIndex))]
    private string _type = "OpenAI API";

    // Track the currency currently used to display model prices in this page

    public ProviderSettingsViewModel(Provider provider, ProviderService providerService)
    {
        Provider = provider;
        _providerService = providerService;

        // Initialize fields
        _name = provider.Name;

        // Ensure strictly matching string reference for ComboBox
        var typeMatch = ProviderTypes.FirstOrDefault(t => t == provider.Type);
        _type = typeMatch ?? provider.Type;

        _baseUrl = provider.ApiBaseUrl;
        _description = provider.Description;
        _providerCurrency = provider.Currency;
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
    public void Save()
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

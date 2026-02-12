using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;

namespace KeyPocket.UI.ViewModels;

public partial class ModelItemViewModel : ObservableObject
{
    private static readonly string[] CapabilityOrder =
    {
        ModelTags.Text,
        ModelTags.File,
        ModelTags.Image,
        ModelTags.Audio,
        ModelTags.Video,
        ModelTags.Embeddings
    };

    internal readonly ModelInfo _model;

    private readonly string _providerCurrency;
    private readonly ProviderService _providerService;

    [ObservableProperty] public partial string? ProviderIcon { get; set; } // Keep observable if needed, though mostly static for item

    [ObservableProperty] public partial string ProviderName { get; set; }

    public ModelItemViewModel(ModelInfo model, string providerName, string? providerIcon, string providerCurrency,
        ProviderService providerService)
    {
        _model = model;
        ProviderName = providerName;
        ProviderIcon = providerIcon;
        _providerCurrency = providerCurrency ?? "USD";
        _providerService = providerService;

        // Listen for theme changes to update icon
        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (r, m) =>
        {
            OnPropertyChanged(nameof(ModelIconUri));
            OnPropertyChanged(nameof(HasModelIcon));
        });
    }

    public Uri? ModelIconUri
    {
        get
        {
            var iconName = ProviderIconHelper.GetIconForModel(Id) ?? ProviderIconHelper.GetIconForModel(DisplayName);
            if (!string.IsNullOrEmpty(iconName))
                return ProviderIconHelper.GetPresetIconUri(iconName, ThemeHelper.IsDarkTheme());
            return null; // Or fallback to ProviderIcon? Let's keep it null for now to only show special ones.
        }
    }

    public bool HasModelIcon => ModelIconUri != null;

    public string Id => _model.Id;
    public string DisplayName => _model.DisplayName;
    public Guid ProviderId => _model.ProviderId;

    public bool IsChatModel => _model.Tags.Contains(ModelTags.Text);
    public bool IsEmbeddingModel => _model.Tags.Contains(ModelTags.Embeddings);

    public decimal? InputPrice => _model.InputPricePerMTokens;
    public decimal? OutputPrice => _model.OutputPricePerMTokens;

    public IReadOnlyList<string> CapabilityTags =>
        CapabilityOrder.Where(tag => _model.Tags.Contains(tag)).ToList();

    // For sorting: returns converted price if available, else null
    public decimal? ConvertedInputPrice
    {
        get
        {
            if (!InputPrice.HasValue) return null;
            return ExchangeRateHelper.Convert(InputPrice.Value, _providerCurrency,
                SettingsHelper.Current.SelectedCurrency);
        }
    }

    // Helper for UI formatting as requested: "Null" if no price
    public string InputPriceFormatted => GetMainPrice(InputPrice);
    public string InputPriceOriginal => GetSecondaryPrice(InputPrice);

    public string OutputPriceFormatted => GetMainPrice(OutputPrice);
    public string OutputPriceOriginal => GetSecondaryPrice(OutputPrice);

    public bool IsFavorite
    {
        get => _model.Tags.Contains(ModelTags.Favorite);
        set
        {
            var currentValue = _model.Tags.Contains(ModelTags.Favorite);
            if (currentValue != value)
            {
                OnPropertyChanging();
                if (value)
                    _model.Tags.Add(ModelTags.Favorite);
                else
                    _model.Tags.Remove(ModelTags.Favorite);

                _providerService.ToggleFavoriteModel(ProviderId, Id);
                OnPropertyChanged();
            }
        }
    }

    private string GetMainPrice(decimal? price)
    {
        if (!price.HasValue) return "Null";

        var settings = SettingsHelper.Current;
        var targetCurrency = settings.SelectedCurrency;
        var sourceCurrency = _providerCurrency;

        var converted = ExchangeRateHelper.Convert(price.Value, sourceCurrency, targetCurrency);

        // If conversion fails (no rate), return original (with source symbol)
        if (converted == null)
            return $"{ExchangeRateHelper.GetCurrencySymbol(sourceCurrency)}{FormatPrice(price.Value)}"; // e.g. $0.002

        // If conversion succeeds, return converted (with target symbol)
        return $"{ExchangeRateHelper.GetCurrencySymbol(targetCurrency)}{FormatPrice(converted.Value)}";
    }

    private string GetSecondaryPrice(decimal? price)
    {
        if (!price.HasValue) return string.Empty;

        var settings = SettingsHelper.Current;
        var targetCurrency = settings.SelectedCurrency;
        var sourceCurrency = _providerCurrency;

        var converted = ExchangeRateHelper.Convert(price.Value, sourceCurrency, targetCurrency);

        // If conversion failed or target is same as source, don't show secondary
        if (converted == null || targetCurrency == sourceCurrency) return string.Empty;

        // Show original in parens
        return $"({ExchangeRateHelper.GetCurrencySymbol(sourceCurrency)}{FormatPrice(price.Value)})";
    }

    private static string FormatPrice(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void CopyId()
    {
        var package = new DataPackage();
        package.SetText(Id);
        Clipboard.SetContent(package);
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
    }
}

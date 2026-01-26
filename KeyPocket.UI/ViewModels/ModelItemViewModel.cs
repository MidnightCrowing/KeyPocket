using System;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;

namespace KeyPocket.UI.ViewModels;

public partial class ModelItemViewModel : ObservableObject
{
    private readonly ModelInfo _model;

    private readonly string _providerCurrency;
    private readonly ProviderService _providerService;

    [ObservableProperty] private string? _providerIcon; // Keep observable if needed, though mostly static for item

    [ObservableProperty] private string _providerName;

    public ModelItemViewModel(ModelInfo model, string providerName, string? providerIcon, string providerCurrency,
        ProviderService providerService)
    {
        _model = model;
        _providerName = providerName;
        _providerIcon = providerIcon;
        _providerCurrency = providerCurrency ?? "USD";
        _providerService = providerService;
    }

    public string Id => _model.Id;
    public string DisplayName => _model.DisplayName;
    public Guid ProviderId => _model.ProviderId;

    public bool IsChatModel => _model.IsChatModel;
    public bool IsEmbeddingModel => _model.IsEmbeddingModel;

    public decimal? InputPrice => _model.InputPricePerMTokens;
    public decimal? OutputPrice => _model.OutputPricePerMTokens;

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
        get => _model.IsFavorite;
        set
        {
            if (_model.IsFavorite != value)
            {
                OnPropertyChanging();
                _model.IsFavorite = value;
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
            return $"{ExchangeRateHelper.GetCurrencySymbol(sourceCurrency)}{price.Value:F3}"; // e.g. $0.002

        // If conversion succeeds, return converted (with target symbol)
        return $"{ExchangeRateHelper.GetCurrencySymbol(targetCurrency)}{converted:F3}";
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
        return $"({ExchangeRateHelper.GetCurrencySymbol(sourceCurrency)}{price.Value:F3})";
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
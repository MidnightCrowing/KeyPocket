using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.Core.Models;
using KeyPocket.UI.Helpers;
using KeyPocket.Core.Services;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace KeyPocket.UI.ViewModels;

public partial class ModelItemViewModel : ObservableObject
{
    private readonly ProviderService _providerService;
    private readonly ModelInfo _model;

    [ObservableProperty]
    private string _providerName;

    [ObservableProperty]
    private string? _providerIcon;

    public ModelItemViewModel(ModelInfo model, string providerName, string? providerIcon, ProviderService providerService)
    {
        _model = model;
        _providerName = providerName;
        _providerIcon = providerIcon;
        _providerService = providerService;
    }

    public string Id => _model.Id;
    public string DisplayName => _model.DisplayName;
    public Guid ProviderId => _model.ProviderId;
    
    public bool IsChatModel => _model.IsChatModel;
    public bool IsEmbeddingModel => _model.IsEmbeddingModel;

    public decimal? InputPrice => _model.InputPricePerMTokens;
    public decimal? OutputPrice => _model.OutputPricePerMTokens;

    // Helper for UI formatting as requested: "Null" if no price
    public string InputPriceFormatted => InputPrice.HasValue ? FormatPrice(InputPrice.Value) : "Null";
    public string OutputPriceFormatted => OutputPrice.HasValue ? FormatPrice(OutputPrice.Value) : "Null";

    private static string FormatPrice(decimal usdPrice)
    {
        var settings = SettingsHelper.Current;
        if (settings.SelectedCurrency == "CNY")
        {
            var cny = usdPrice * settings.UsdToCnyRate;
            return $"¥{cny:F3}";
        }

        // Default: USD
        return $"${usdPrice:F3}";
    }


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

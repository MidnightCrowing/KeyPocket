using System.Globalization;
using KeyPocket.UI.Helpers;

namespace KeyPocket.UI.ViewModels;

public partial class ModelWrapper
{
    public string ProviderSymbol => ExchangeRateHelper.GetCurrencySymbol(InputCurrency);

    public string CurrencySymbol => ExchangeRateHelper.GetCurrencySymbol(SettingsHelper.Current.SelectedCurrency);

    public string FavoriteIcon => IsFavorite ? "\uE735" : "\uE734";

    public string InputPriceDisplay
    {
        get
        {
            if (InputPriceValue == null) return "";
            return string.Format(CultureInfo.InvariantCulture, "{0}{1:0.###}", ProviderSymbol, InputPriceValue);
        }
    }

    public string OutputPriceDisplay
    {
        get
        {
            if (OutputPriceValue == null) return "";
            return string.Format(CultureInfo.InvariantCulture, "{0}{1:0.###}", ProviderSymbol, OutputPriceValue);
        }
    }

    public void RefreshCurrencySymbol()
    {
        OnPropertyChanged(nameof(CurrencySymbol));
    }
}

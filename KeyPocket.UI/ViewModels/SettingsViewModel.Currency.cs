using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.UI.Helpers;

namespace KeyPocket.UI.ViewModels;

public partial class SettingsViewModel
{
    [ObservableProperty] public partial bool IsAddingCurrency { get; set; }

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmAddCurrencyCommand))]
    public partial string NewCurrencyCode { get; set; } = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmAddCurrencyCommand))]
    public partial string NewCurrencySymbol { get; set; } = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCurrencyCommand))]
    public partial string SelectedCurrency { get; set; }

    [RelayCommand]
    private void StartAddCurrency()
    {
        NewCurrencyCode = string.Empty;
        NewCurrencySymbol = string.Empty;
        IsAddingCurrency = true;
    }

    [RelayCommand]
    private void CancelAddCurrency()
    {
        IsAddingCurrency = false;
        NewCurrencyCode = string.Empty;
    }

    private bool CanAddCurrency()
    {
        if (string.IsNullOrWhiteSpace(NewCurrencyCode) || string.IsNullOrWhiteSpace(NewCurrencySymbol))
            return false;

        var code = NewCurrencyCode.Trim();
        if (code.Any(char.IsDigit) || code.Length < 2) return false;

        return true;
    }

    [RelayCommand(CanExecute = nameof(CanAddCurrency))]
    private void ConfirmAddCurrency()
    {
        var code = NewCurrencyCode.Trim().ToUpper();
        var symbol = NewCurrencySymbol.Trim();

        if (!AvailableCurrencies.Contains(code))
        {
            AvailableCurrencies.Add(code);
            var list = SettingsHelper.Current.AvailableCurrencies;
            list.Add(code);
            SettingsHelper.Current.AvailableCurrencies = list;

            var symbols = SettingsHelper.Current.CurrencySymbols;
            symbols[code] = symbol;
            SettingsHelper.Current.CurrencySymbols = symbols;

            SelectedCurrency = code;
        }
        else
        {
            var symbols = SettingsHelper.Current.CurrencySymbols;
            if (!symbols.ContainsKey(code) || symbols[code] != symbol)
            {
                symbols[code] = symbol;
                SettingsHelper.Current.CurrencySymbols = symbols;
            }
        }

        IsAddingCurrency = false;
        NewCurrencyCode = string.Empty;
        NewCurrencySymbol = string.Empty;
    }

    private bool CanDeleteSelectedCurrency()
    {
        return !string.IsNullOrEmpty(SelectedCurrency) && SelectedCurrency != "USD" && SelectedCurrency != "CNY";
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedCurrency))]
    private void DeleteSelectedCurrency()
    {
        var current = SelectedCurrency;
        if (string.IsNullOrEmpty(current) || current == "USD" || current == "CNY") return;

        if (AvailableCurrencies.Contains(current))
        {
            AvailableCurrencies.Remove(current);
            var list = SettingsHelper.Current.AvailableCurrencies;
            list.Remove(current);
            SettingsHelper.Current.AvailableCurrencies = list;

            var symbols = SettingsHelper.Current.CurrencySymbols;
            if (symbols.ContainsKey(current))
            {
                symbols.Remove(current);
                SettingsHelper.Current.CurrencySymbols = symbols;
            }

            SelectedCurrency = "USD";
        }
    }

    partial void OnSelectedCurrencyChanged(string value)
    {
        SettingsHelper.Current.SelectedCurrency = value;
    }

    private void LoadCurrencies()
    {
        AvailableCurrencies.Clear();
        foreach (var c in SettingsHelper.Current.AvailableCurrencies) AvailableCurrencies.Add(c);
    }
}

using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.UI.Helpers;

namespace KeyPocket.UI.ViewModels;

public partial class SettingsViewModel
{
    [ObservableProperty] private bool _isAddingRate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomRateCommand))]
    private string _newRateSource = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomRateCommand))]
    private string _newRateTarget = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomRateCommand))]
    private double _newRateValue = double.NaN;

    partial void OnNewRateValueChanged(double value)
    {
        if (!double.IsNaN(value))
        {
            var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
            if (Math.Abs(value - rounded) > double.Epsilon) NewRateValue = rounded;
        }
    }

    private void LoadRates()
    {
        Rates.Clear();
        var exchangeRates = SettingsHelper.Current.ExchangeRates;
        foreach (var kvp in exchangeRates)
        {
            var parts = kvp.Key.Split('_');
            if (parts.Length == 2) Rates.Add(new ExchangeRateItem(parts[0], parts[1], kvp.Value, OnRateItemChanged));
        }

        Rates.Add(new AddRatePlaceholder());
    }

    private void OnRateItemChanged(string key, decimal newRate)
    {
        var rates = SettingsHelper.Current.ExchangeRates;
        rates[key] = newRate;
        SettingsHelper.Current.ExchangeRates = rates;
    }

    [RelayCommand]
    private void DeleteRate(ExchangeRateItem item)
    {
        if (item == null) return;
        Rates.Remove(item);

        var rates = SettingsHelper.Current.ExchangeRates;
        var key = $"{item.Source}_{item.Target}";
        if (rates.ContainsKey(key))
        {
            rates.Remove(key);
            SettingsHelper.Current.ExchangeRates = rates;
        }

        if (!Rates.Any(x => x is AddRatePlaceholder)) Rates.Add(new AddRatePlaceholder());
    }

    [RelayCommand]
    private void StartAddRate()
    {
        NewRateSource = string.Empty;
        NewRateTarget = SelectedCurrency;
        NewRateValue = double.NaN;
        IsAddingRate = true;
    }

    [RelayCommand]
    private void CancelAddRate()
    {
        IsAddingRate = false;
        NewRateValue = double.NaN;
    }

    private bool CanAddCustomRate()
    {
        if (double.IsNaN(NewRateValue) || NewRateValue <= 0) return false;
        if (string.IsNullOrWhiteSpace(NewRateSource) || string.IsNullOrWhiteSpace(NewRateTarget)) return false;
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanAddCustomRate))]
    private void AddCustomRate()
    {
        if (double.IsNaN(NewRateValue)) return;
        if (string.IsNullOrWhiteSpace(NewRateSource) || string.IsNullOrWhiteSpace(NewRateTarget)) return;

        var key = $"{NewRateSource.Trim().ToUpper()}_{NewRateTarget.Trim().ToUpper()}";
        var rates = SettingsHelper.Current.ExchangeRates;
        rates[key] = (decimal)NewRateValue;
        SettingsHelper.Current.ExchangeRates = rates;

        var newItem = new ExchangeRateItem(
            NewRateSource.Trim().ToUpper(),
            NewRateTarget.Trim().ToUpper(),
            (decimal)NewRateValue,
            OnRateItemChanged);

        var placeholder = Rates.FirstOrDefault(x => x is AddRatePlaceholder);
        if (placeholder != null)
        {
            var index = Rates.IndexOf(placeholder);
            Rates.Insert(index, newItem);
        }
        else
        {
            Rates.Add(newItem);
            Rates.Add(new AddRatePlaceholder());
        }

        IsAddingRate = false;
        NewRateValue = double.NaN;
    }
}

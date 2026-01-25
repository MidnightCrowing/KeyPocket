using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace KeyPocket.UI.ViewModels;

public partial class ExchangeRateItem : ObservableObject
{
    public string Source { get; }
    public string Target { get; }

    [ObservableProperty]
    private decimal _rate;

    public ExchangeRateItem(string source, string target, decimal rate, Action<string, decimal> onRateChanged)
    {
        Source = source;
        Target = target;
        _rate = rate;
        _onRateChanged = onRateChanged;
    }

    private readonly Action<string, decimal> _onRateChanged;

    partial void OnRateChanged(decimal value)
    {
        var key = $"{Source.ToUpper()}_{Target.ToUpper()}";
        _onRateChanged?.Invoke(key, value);
        OnPropertyChanged(nameof(RateValue));
    }
    
    public double RateValue
    {
        get => (double)Rate;
        set => Rate = (decimal)value;
    }
}

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _themeIndex;

    // --- Currency Selection ---

    public ObservableCollection<string> AvailableCurrencies { get; private set; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCurrencyCommand))]
    private string _selectedCurrency;

    [ObservableProperty]
    private bool _isAddingCurrency;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmAddCurrencyCommand))]
    private string _newCurrencyCode = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmAddCurrencyCommand))]
    private string _newCurrencySymbol = string.Empty;

    // ...

    // ... Commands ...

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
        // Must have code and symbol
        if (string.IsNullOrWhiteSpace(NewCurrencyCode) || string.IsNullOrWhiteSpace(NewCurrencySymbol))
            return false;
        
        // Code usually 3 letters, but user said 'specifications'
        // Let's enforce non-empty and maybe no digits for code, max len is controlled by UI
        string code = NewCurrencyCode.Trim();
        if (code.Any(char.IsDigit) || code.Length < 2) return false;

        return true;
    }

    [RelayCommand(CanExecute = nameof(CanAddCurrency))]
    private void ConfirmAddCurrency()
    {
        // Logic remains similar but simplified since checks are done
        string code = NewCurrencyCode.Trim().ToUpper();
        string symbol = NewCurrencySymbol.Trim();
        
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
        // Only allow deleting non-default currencies
        // "USD" and "CNY" are hardcoded defaults in this context
        return !string.IsNullOrEmpty(SelectedCurrency) && SelectedCurrency != "USD" && SelectedCurrency != "CNY";
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedCurrency))]
    private void DeleteSelectedCurrency()
    {
        string current = SelectedCurrency;
        // Prevent deleting defaults or if empty
        if (string.IsNullOrEmpty(current) || current == "USD" || current == "CNY") return;

        if (AvailableCurrencies.Contains(current))
        {
            // Remove from list
            AvailableCurrencies.Remove(current);
            var list = SettingsHelper.Current.AvailableCurrencies;
            list.Remove(current);
            SettingsHelper.Current.AvailableCurrencies = list;

            // Remove symbol
            var symbols = SettingsHelper.Current.CurrencySymbols;
            if (symbols.ContainsKey(current))
            {
                symbols.Remove(current);
                SettingsHelper.Current.CurrencySymbols = symbols;
            }

            // Fallback selection
            SelectedCurrency = "USD";
        }
    }

    // --- Exchange Rates ---

    public ObservableCollection<ExchangeRateItem> Rates { get; private set; } = new();

    // New Custom Rate Input
    [ObservableProperty]
    private bool _isAddingRate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomRateCommand))]
    private string _newRateSource = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomRateCommand))]
    private string _newRateTarget = string.Empty;

    // Use double.NaN to represent empty/unset for NumberBox
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomRateCommand))]
    private double _newRateValue = double.NaN;

    partial void OnNewRateValueChanged(double value)
    {
        if (!double.IsNaN(value))
        {
            // Enforce 2 decimal places to match UI and prevent float drift
            double rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
            if (Math.Abs(value - rounded) > double.Epsilon)
            {
                NewRateValue = rounded;
            }
        }
    }

    public string Version
    {
        get
        {
            return ProcessInfoHelper.GetVersion() is Version version
                ? string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision)
                : string.Empty;
        }
    }

    public SettingsViewModel()
    {
        // 1. Theme
        _themeIndex = ThemeHelper.Theme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };

        // 2. Currencies
        LoadCurrencies();
        _selectedCurrency = SettingsHelper.Current.SelectedCurrency;

        // 3. Rates
        LoadRates();
    }

    private void LoadCurrencies()
    {
        AvailableCurrencies.Clear();
        foreach (var c in SettingsHelper.Current.AvailableCurrencies)
        {
            AvailableCurrencies.Add(c);
        }
    }

    private void LoadRates()
    {
        Rates.Clear();
        foreach (var kvp in SettingsHelper.Current.ExchangeRates)
        {
            // Key format: SOURCE_TARGET
            var parts = kvp.Key.Split('_');
            if (parts.Length == 2)
            {
                Rates.Add(new ExchangeRateItem(parts[0], parts[1], kvp.Value, OnRateItemChanged));
            }
        }
    }

    private void OnRateItemChanged(string key, decimal newRate)
    {
        var rates = SettingsHelper.Current.ExchangeRates;
        rates[key] = newRate;
        SettingsHelper.Current.ExchangeRates = rates; // Trigger save
    }



    [RelayCommand]
    private void DeleteRate(ExchangeRateItem item)
    {
        if (item == null) return;
        Rates.Remove(item);

        var rates = SettingsHelper.Current.ExchangeRates;
        string key = $"{item.Source}_{item.Target}";
        if (rates.ContainsKey(key))
        {
            rates.Remove(key);
            SettingsHelper.Current.ExchangeRates = rates; // Save
        }
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
        // Check for NaN or 0/negative if applicable, though 0 might be valid technically but weird for rate. 
        // Let's assume > 0 is required usually, but user didn't specify validation strictness.
        if (!double.IsNaN(NewRateValue))
        {
            if (string.IsNullOrWhiteSpace(NewRateSource) || string.IsNullOrWhiteSpace(NewRateTarget)) return;

            string key = $"{NewRateSource.Trim().ToUpper()}_{NewRateTarget.Trim().ToUpper()}";
            var rates = SettingsHelper.Current.ExchangeRates;
            rates[key] = (decimal)NewRateValue;
            SettingsHelper.Current.ExchangeRates = rates; // Trigger save

            // Visual update
            LoadRates(); // Reload list to show new item

            IsAddingRate = false;
            NewRateValue = double.NaN;
        }
    }


    partial void OnSelectedCurrencyChanged(string value)
    {
        SettingsHelper.Current.SelectedCurrency = value;
    }

    partial void OnThemeIndexChanged(int value)
    {
        ThemeHelper.Theme = value switch
        {
            0 => ElementTheme.Light,
            1 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    [RelayCommand]
    private async Task OpenGitHubAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/MidnightCrowing/KeyPocket"));
    }

    [RelayCommand]
    private async Task OpenFeedbackAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/MidnightCrowing/KeyPocket/issues"));
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml;

namespace KeyPocket.UI.ViewModels;

public partial class ExchangeRateItem : ObservableObject
{
    private readonly Action<string, decimal> _onRateChanged;

    [ObservableProperty] private decimal _rate;

    public ExchangeRateItem(string source, string target, decimal rate, Action<string, decimal> onRateChanged)
    {
        Source = source;
        Target = target;
        _rate = rate;
        _onRateChanged = onRateChanged;
    }

    public string Source { get; }
    public string Target { get; }

    public double RateValue
    {
        get => (double)Rate;
        set => Rate = (decimal)value;
    }

    partial void OnRateChanged(decimal value)
    {
        var key = $"{Source.ToUpper()}_{Target.ToUpper()}";
        _onRateChanged?.Invoke(key, value);
        OnPropertyChanged(nameof(RateValue));
    }
}

public class AddRatePlaceholder
{
    // Just a placeholder type
}

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isAddingCurrency;

    // New Custom Rate Input
    [ObservableProperty] private bool _isAddingRate;

    [ObservableProperty] private int _languageIndex;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmAddCurrencyCommand))]
    private string _newCurrencyCode = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmAddCurrencyCommand))]
    private string _newCurrencySymbol = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(AddCustomRateCommand))]
    private string _newRateSource = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(AddCustomRateCommand))]
    private string _newRateTarget = string.Empty;

    // Use double.NaN to represent empty/unset for NumberBox
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(AddCustomRateCommand))]
    private double _newRateValue = double.NaN;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCurrencyCommand))]
    private string _selectedCurrency;

    [ObservableProperty] private bool _showRestartWarning;

    [ObservableProperty] private int _themeIndex;

    public SettingsViewModel()
    {
        // 1. Theme
        _themeIndex = ThemeHelper.Theme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };

        // 1.5 Language
        var lang = ApplicationLanguages.PrimaryLanguageOverride;
        if (string.IsNullOrEmpty(lang))
            _languageIndex = 0; // Auto
        else if (lang.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            _languageIndex = 1;
        else if (lang.StartsWith("zh-CN", StringComparison.OrdinalIgnoreCase) ||
                 lang.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase))
            _languageIndex = 2;
        else if (lang.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                 lang.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
            _languageIndex = 3;
        else
            _languageIndex = 0; // Default to Auto for others


        // 2. Currencies
        LoadCurrencies();
        _selectedCurrency = SettingsHelper.Current.SelectedCurrency;

        // 3. Rates
        LoadRates();
    }

    // --- Currency Selection ---

    public ObservableCollection<string> AvailableCurrencies { get; } = new();

    // --- Exchange Rates ---

    // 使用 object 以容纳 ExchangeRateItem 和 AddRatePlaceholder
    public ObservableCollection<object> Rates { get; } = new();

    public string Version =>
        ProcessInfoHelper.GetVersion() is Version version
            ? string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision)
            : string.Empty;

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

        var code = NewCurrencyCode.Trim();
        if (code.Any(char.IsDigit) || code.Length < 2) return false;

        return true;
    }

    [RelayCommand(CanExecute = nameof(CanAddCurrency))]
    private void ConfirmAddCurrency()
    {
        // Logic remains similar but simplified since checks are done
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
        // Only allow deleting non-default currencies
        // "USD" and "CNY" are hardcoded defaults in this context
        return !string.IsNullOrEmpty(SelectedCurrency) && SelectedCurrency != "USD" && SelectedCurrency != "CNY";
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedCurrency))]
    private void DeleteSelectedCurrency()
    {
        var current = SelectedCurrency;
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

    partial void OnNewRateValueChanged(double value)
    {
        if (!double.IsNaN(value))
        {
            // Enforce 2 decimal places to match UI and prevent float drift
            var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
            if (Math.Abs(value - rounded) > double.Epsilon) NewRateValue = rounded;
        }
    }

    private void LoadCurrencies()
    {
        AvailableCurrencies.Clear();
        foreach (var c in SettingsHelper.Current.AvailableCurrencies) AvailableCurrencies.Add(c);
    }

    private void LoadRates()
    {
        Rates.Clear();
        var exchangeRates = SettingsHelper.Current.ExchangeRates;
        foreach (var kvp in exchangeRates)
        {
            // Key format: SOURCE_TARGET
            var parts = kvp.Key.Split('_');
            if (parts.Length == 2) Rates.Add(new ExchangeRateItem(parts[0], parts[1], kvp.Value, OnRateItemChanged));
        }

        // 始终在最后添加 Placeholder
        Rates.Add(new AddRatePlaceholder());
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
        var key = $"{item.Source}_{item.Target}";
        if (rates.ContainsKey(key))
        {
            rates.Remove(key);
            SettingsHelper.Current.ExchangeRates = rates; // Save
        }

        // 确保 Placeholder 还在
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
        if (!double.IsNaN(NewRateValue))
        {
            if (string.IsNullOrWhiteSpace(NewRateSource) || string.IsNullOrWhiteSpace(NewRateTarget)) return;

            var key = $"{NewRateSource.Trim().ToUpper()}_{NewRateTarget.Trim().ToUpper()}";
            var rates = SettingsHelper.Current.ExchangeRates;
            rates[key] = (decimal)NewRateValue;
            SettingsHelper.Current.ExchangeRates = rates; // Trigger save

            // Visual update - insert BEFORE the placeholder
            var newItem = new ExchangeRateItem(NewRateSource.Trim().ToUpper(), NewRateTarget.Trim().ToUpper(),
                (decimal)NewRateValue, OnRateItemChanged);

            // 找到 Placeholder 的位置
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

    partial void OnLanguageIndexChanged(int value)
    {
        var newLang = value switch
        {
            1 => "en-US",
            2 => "zh-CN",
            3 => "zh-TW",
            _ => string.Empty // Auto
        };

        if (ApplicationLanguages.PrimaryLanguageOverride != newLang)
        {
            ApplicationLanguages.PrimaryLanguageOverride = newLang;
            ShowRestartWarning = true;
        }
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
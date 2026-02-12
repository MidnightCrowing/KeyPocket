using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KeyPocket.UI.ViewModels;

public partial class ExchangeRateItem : ObservableObject
{
    private readonly Action<string, decimal> _onRateChanged;

    [ObservableProperty] public partial decimal Rate { get; set; }

    public ExchangeRateItem(string source, string target, decimal rate, Action<string, decimal> onRateChanged)
    {
        Source = source ?? string.Empty;
        Target = target ?? string.Empty;
        _onRateChanged = onRateChanged;
        Rate = rate;
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
        if (string.IsNullOrWhiteSpace(Source) || string.IsNullOrWhiteSpace(Target))
        {
            OnPropertyChanged(nameof(RateValue));
            return;
        }

        var key = $"{Source.ToUpper()}_{Target.ToUpper()}";
        _onRateChanged?.Invoke(key, value);
        OnPropertyChanged(nameof(RateValue));
    }
}

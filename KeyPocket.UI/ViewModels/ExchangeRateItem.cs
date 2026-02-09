using System;
using CommunityToolkit.Mvvm.ComponentModel;

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

using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KeyPocket.UI.ViewModels;

public partial class ExchangeRateItem : ObservableObject
{
    private readonly Action<string, decimal> _onRateChanged;

    [ObservableProperty] public partial decimal Rate { get; set; }

    public ExchangeRateItem(string source, string target, decimal rate, Action<string, decimal> onRateChanged)
    {
        Source = source;
        Target = target;
        Rate = rate;
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
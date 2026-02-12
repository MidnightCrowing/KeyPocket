using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace KeyPocket.UI.ViewModels;

public partial class ModelWrapper
{
    [ObservableProperty] public partial string InputPrice { get; set; } = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsReadOnly))]
    public partial bool IsEditing { get; set; }

    [ObservableProperty] public partial string NewId { get; set; } = string.Empty;

    [ObservableProperty] public partial string NewName { get; set; } = string.Empty;

    [ObservableProperty] public partial string OutputPrice { get; set; } = string.Empty;

    public bool IsReadOnly => !IsEditing;

    public bool HasInputPrice => InputPriceValue.HasValue && InputPriceValue.Value > 0;

    public bool HasOutputPrice => OutputPriceValue.HasValue && OutputPriceValue.Value > 0;

    public double InputPriceValueOrZero
    {
        get => InputPriceValue ?? 0;
        set => InputPriceValue = value;
    }

    public double OutputPriceValueOrZero
    {
        get => OutputPriceValue ?? 0;
        set => OutputPriceValue = value;
    }

    public ICommand? StartEditCommand { get; set; }

    public ICommand? ToggleFavoriteCommand { get; set; }

    public ICommand? DeleteCommand { get; set; }

    public ICommand? ConfirmAddCommand { get; set; }

    public ICommand? CancelAddCommand { get; set; }

    public ICommand? ConfirmEditCommand { get; set; }

    public ICommand? CancelEditCommand { get; set; }

    partial void OnNewIdChanged(string value)
    {
        (ConfirmAddCommand as IRelayCommand)?.NotifyCanExecuteChanged();
    }
}

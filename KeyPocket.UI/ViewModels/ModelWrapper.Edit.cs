using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace KeyPocket.UI.ViewModels;

public partial class ModelWrapper
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReadOnly))]
    private bool _isEditing;

    [ObservableProperty] private string _newId = string.Empty;

    partial void OnNewIdChanged(string value)
    {
        (ConfirmAddCommand as IRelayCommand)?.NotifyCanExecuteChanged();
    }

    [ObservableProperty] private string _newName = string.Empty;

    [ObservableProperty] private string _inputPrice = string.Empty;

    [ObservableProperty] private string _outputPrice = string.Empty;

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
}

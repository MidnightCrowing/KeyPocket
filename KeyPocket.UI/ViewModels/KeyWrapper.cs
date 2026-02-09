using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace KeyPocket.UI.ViewModels;

public partial class KeyWrapper : ObservableObject
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsReadOnly))]
    private bool _isEditing;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(FavoriteIcon))]
    private bool _isFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTagDisplayVisible))]
    [NotifyPropertyChangedFor(nameof(IsTagTextVisible))]
    private bool _isTagEditing;

    [ObservableProperty] private string _maskedKey = "";

    [ObservableProperty] private string _newKey = "";
    private string? _originalTag;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTag))]
    [NotifyPropertyChangedFor(nameof(IsTagTextVisible))]
    [NotifyPropertyChangedFor(nameof(IsTagDisplayVisible))]
    private string? _tag;

    public Guid Id { get; set; }

    public bool HasTag => !string.IsNullOrWhiteSpace(Tag);

    public bool IsReadOnly => !IsEditing;

    // Show icon button when: not editing AND no tag
    public bool IsTagDisplayVisible => !IsTagEditing && !HasTag;

    // Show tag text button when: not editing AND has tag
    public bool IsTagTextVisible => !IsTagEditing && HasTag;

    // Commands
    public ICommand? ToggleFavoriteCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
    public ICommand? ConfirmAddCommand { get; set; }
    public ICommand? CancelAddCommand { get; set; }
    public ICommand? CopyKeyCommand { get; set; }

    // Tag Commands
    public ICommand? StartEditTagCommand { get; set; }
    public ICommand? CommitTagEditCommand { get; set; }
    public ICommand? CancelTagEditCommand { get; set; }

    public string FavoriteIcon => IsFavorite ? "\uE735" : "\uE734";

    public void SetOriginalTag()
    {
        _originalTag = Tag;
    }

    public void RestoreOriginalTag()
    {
        Tag = _originalTag;
        _originalTag = null;
    }

    partial void OnNewKeyChanged(string value)
    {
        (ConfirmAddCommand as IRelayCommand)?.NotifyCanExecuteChanged();
    }
}
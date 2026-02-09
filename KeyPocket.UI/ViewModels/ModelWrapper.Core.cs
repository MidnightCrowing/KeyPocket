using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Models;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;

namespace KeyPocket.UI.ViewModels;

public partial class ModelWrapper : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModelIconUri))]
    [NotifyPropertyChangedFor(nameof(HasModelIcon))]
    private string _id = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProviderSymbol))]
    [NotifyPropertyChangedFor(nameof(InputPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(OutputPriceDisplay))]
    private string _inputCurrency = "USD";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InputPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(OutputPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(HasInputPrice))]
    private double? _inputPriceValue;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(FavoriteIcon))]
    private bool _isFavorite;

    private bool _isSyncingFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModelIconUri))]
    [NotifyPropertyChangedFor(nameof(HasModelIcon))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InputPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(OutputPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(HasOutputPrice))]
    private double? _outputPriceValue;

    public ModelWrapper()
    {
        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (r, m) =>
        {
            OnPropertyChanged(nameof(ModelIconUri));
            OnPropertyChanged(nameof(HasModelIcon));
        });
    }

    // Shared list of available tags for binding (Static backing)
    public static List<string> AvailableTagsList { get; } = new()
    {
        ModelTags.Text,
        ModelTags.File,
        ModelTags.Image,
        ModelTags.Audio,
        ModelTags.Video,
        ModelTags.Embeddings,
        ModelTags.Favorite,
        ModelTags.Deprecated
    };

    // Dynamic suggestions based on input and available tags
    public ObservableCollection<string> SuggestedTags { get; } = new();

    public ObservableCollection<string> Tags { get; } = new();

    public Uri? ModelIconUri
    {
        get
        {
            var iconName = ProviderIconHelper.GetIconForModel(Name) ?? ProviderIconHelper.GetIconForModel(Id);
            if (!string.IsNullOrEmpty(iconName))
                return ProviderIconHelper.GetPresetIconUri(iconName, ThemeHelper.IsDarkTheme());

            return null;
        }
    }

    public bool HasModelIcon => ModelIconUri != null;

    partial void OnIsFavoriteChanged(bool value)
    {
        if (_isSyncingFavorite) return;

        try
        {
            _isSyncingFavorite = true;
            if (value)
            {
                if (!Tags.Contains(ModelTags.Favorite)) Tags.Add(ModelTags.Favorite);
            }
            else
            {
                if (Tags.Contains(ModelTags.Favorite)) Tags.Remove(ModelTags.Favorite);
            }

            // Update suggestions after tag change
            UpdateSuggestions("");
        }
        finally
        {
            _isSyncingFavorite = false;
        }

        OnPropertyChanged(nameof(FavoriteIcon));
    }

    public void InitializeTags()
    {
        Tags.CollectionChanged -= OnTagsCollectionChanged;

        // One-time sync: ensure IsFavorite and Tags are in sync before attaching listener
        var hasFavoriteTag = Tags.Contains(ModelTags.Favorite);
        if (IsFavorite && !hasFavoriteTag)
            Tags.Add(ModelTags.Favorite);
        else if (!IsFavorite && hasFavoriteTag) Tags.Remove(ModelTags.Favorite);

        Tags.CollectionChanged += OnTagsCollectionChanged;
        // Initial populate of suggestions
        UpdateSuggestions("");
    }

    private void OnTagsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncingFavorite)
        {
            UpdateSuggestions("");
            return;
        }

        try
        {
            _isSyncingFavorite = true;
            var hasFavorite = Tags.Contains(ModelTags.Favorite);
            if (IsFavorite != hasFavorite) IsFavorite = hasFavorite;
        }
        finally
        {
            _isSyncingFavorite = false;
        }

        UpdateSuggestions("");
    }

    public void UpdateSuggestions(string input)
    {
        var currentInput = input?.Trim() ?? "";

        var unselected = AvailableTagsList.Where(t => !Tags.Contains(t)).ToList();
        var filtered = string.IsNullOrEmpty(currentInput)
            ? unselected
            : unselected.Where(t => t.Contains(currentInput, StringComparison.OrdinalIgnoreCase)).ToList();

        SuggestedTags.Clear();
        foreach (var tag in filtered) SuggestedTags.Add(tag);
    }
}
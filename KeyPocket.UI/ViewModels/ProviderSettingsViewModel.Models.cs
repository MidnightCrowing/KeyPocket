using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Models;
using KeyPocket.UI.Messages;

namespace KeyPocket.UI.ViewModels;

public partial class ProviderSettingsViewModel : ObservableObject
{
    // --- Models ---

    /// <summary>
    ///     Refreshes the Models collection incrementally, preserving editing state.
    ///     Only updates/adds changed models, does not clear the entire list.
    /// </summary>
    /// <param name="current">Optional provider to load from, otherwise reloads from service</param>
    private void RefreshModels(Provider? current = null)
    {
        if (current == null) current = GetProviderFromService(Provider.Id);
        if (current == null) return;
        Provider = current;

        _isSyncingOrder = true;
        try
        {
            var existingWrapperIds = Models.Select(w => w.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Add new models that don't exist in UI
            foreach (var model in Provider.Models)
                if (!existingWrapperIds.Contains(model.Id))
                {
                    var wrapper = new ModelWrapper
                    {
                        Id = model.Id,
                        Name = model.DisplayName,
                        InputPriceValue = (double)(model.InputPricePerMTokens ?? 0),
                        OutputPriceValue = (double)(model.OutputPricePerMTokens ?? 0),
                        InputCurrency = Provider.Currency ?? "USD",
                        IsFavorite = model.Tags?.Contains(ModelTags.Favorite) ?? false,
                        IsEditing = false
                    };

                    wrapper.InputPrice = wrapper.InputPriceValue?.ToString() ?? string.Empty;
                    wrapper.OutputPrice = wrapper.OutputPriceValue?.ToString() ?? string.Empty;

                    if (model.Tags != null)
                        foreach (var tag in model.Tags)
                            wrapper.Tags.Add(tag);

                    InjectModelCommands(wrapper);
                    wrapper.InitializeTags(); // Hook up events and initial suggestions
                    Models.Add(wrapper);
                }

            // Update existing wrappers that were modified (only if not editing)
            foreach (var wrapper in Models.ToList())
            {
                var model = Provider.Models.FirstOrDefault(m =>
                    m.Id.Equals(wrapper.Id, StringComparison.OrdinalIgnoreCase));
                if (model != null && !wrapper.IsEditing)
                {
                    // Update wrapper properties from reloaded model
                    wrapper.Name = model.DisplayName;
                    wrapper.InputPriceValue = (double)(model.InputPricePerMTokens ?? 0);
                    wrapper.OutputPriceValue = (double)(model.OutputPricePerMTokens ?? 0);
                    wrapper.InputCurrency = Provider.Currency ?? "USD";
                    wrapper.IsFavorite = model.Tags?.Contains(ModelTags.Favorite) ?? false;

                    wrapper.InputPrice = wrapper.InputPriceValue?.ToString() ?? string.Empty;
                    wrapper.OutputPrice = wrapper.OutputPriceValue?.ToString() ?? string.Empty;

                    wrapper.Tags.Clear();
                    if (model.Tags != null)
                        foreach (var tag in model.Tags)
                            wrapper.Tags.Add(tag);

                    wrapper.InitializeTags();
                }
                else if (model == null)
                {
                    // Model was deleted, remove wrapper
                    Models.Remove(wrapper);
                }
            }
        }
        finally
        {
            _isSyncingOrder = false;
        }
    }

    private void InjectModelCommands(ModelWrapper w)
    {
        w.ToggleFavoriteCommand = new RelayCommand(() => ToggleFavoriteModel(w));
        w.DeleteCommand = new RelayCommand(() => DeleteModel(w));
        w.ConfirmAddCommand = new RelayCommand(() => ConfirmAddModel(w), () => !string.IsNullOrWhiteSpace(w.NewId));
        w.CancelAddCommand = new RelayCommand(() => CancelAddModel(w));
        w.StartEditCommand = new RelayCommand(() => StartEditModel(w));
        w.ConfirmEditCommand = new RelayCommand(() => ConfirmEditModel(w));
        w.CancelEditCommand = new RelayCommand(() => CancelEditModel(w));
    }

    [RelayCommand]
    public void AddModel()
    {
        // Add a placeholder for editing
        var w = new ModelWrapper
        {
            Id = string.Empty, // New
            IsEditing = true,
            NewId = "",
            NewName = "",
            InputPriceValue = 0,
            OutputPriceValue = 0,
            InputCurrency = ProviderCurrency ?? "USD" // Use current selection
        };
        w.Tags.Add(ModelTags.Text); // Default tag for new models
        InjectModelCommands(w);
        w.InitializeTags(); // Hook up events and initial suggestions
        Models.Add(w);
    }

    private void ConfirmAddModel(ModelWrapper? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.NewId)) return;

        decimal? inputPrice = null;
        decimal? outputPrice = null;

        if (item.InputPriceValue > 0)
            // Direct assignment, assuming input is in Provider Currency
            inputPrice = (decimal)item.InputPriceValue;

        if (item.OutputPriceValue > 0)
            // Direct assignment, assuming input is in Provider Currency
            outputPrice = (decimal)item.OutputPriceValue;

        // Check if this is editing an existing model or adding a new one
        if (string.IsNullOrEmpty(item.Id)) // New model
        {
            var model = new ModelInfo
            {
                Id = item.NewId,
                DisplayName = string.IsNullOrWhiteSpace(item.NewName)
                    ? FormatDefaultModelName(item.NewId)
                    : item.NewName,
                ProviderId = Provider.Id,
                InputPricePerMTokens = inputPrice,
                OutputPricePerMTokens = outputPrice,
                Tags = new HashSet<string>(item.Tags)
            };

            _providerService.AddModel(Provider.Id, model);

            // Refresh provider to get clean state but don't reload list
            var current = GetProviderFromService(Provider.Id);
            if (current != null) Provider = current;

            // Update item in-place
            item.Id = model.Id;
            item.Name = model.DisplayName;
            item.InputPriceValue = (double)(inputPrice ?? 0);
            item.OutputPriceValue = (double)(outputPrice ?? 0);

            // Format strings for display if needed, but they are bound to InputPriceValue/OutputPriceValue usually via converter 
            // Wrapper properties:
            item.InputPrice = item.InputPriceValue?.ToString() ?? string.Empty;
            item.OutputPrice = item.OutputPriceValue?.ToString() ?? string.Empty;

            item.IsEditing = false;
        }
        else // Editing existing model
        {
            var model = Provider.Models.FirstOrDefault(m => m.Id == item.Id);
            if (model != null)
            {
                // Update model properties
                model.Id = item.NewId;
                model.DisplayName = string.IsNullOrWhiteSpace(item.NewName) ? item.NewId : item.NewName;
                model.InputPricePerMTokens = inputPrice;
                model.OutputPricePerMTokens = outputPrice;
                model.Tags = new HashSet<string>(item.Tags);

                // Save changes
                _providerService.UpdateProvider(Provider);

                // Refresh provider locally
                var current = GetProviderFromService(Provider.Id);
                if (current != null) Provider = current;

                // Update wrapper
                item.Id = model.Id;
                item.Name = model.DisplayName;
                item.IsEditing = false;
            }
        }
    }

    private void CancelAddModel(ModelWrapper? item)
    {
        if (item == null) return;
        if (string.IsNullOrEmpty(item.Id)) // New model being added
            Models.Remove(item);
        else
            item.IsEditing = false;
    }

    private void StartEditModel(ModelWrapper? item)
    {
        if (item == null || string.IsNullOrEmpty(item.Id)) return;

        // Populate edit fields with current values
        item.NewId = item.Id;
        item.NewName = item.Name;

        // Get current model to populate prices
        var model = Provider.Models.FirstOrDefault(m => m.Id == item.Id);
        if (model != null)
        {
            // Load stored prices (in Provider.Currency)
            // Initialize InputCurrency to current ProviderCurrency (reflecting any unsaved changes in dropdown)
            item.InputCurrency = ProviderCurrency ?? "USD";

            item.InputPriceValue = (double)(model.InputPricePerMTokens ?? 0);
            item.OutputPriceValue = (double)(model.OutputPricePerMTokens ?? 0);

            item.Tags.Clear();
            if (model.Tags != null)
                foreach (var tag in model.Tags)
                    item.Tags.Add(tag);

            item.InitializeTags(); // Hook up events and initial suggestions AFTER loading tags to avoid early sync triggers
        }

        item.IsEditing = true;
    }

    private void ConfirmEditModel(ModelWrapper? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.NewId)) return;

        decimal? inputPrice = null;
        decimal? outputPrice = null;

        if (item.InputPriceValue > 0) inputPrice = (decimal)item.InputPriceValue;

        if (item.OutputPriceValue > 0) outputPrice = (decimal)item.OutputPriceValue;

        // Update the model
        var model = Provider.Models.FirstOrDefault(m => m.Id == item.Id);
        if (model != null)
        {
            model.DisplayName = string.IsNullOrWhiteSpace(item.NewName) ? item.NewId : item.NewName;
            model.InputPricePerMTokens = inputPrice;
            model.OutputPricePerMTokens = outputPrice;
            model.Tags = new HashSet<string>(item.Tags);
            _providerService.UpdateProvider(Provider);
        }

        // Refresh local provider, but don't reload list to keep UI state
        var providers = _providerService.GetAllProviders();
        var current = providers.FirstOrDefault(p => p.Id == Provider.Id);
        if (current != null) Provider = current;

        item.IsEditing = false;
    }

    private void CancelEditModel(ModelWrapper? item)
    {
        if (item == null) return;
        item.IsEditing = false;
    }

    private void DeleteModel(ModelWrapper? item)
    {
        if (item == null) return;
        _providerService.RemoveModel(Provider.Id, item.Id);
        Models.Remove(item);

        // Refresh local provider to keep in sync
        var providers = _providerService.GetAllProviders();
        var current = providers.FirstOrDefault(p => p.Id == Provider.Id);
        if (current != null) Provider = current;
    }

    private void ToggleFavoriteModel(ModelWrapper? item)
    {
        if (item == null) return;
        _providerService.ToggleFavoriteModel(Provider.Id, item.Id);

        // Refresh local state without full reload
        item.IsFavorite = !item.IsFavorite;

        // Ensure the underlying model in memory is also updated 
        // otherwise StartEditModel might reload stale data
        var model = Provider.Models.FirstOrDefault(m => m.Id == item.Id);
        if (model != null)
        {
            if (item.IsFavorite) model.Tags.Add(ModelTags.Favorite);
            else model.Tags.Remove(ModelTags.Favorite);
        }
    }

    private void OnApiKeysCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncingOrder || _providerService == null) return;
        SyncApiKeysOrder();
    }

    private void SyncApiKeysOrder()
    {
        try
        {
            _isSyncingOrder = true;
            var newOrder = new List<ApiKey>();
            foreach (var wrapper in ApiKeys)
                if (wrapper.Id != Guid.Empty)
                {
                    var existing = Provider.ApiKeys.FirstOrDefault(k => k.Id == wrapper.Id);
                    if (existing != null) newOrder.Add(existing);
                }

            if (newOrder.Count == Provider.ApiKeys.Count)
            {
                Provider.ApiKeys = newOrder;
                _providerService.UpdateProvider(Provider);
                WeakReferenceMessenger.Default.Send(new ProviderUpdatedMessage(Provider.Id));
            }
        }
        finally
        {
            _isSyncingOrder = false;
        }
    }

    private void OnModelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncingOrder || _providerService == null) return;
        SyncModelsOrder();
    }

    private void SyncModelsOrder()
    {
        try
        {
            _isSyncingOrder = true;
            var newOrder = new List<ModelInfo>();
            foreach (var wrapper in Models)
                if (!string.IsNullOrEmpty(wrapper.Id))
                {
                    var existing = Provider.Models.FirstOrDefault(m => m.Id == wrapper.Id);
                    if (existing != null) newOrder.Add(existing);
                }

            if (newOrder.Count == Provider.Models.Count)
            {
                Provider.Models = newOrder;
                _providerService.UpdateProvider(Provider);
                WeakReferenceMessenger.Default.Send(new ProviderUpdatedMessage(Provider.Id));
            }
        }
        finally
        {
            _isSyncingOrder = false;
        }
    }
}
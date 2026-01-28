using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;
using Microsoft.UI.Dispatching;

namespace KeyPocket.UI.ViewModels;

public class DefaultIconItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public partial class ProviderSettingsViewModel : ObservableObject
{
    private readonly ProviderService _providerService;

    [ObservableProperty] private string? _baseUrl;

    [ObservableProperty] private string? _description;

    [ObservableProperty] private bool _hasCustomIcon;

    private bool _isSyncingOrder;

    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ProviderCurrencySymbol))]
    private string _providerCurrency = "USD";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TypeIndex))]
    private string _type = "OpenAI API";

    // Track the currency currently used to display model prices in this page

    public ProviderSettingsViewModel(Provider provider, ProviderService providerService)
    {
        Provider = provider;
        _providerService = providerService;

        // Initialize fields
        _name = provider.Name;

        // Ensure strictly matching string reference for ComboBox
        var typeMatch = ProviderTypes.FirstOrDefault(t => t == provider.Type);
        _type = typeMatch ?? provider.Type;

        _baseUrl = provider.ApiBaseUrl;
        _description = provider.Description;
        _providerCurrency = provider.Currency;
        LoadKeys(Provider);
        LoadModels(Provider);
        HasCustomIcon = !string.IsNullOrEmpty(provider.IconPath);

        LoadDefaultIcons();

        ApiKeys.CollectionChanged += OnApiKeysCollectionChanged;
        Models.CollectionChanged += OnModelsCollectionChanged;

        // Subscribe to global usage changes to update currency symbol if needed
        // (Logic simplified intentionally)
    }

    // Default for designer
    public ProviderSettingsViewModel()
    {
        _providerService = null!;
        Provider = new Provider();
    }

    public int TypeIndex
    {
        get => ProviderTypes.IndexOf(Type);
        set
        {
            if (value >= 0 && value < ProviderTypes.Count) Type = ProviderTypes[value];
        }
    }

    // Available currencies for selection
    public List<string> AvailableCurrencies => SettingsHelper.Current.AvailableCurrencies;

    // List is sufficient for static content and lighter than ObservableCollection
    public List<string> ProviderTypes { get; } = new()
    {
        "OpenAI API",
        "Claude API",
        "Google Gemini API",
        "Custom"
    };

    public ObservableCollection<KeyWrapper> ApiKeys { get; } = new();
    public ObservableCollection<ModelWrapper> Models { get; } = new();
    public ObservableCollection<DefaultIconItem> DefaultIcons { get; } = new();

    // Expose Provider for access
    public Provider Provider { get; private set; }

    // Current currency symbol (based on SettingsHelper.Current.SelectedCurrency)
    // NOTE: Ideally this VM should expose "ProviderCurrencySymbol" for the "add model" UI.

    public string ProviderCurrencySymbol => ExchangeRateHelper.GetCurrencySymbol(ProviderCurrency);

    partial void OnProviderCurrencyChanged(string value)
    {
        if (Models != null)
            foreach (var m in Models)
                m.InputCurrency = value;
    }

    [RelayCommand]
    public void Save()
    {
        if (_providerService == null) return;

        // Update local object
        Provider.Name = Name;
        Provider.Type = Type;
        Provider.ApiBaseUrl = BaseUrl;
        Provider.Description = Description;
        Provider.Currency = ProviderCurrency;

        // Persist
        _providerService.UpdateProvider(Provider);

        // Notify sidebar to update
        WeakReferenceMessenger.Default.Send(new ProviderUpdatedMessage(Provider.Id));
    }

    public async Task UpdateIconAsync(StorageFile? file)
    {
        if (_providerService == null) return;

        try
        {
            if (file == null)
            {
                // Remove logic
                Provider.IconPath = null;
                _providerService.UpdateProviderIcon(Provider.Id, null); // Pass null to clear
            }
            else
            {
                // Direct access - no copying
                var originalPath = file.Path;

                Provider.IconPath = originalPath;
                _providerService.UpdateProviderIcon(Provider.Id, originalPath);
            }

            // Update property
            HasCustomIcon = !string.IsNullOrEmpty(Provider.IconPath);

            // Notify sidebar to update icon
            WeakReferenceMessenger.Default.Send(new ProviderUpdatedMessage(Provider.Id));
        }
        catch (Exception)
        {
            // Logging?
        }
    }

    public void DeleteProvider()
    {
        if (_providerService == null) return;

        var providerId = Provider.Id;
        _providerService.RemoveProvider(providerId);

        // Send message to update sidebar and home
        WeakReferenceMessenger.Default.Send(new ProviderDeletedMessage(providerId));
    }

    // --- API Keys ---

    private void LoadKeys()
    {
        if (_providerService == null) return;
        ApiKeys.Clear();

        // Refresh provider data
        var providers = _providerService.GetAllProviders();
        var current = providers.FirstOrDefault(p => p.Id == Provider.Id);
        if (current == null) return;
        Provider = current;
    }

    private Provider? GetProviderFromService(Guid id)
    {
        return _providerService.GetAllProviders().FirstOrDefault(p => p.Id == id);
    }

    private void LoadKeys(Provider? current = null)
    {
        if (current == null) current = GetProviderFromService(Provider.Id);
        if (current == null) return;
        Provider = current;

        ApiKeys.Clear();

        _isSyncingOrder = true;
        try
        {
            foreach (var k in Provider.ApiKeys)
            {
                // Decrypt the key to get the original plain text for proper masking
                var plainKey = string.Empty;
                try
                {
                    plainKey = _providerService.GetDecryptedApiKey(Provider.Id, k.Id);
                }
                catch
                {
                    plainKey = "[Error]";
                }

                // Generate masked key: first 7 + dots + last 4
                var maskedKey = string.Empty;
                if (plainKey.Length >= 11)
                {
                    var dotsCount = Math.Min(20, plainKey.Length - 11); // Max 20 dots
                    var dots = new string('·', dotsCount);
                    maskedKey = plainKey.Substring(0, 7) + dots + plainKey.Substring(plainKey.Length - 4);
                }
                else if (plainKey.Length > 0)
                {
                    maskedKey = plainKey.Substring(0, Math.Min(7, plainKey.Length)) + "······";
                }

                var w = new KeyWrapper
                {
                    Id = k.Id,
                    Tag = k.Tag,
                    MaskedKey = maskedKey,
                    IsFavorite = k.IsFavorite,
                    IsEditing = false
                };
                InjectKeyCommands(w);
                ApiKeys.Add(w);
            }
        }
        finally
        {
            _isSyncingOrder = false;
        }
    }

    [RelayCommand]
    public void AddKey()
    {
        // Add a placeholder for editing
        var w = new KeyWrapper
        {
            Id = Guid.Empty, // New
            IsEditing = true,
            NewKey = ""
        };
        InjectKeyCommands(w);
        ApiKeys.Add(w);
    }

    private void InjectKeyCommands(KeyWrapper w)
    {
        // Use closures to capture 'w' so explicit CommandParameter is not needed in XAML for simple buttons
        w.ToggleFavoriteCommand = new RelayCommand(() => ToggleFavoriteKey(w));
        w.DeleteCommand = new RelayCommand(() => DeleteKey(w));
        w.ConfirmAddCommand = new RelayCommand(() => ConfirmAddKey(w), () => !string.IsNullOrWhiteSpace(w.NewKey));
        w.CancelAddCommand = new RelayCommand(() => CancelAddKey(w));
        w.CopyKeyCommand = new RelayCommand(() => CopyKey(w));

        w.StartEditTagCommand = new RelayCommand(() => { w.IsTagEditing = true; });

        w.CommitTagEditCommand = new RelayCommand(() =>
        {
            w.IsTagEditing = false;
            SaveKeyTag(w);
        });
    }

    private void ConfirmAddKey(KeyWrapper? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.NewKey)) return;

        // Keep track of existing IDs to find the new one later
        var existingIds = Provider.ApiKeys.Select(k => k.Id).ToHashSet();

        // Add to service
        _providerService.AddApiKey(Provider.Id, item.NewKey);

        // Refresh provider from source to get the new key ID and state
        var updatedProvider = GetProviderFromService(Provider.Id);
        if (updatedProvider == null) return;

        Provider = updatedProvider;

        // Find the new key
        var newKeyModel = updatedProvider.ApiKeys.FirstOrDefault(k => !existingIds.Contains(k.Id));

        if (newKeyModel != null)
        {
            // Calculate masked key locally for display
            string maskedKey;
            if (item.NewKey.Length >= 11)
            {
                var dotsCount = Math.Min(20, item.NewKey.Length - 11);
                var dots = new string('·', dotsCount);
                maskedKey = item.NewKey.Substring(0, 7) + dots + item.NewKey.Substring(item.NewKey.Length - 4);
            }
            else
            {
                maskedKey = item.NewKey.Substring(0, Math.Min(7, item.NewKey.Length)) + "······";
            }

            // Update the wrapper in-place
            item.Id = newKeyModel.Id;
            item.MaskedKey = maskedKey;
            item.NewKey = string.Empty; // Clear plain text
            item.IsEditing = false;
            // Commands are already injected
        }
        else
        {
            // Fallback: reload all if something went wrong finding the match
            LoadKeys();
        }
    }

    private void CancelAddKey(KeyWrapper? item)
    {
        if (item == null) return;
        if (item.Id == Guid.Empty)
            ApiKeys.Remove(item);
        else
            item.IsEditing = false;
    }

    private void SaveKeyTag(KeyWrapper? item)
    {
        if (item == null || item.Id == Guid.Empty) return;
        _providerService.UpdateApiKeyTag(Provider.Id, item.Id, item.Tag);
        // No reload needed for tag update usually, but to be safe
    }

    private void DeleteKey(KeyWrapper? item)
    {
        if (_providerService == null || item == null) return;
        if (item.Id == Guid.Empty)
        {
            ApiKeys.Remove(item);
            return;
        }

        _providerService.RemoveApiKey(Provider.Id, item.Id);
        ApiKeys.Remove(item);

        // Update local object
        var key = Provider.ApiKeys.FirstOrDefault(k => k.Id == item.Id);
        if (key != null) Provider.ApiKeys.Remove(key);
    }

    private void ToggleFavoriteKey(KeyWrapper? item)
    {
        if (_providerService == null || item == null || item.Id == Guid.Empty) return;
        _providerService.ToggleFavoriteApiKey(Provider.Id, item.Id);
        item.IsFavorite = !item.IsFavorite;

        // Update local object
        var key = Provider.ApiKeys.FirstOrDefault(k => k.Id == item.Id);
        if (key != null) key.IsFavorite = item.IsFavorite;
    }

    private void CopyKey(KeyWrapper? item)
    {
        if (_providerService == null || item == null || item.Id == Guid.Empty) return;

        try
        {
            var plainKey = _providerService.GetDecryptedApiKey(Provider.Id, item.Id);
            var dataPackage = new DataPackage();
            dataPackage.SetText(plainKey);
            Clipboard.SetContent(dataPackage);
        }
        catch
        {
            // Silently fail or show notification
        }
    }

    public string GetDecryptedKey(Guid keyId)
    {
        if (_providerService == null) return string.Empty;
        try
        {
            return _providerService.GetDecryptedApiKey(Provider.Id, keyId);
        }
        catch
        {
            return string.Empty;
        }
    }

    // --- Models ---

    private void LoadModels(Provider? current = null)
    {
        if (current == null) current = GetProviderFromService(Provider.Id);
        if (current == null) return;
        Provider = current;

        Models.Clear();

        _isSyncingOrder = true;
        try
        {
            foreach (var m in Provider.Models)
            {
                var w = new ModelWrapper
                {
                    Id = m.Id,
                    Name = m.DisplayName,
                    IsFavorite = m.IsFavorite,
                    IsEditing = false
                };

                // Initialize editing currency to Provider's currency
                w.InputCurrency = Provider.Currency ?? "USD";

                // Load stored values (which are in Provider.Currency)
                w.InputPriceValue = (double)(m.InputPricePerMTokens ?? 0);
                w.OutputPriceValue = (double)(m.OutputPricePerMTokens ?? 0);


                w.InputPrice = w.InputPriceValue.ToString();
                w.OutputPrice = w.OutputPriceValue.ToString();

                InjectModelCommands(w);
                Models.Add(w);
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
        InjectModelCommands(w);
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
                OutputPricePerMTokens = outputPrice
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
            item.InputPrice = item.InputPriceValue.ToString();
            item.OutputPrice = item.OutputPriceValue.ToString();

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

        // No global LoadModels() call here to preserve other new cards
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
        // Or full reload if we trust service source of truth
    }

    private void OnApiKeysCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncingOrder || _providerService == null) return;
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

    private async void LoadDefaultIcons()
    {
        // Capture dispatcher from UI thread
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Capture theme on UI thread
        var isDark = ThemeHelper.IsDarkTheme();

        await Task.Run(() =>
        {
            var iconNames = ProviderIconHelper.GetAllPresetIconNames();

            dispatcherQueue?.TryEnqueue(() =>
            {
                DefaultIcons.Clear();
                foreach (var iconName in iconNames)
                {
                    var displayName = iconName.Length > 0
                        ? char.ToUpper(iconName[0]) + iconName.Substring(1)
                        : iconName;

                    DefaultIcons.Add(new DefaultIconItem
                    {
                        Name = displayName,
                        Path = ProviderIconHelper.GetPresetIconUri(iconName, isDark).ToString(),
                        FileName = $"{iconName}.png"
                    });
                }
            });
        });
    }

    [RelayCommand]
    public async Task SelectDefaultIcon(DefaultIconItem? item)
    {
        if (item == null || _providerService == null) return;

        try
        {
            // When selecting a preset, we just save the name!
            // The main window will resolve it to Assets/ProviderIcons/{Name}-{Theme}.png

            // Set new path as the Name (e.g. "Openai")
            // We use the normalized Name from the item
            var newPath = item.Name;

            Provider.IconPath = newPath;
            _providerService.UpdateProviderIcon(Provider.Id, newPath);

            // Update property
            HasCustomIcon = !string.IsNullOrEmpty(Provider.IconPath);

            // Notify sidebar
            WeakReferenceMessenger.Default.Send(new ProviderUpdatedMessage(Provider.Id));
        }
        catch (Exception)
        {
            // Logging?
        }
    }

    private string FormatDefaultModelName(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return id;
        // Convert to Title Case (e.g. "gpt-4" -> "Gpt-4", "deepseek" -> "Deepseek")
        // ToLower() first to ensure ToTitleCase processes it correctly even if input is ALLCAPS or mixed.
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id.ToLower());
    }
}

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

    public string FavoriteIcon => IsFavorite ? "\uE735" : "\uE734";

    partial void OnNewKeyChanged(string value)
    {
        (ConfirmAddCommand as IRelayCommand)?.NotifyCanExecuteChanged();
    }
}

public partial class ModelWrapper : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProviderSymbol))]
    [NotifyPropertyChangedFor(nameof(InputPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(OutputPriceDisplay))]
    private string _inputCurrency = "USD";

    // Display strings
    [ObservableProperty] private string _inputPrice = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InputPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(HasInputPrice))]
    private double _inputPriceValue;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsReadOnly))]
    private bool _isEditing;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(FavoriteIcon))]
    private bool _isFavorite;

    [ObservableProperty] private string _name = string.Empty;

    // For new model entry
    [ObservableProperty] private string _newId = "";

    [ObservableProperty] private string _newName = "";

    [ObservableProperty] private string _outputPrice = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(HasOutputPrice))]
    private double _outputPriceValue;

    public string Id { get; set; } = string.Empty;

    public bool IsReadOnly => !IsEditing;

    // Provide currency symbol for each wrapper local editing context
    public string ProviderSymbol => ExchangeRateHelper.GetCurrencySymbol(InputCurrency);

    // Formatted display properties for UI read-only view
    // Using N3 to ensure decimal alignment visually if font is monospaced or widths match
    public string InputPriceDisplay => InputPriceValue > 0 ? $"{ProviderSymbol}{InputPriceValue:N3}" : "";
    public string OutputPriceDisplay => OutputPriceValue > 0 ? $"{ProviderSymbol}{OutputPriceValue:N3}" : "";
    public bool HasInputPrice => InputPriceValue > 0;
    public bool HasOutputPrice => OutputPriceValue > 0;

    // Provide currency symbol for each wrapper (updates via RefreshCurrencySymbol)
    public string CurrencySymbol => SettingsHelper.Current.SelectedCurrency == "CNY" ? "¥" : "$";

    public string FavoriteIcon => IsFavorite ? "\uE735" : "\uE734";

    public ICommand? ToggleFavoriteCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }

    // For inline editing
    public ICommand? ConfirmAddCommand { get; set; }
    public ICommand? CancelAddCommand { get; set; }
    public ICommand? StartEditCommand { get; set; }
    public ICommand? ConfirmEditCommand { get; set; }
    public ICommand? CancelEditCommand { get; set; }

    partial void OnNewIdChanged(string value)
    {
        (ConfirmAddCommand as IRelayCommand)?.NotifyCanExecuteChanged();
    }

    partial void OnInputPriceValueChanged(double value)
    {
        if (value < 0) InputPriceValue = 0;
    }

    partial void OnOutputPriceValueChanged(double value)
    {
        if (value < 0) OutputPriceValue = 0;
    }

    // Called by parent ViewModel when global settings change
    public void RefreshCurrencySymbol()
    {
        OnPropertyChanged(nameof(CurrencySymbol));
    }
}
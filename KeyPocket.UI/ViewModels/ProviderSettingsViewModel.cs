#pragma warning disable MVVMTK0045
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.UI.Messages;
using KeyPocket.UI.Pages;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace KeyPocket.UI.ViewModels;

public partial class ProviderSettingsViewModel : ObservableObject
{
    private readonly ProviderService _providerService;
    private Provider _originalProvider;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _type = "OpenAI API";

    [ObservableProperty]
    private string? _baseUrl;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _hasCustomIcon;

    public ObservableCollection<string> ProviderTypes { get; } = new()
    {
        "OpenAI API",
        "Claude API",
        "Google Gemini API",
        "Custom"
    };

    public ObservableCollection<KeyWrapper> ApiKeys { get; } = new();
    public ObservableCollection<ModelWrapper> Models { get; } = new();

    public ProviderSettingsViewModel(Provider provider, ProviderService providerService)
    {
        _originalProvider = provider;
        _providerService = providerService;

        // Init fields
        _name = provider.Name;
        _type = provider.Type;
        _baseUrl = provider.ApiBaseUrl;
        _description = provider.Description;
        LoadKeys(_originalProvider);
        LoadModels(_originalProvider);
        HasCustomIcon = !string.IsNullOrEmpty(provider.IconPath);
    }

    // 公开 Provider 以便访问
    public Provider Provider => _originalProvider;

    // Default for designer
    public ProviderSettingsViewModel() 
    {
        _providerService = null!;
        _originalProvider = new Provider();
    }

    [RelayCommand]
    public void Save()
    {
        if (_providerService == null) return;

        // Update local object
        _originalProvider.Name = Name;
        _originalProvider.Type = Type;
        _originalProvider.ApiBaseUrl = BaseUrl;
        _originalProvider.Description = Description;

        // Persist
        _providerService.UpdateProvider(_originalProvider);
        
        // Notify sidebar to update
        WeakReferenceMessenger.Default.Send(new ProviderUpdatedMessage(_originalProvider.Id));
    }

    public async System.Threading.Tasks.Task UpdateIconAsync(Windows.Storage.StorageFile? file)
    {
        if (_providerService == null) return;
        
        try 
        {
            if (file == null)
            {
                // Remove logic
                _originalProvider.IconPath = null;
                _providerService.UpdateProviderIcon(_originalProvider.Id, null); // Pass null to clear
            }
            else
            {
                var iconsFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("Icons", Windows.Storage.CreationCollisionOption.OpenIfExists);
                // Use Provider ID as filename to keep it simpler and unique per provider
                string ext = file.FileType;
                string fileName = $"{_originalProvider.Id}{ext}";
                
                var targetFile = await file.CopyAsync(iconsFolder, fileName, Windows.Storage.NameCollisionOption.ReplaceExisting);
                string relativePath = System.IO.Path.Combine("Icons", fileName);
                
                _originalProvider.IconPath = relativePath;
                _providerService.UpdateProviderIcon(_originalProvider.Id, relativePath);
            }
            
            // Update property
            HasCustomIcon = !string.IsNullOrEmpty(_originalProvider.IconPath);
            
            // Notify sidebar to update icon
            WeakReferenceMessenger.Default.Send(new ProviderUpdatedMessage(_originalProvider.Id)); 
        }
        catch (Exception)
        {
            // Logging?
        }
    }

    public void DeleteProvider()
    {
        if (_providerService == null) return;
        
        var providerId = _originalProvider.Id;
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
        var current = providers.FirstOrDefault(p => p.Id == _originalProvider.Id);
        if (current == null) return;
        _originalProvider = current;
    }

    private Provider? GetProviderFromService(Guid id)
    {
        return _providerService.GetAllProviders().FirstOrDefault(p => p.Id == id);
    }

    private void LoadKeys(Provider? current = null)
    {
        if (current == null) current = GetProviderFromService(_originalProvider.Id);
        if (current == null) return;
        _originalProvider = current;

        ApiKeys.Clear();

        foreach (var k in _originalProvider.ApiKeys)
        {
            // Decrypt the key to get the original plain text for proper masking
            string plainKey = string.Empty;
            try
            {
                plainKey = _providerService.GetDecryptedApiKey(_originalProvider.Id, k.Id);
            }
            catch
            {
                plainKey = "[Error]";
            }

            // Generate masked key: first 7 + dots + last 4
            string maskedKey = string.Empty;
            if (plainKey.Length >= 11)
            {
                int dotsCount = Math.Min(20, plainKey.Length - 11); // Max 20 dots
                string dots = new string('·', dotsCount);
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
        w.ConfirmAddCommand = new RelayCommand(() => ConfirmAddKey(w));
        w.CancelAddCommand = new RelayCommand(() => CancelAddKey(w));
        w.CopyKeyCommand = new RelayCommand(() => CopyKey(w));
        
        w.StartEditTagCommand = new RelayCommand(() => 
        {
            w.IsTagEditing = true;
        });

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
        var existingIds = _originalProvider.ApiKeys.Select(k => k.Id).ToHashSet();
        
        // Add to service
        _providerService.AddApiKey(_originalProvider.Id, item.NewKey);
        
        // Refresh provider from source to get the new key ID and state
        var updatedProvider = GetProviderFromService(_originalProvider.Id);
        if (updatedProvider == null) return;
        
        _originalProvider = updatedProvider;
        
        // Find the new key
        var newKeyModel = updatedProvider.ApiKeys.FirstOrDefault(k => !existingIds.Contains(k.Id));
        
        if (newKeyModel != null)
        {
            // Calculate masked key locally for display
            string maskedKey;
            if (item.NewKey.Length >= 11)
            {
                int dotsCount = Math.Min(20, item.NewKey.Length - 11);
                string dots = new string('·', dotsCount);
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
        {
            ApiKeys.Remove(item);
        }
        else
        {
            item.IsEditing = false;
        }
    }

    private void SaveKeyTag(KeyWrapper? item)
    {
        if (item == null || item.Id == Guid.Empty) return;
        _providerService.UpdateApiKeyTag(_originalProvider.Id, item.Id, item.Tag);
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

        _providerService.RemoveApiKey(_originalProvider.Id, item.Id);
        ApiKeys.Remove(item);
        
        // Update local object
        var key = _originalProvider.ApiKeys.FirstOrDefault(k => k.Id == item.Id);
        if (key != null) _originalProvider.ApiKeys.Remove(key);
    }

    private void ToggleFavoriteKey(KeyWrapper? item)
    {
        if (_providerService == null || item == null || item.Id == Guid.Empty) return;
        _providerService.ToggleFavoriteApiKey(_originalProvider.Id, item.Id);
        item.IsFavorite = !item.IsFavorite;
        
        // Update local object
        var key = _originalProvider.ApiKeys.FirstOrDefault(k => k.Id == item.Id);
        if (key != null) key.IsFavorite = item.IsFavorite;
    }

    private void CopyKey(KeyWrapper? item)
    {
        if (_providerService == null || item == null || item.Id == Guid.Empty) return;
        
        try
        {
            var plainKey = _providerService.GetDecryptedApiKey(_originalProvider.Id, item.Id);
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(plainKey);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
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
            return _providerService.GetDecryptedApiKey(_originalProvider.Id, keyId);
        }
        catch
        {
            return string.Empty;
        }
    }

    // --- Models ---
    
    private void LoadModels(Provider? current = null)
    {
        if (current == null) current = GetProviderFromService(_originalProvider.Id);
        if (current == null) return;
        _originalProvider = current;

        Models.Clear();
        
        foreach (var m in _originalProvider.Models)
        {
             var w = new ModelWrapper
             {
                 Id = m.Id,
                 Name = m.DisplayName,
                 IsFavorite = m.IsFavorite,
                 IsEditing = false
             };
             InjectModelCommands(w);
             Models.Add(w);
        }
    }

    private void InjectModelCommands(ModelWrapper w)
    {
        w.ToggleFavoriteCommand = new RelayCommand(() => ToggleFavoriteModel(w));
        w.DeleteCommand = new RelayCommand(() => DeleteModel(w));
        w.ConfirmAddCommand = new RelayCommand(() => ConfirmAddModel(w));
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
            OutputPriceValue = 0
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
            inputPrice = (decimal)item.InputPriceValue;
        if (item.OutputPriceValue > 0)
            outputPrice = (decimal)item.OutputPriceValue;

        // Check if this is editing an existing model or adding a new one
        if (string.IsNullOrEmpty(item.Id)) // New model
        {
            var model = new ModelInfo
            {
                Id = item.NewId,
                DisplayName = string.IsNullOrWhiteSpace(item.NewName) ? item.NewId : item.NewName,
                ProviderId = _originalProvider.Id,
                InputPricePerMTokens = inputPrice,
                OutputPricePerMTokens = outputPrice
            };
            
            _providerService.AddModel(_originalProvider.Id, model);

            // Refresh provider to get clean state but don't reload list
            var current = GetProviderFromService(_originalProvider.Id);
            if (current != null) _originalProvider = current;

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
            var model = _originalProvider.Models.FirstOrDefault(m => m.Id == item.Id);
            if (model != null)
            {
                // Update model properties
                model.Id = item.NewId;
                model.DisplayName = string.IsNullOrWhiteSpace(item.NewName) ? item.NewId : item.NewName;
                model.InputPricePerMTokens = inputPrice;
                model.OutputPricePerMTokens = outputPrice;
                
                // Save changes
                _providerService.UpdateProvider(_originalProvider);
                
                // Refresh provider locally
                var current = GetProviderFromService(_originalProvider.Id);
                if (current != null) _originalProvider = current;
                
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
        {
            Models.Remove(item);
        }
        else
        {
            item.IsEditing = false;
        }
    }

    private void StartEditModel(ModelWrapper? item)
    {
        if (item == null || string.IsNullOrEmpty(item.Id)) return;
        
        // Populate edit fields with current values
        item.NewId = item.Id;
        item.NewName = item.Name;
        
        // Get current model to populate prices
        var model = _originalProvider.Models.FirstOrDefault(m => m.Id == item.Id);
        if (model != null)
        {
            item.InputPriceValue = (double)(model.InputPricePerMTokens ?? 0);
            item.OutputPriceValue = (double)(model.OutputPricePerMTokens ?? 0);
        }
        
        item.IsEditing = true;
    }

    private void ConfirmEditModel(ModelWrapper? item)
    {
        if (item == null || string.IsNullOrEmpty(item.NewId)) return;

        decimal? inputPrice = null;
        decimal? outputPrice = null;

        if (item.InputPriceValue > 0)
            inputPrice = (decimal)item.InputPriceValue;
        if (item.OutputPriceValue > 0)
            outputPrice = (decimal)item.OutputPriceValue;

        // Update the model
        var model = _originalProvider.Models.FirstOrDefault(m => m.Id == item.Id);
        if (model != null)
        {
            model.DisplayName = string.IsNullOrWhiteSpace(item.NewName) ? item.NewId : item.NewName;
            model.InputPricePerMTokens = inputPrice;
            model.OutputPricePerMTokens = outputPrice;
            _providerService.UpdateProvider(_originalProvider);
        }

        // Refresh local provider, but don't reload list to keep UI state
        var providers = _providerService.GetAllProviders();
        var current = providers.FirstOrDefault(p => p.Id == _originalProvider.Id);
        if (current != null) _originalProvider = current;
        
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
        _providerService.RemoveModel(_originalProvider.Id, item.Id);
        Models.Remove(item);
        
        // Refresh local provider to keep in sync
        var providers = _providerService.GetAllProviders();
        var current = providers.FirstOrDefault(p => p.Id == _originalProvider.Id);
        if (current != null) _originalProvider = current;
    }

    private void ToggleFavoriteModel(ModelWrapper? item)
    {
        if (item == null) return;
        _providerService.ToggleFavoriteModel(_originalProvider.Id, item.Id);
        
        // Refresh local state without full reload
        item.IsFavorite = !item.IsFavorite;
        // Or full reload if we trust service source of truth
    }

}

public partial class KeyWrapper : ObservableObject
{
    public Guid Id { get; set; }
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTag))]
    [NotifyPropertyChangedFor(nameof(IsTagTextVisible))]
    [NotifyPropertyChangedFor(nameof(IsTagDisplayVisible))]
    private string? _tag;

    public bool HasTag => !string.IsNullOrWhiteSpace(Tag);
    
    [ObservableProperty]
    private string _maskedKey = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteIcon))]
    private bool _isFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReadOnly))]
    private bool _isEditing;

    public bool IsReadOnly => !IsEditing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTagDisplayVisible))]
    [NotifyPropertyChangedFor(nameof(IsTagTextVisible))]
    private bool _isTagEditing;

    // Show icon button when: not editing AND no tag
    public bool IsTagDisplayVisible => !IsTagEditing && !HasTag;
    
    // Show tag text button when: not editing AND has tag
    public bool IsTagTextVisible => !IsTagEditing && HasTag;

    [ObservableProperty]
    private string _newKey = "";

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
}

public partial class ModelWrapper : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteIcon))]
    private bool _isFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReadOnly))]
    private bool _isEditing;

    public bool IsReadOnly => !IsEditing;

    // For new model entry
    [ObservableProperty]
    private string _newId = "";

    [ObservableProperty]
    private string _newName = "";

    [ObservableProperty]
    private double _inputPriceValue;

    [ObservableProperty]
    private double _outputPriceValue;

    // Display strings
    [ObservableProperty]
    private string _inputPrice = "0";

    [ObservableProperty]
    private string _outputPrice = "0";

    public string FavoriteIcon => IsFavorite ? "\uE735" : "\uE734";

    public ICommand? ToggleFavoriteCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
    
    // For inline editing
    public ICommand? ConfirmAddCommand { get; set; }
    public ICommand? CancelAddCommand { get; set; }
    public ICommand? StartEditCommand { get; set; }
    public ICommand? ConfirmEditCommand { get; set; }
    public ICommand? CancelEditCommand { get; set; }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;

#pragma warning disable MVVMTK0045

namespace KeyPocket.UI.ViewModels;

public partial class ProviderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    public Guid Id { get; private set; }

    [ObservableProperty]
    private string _icon = "\uE774"; // Globe

    [ObservableProperty]
    private string? _description;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    [ObservableProperty]
    private Brush _statusColor = new SolidColorBrush(Colors.Gray);

    public ObservableCollection<ModelItem> Models { get; set; } = new();
    public ObservableCollection<KeyItem> Keys { get; set; } = new();
    
    // Core Model Reference (Optional, but good for linking back)
    public Provider? CoreModel { get; private set; }

    public ProviderViewModel() { }

    public ProviderViewModel(Provider provider, ProviderService? providerService = null)
    {
        CoreModel = provider;
        Id = provider.Id;
        Name = provider.Name;
        Description = provider.Description;
        Name = provider.Name;
        Description = provider.Description;
        UpdateIcon(provider.Type, provider.IconPath);
        
        // Populate favorite models
        var favoriteModels = provider.Models.Where(m => m.IsFavorite).ToList();
        foreach (var model in favoriteModels)
        {
            Models.Add(new ModelItem 
            { 
                Id = model.Id, 
                Name = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Id : model.DisplayName 
            });
        }
        
        // Populate favorite API keys
        var favoriteKeys = provider.ApiKeys.Where(k => k.IsFavorite).ToList();
        foreach (var key in favoriteKeys)
        {
            // Decrypt the key for display
            string decryptedKey = string.Empty;
            if (providerService != null)
            {
                decryptedKey = providerService.GetDecryptedApiKey(provider.Id, key.Id);
            }
            
            if (!string.IsNullOrEmpty(decryptedKey))
            {
                var keyStart = decryptedKey.Length >= 7 ? decryptedKey.Substring(0, 7) : decryptedKey;
                var keyEnd = decryptedKey.Length >= 4 ? decryptedKey.Substring(decryptedKey.Length - 4) : "";
                
                Keys.Add(new KeyItem 
                { 
                    KeyStart = keyStart,
                    KeyEnd = keyEnd,
                    FullKey = decryptedKey
                });
            }
        }
    }

    [ObservableProperty]
    private Microsoft.UI.Xaml.Media.ImageSource? _customIconSource;

    [ObservableProperty]
    private bool _isCustomIcon;

    public void UpdateIcon(string type, string? iconPath = null)
    {
        // 1. Try Custom Icon
        if (!string.IsNullOrEmpty(iconPath))
        {
            try
            {
                var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var fullPath = System.IO.Path.Combine(localFolder.Path, iconPath);
                
                if (System.IO.File.Exists(fullPath))
                {
                    CustomIconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(fullPath));
                    IsCustomIcon = true;
                    return;
                }
            }
            catch { /* Ignore load errors, fallback to glyph */ }
        }

        // 2. Fallback to Glyph
        IsCustomIcon = false;
        CustomIconSource = null;
        Icon = "\uE99A"; // Default icon for all types
    }
}

// Keep these simple wrapper/items for now, or extract fully later
public class ModelItem { public string Name { get; set; } = ""; public string Id { get; set; } = ""; }
public class KeyItem 
{ 
    public string KeyStart { get; set; } = "";
    public string KeyEnd { get; set; } = "";
    public string FullKey { get; set; } = "";
}

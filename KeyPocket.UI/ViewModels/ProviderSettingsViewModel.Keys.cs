using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;
using Microsoft.UI.Dispatching;
using WinRT.Interop;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace KeyPocket.UI.ViewModels;

public partial class ProviderSettingsViewModel : ObservableObject
{
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

        w.StartEditTagCommand = new RelayCommand(() =>
        {
            w.IsTagEditing = true;
            w.SetOriginalTag();
        });

        w.CommitTagEditCommand = new RelayCommand(() =>
        {
            w.IsTagEditing = false;
            SaveKeyTag(w);
        });

        w.CancelTagEditCommand = new RelayCommand(() =>
        {
            w.RestoreOriginalTag();
            w.IsTagEditing = false;
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

}

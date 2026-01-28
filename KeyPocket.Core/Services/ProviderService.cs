using Windows.Storage;
using KeyPocket.Core.Crypto;
using KeyPocket.Core.Models;
using KeyPocket.Core.Storage;

namespace KeyPocket.Core.Services;

/// <summary>
///     Manages Provider business logic.
/// </summary>
public class ProviderService
{
    private readonly ISecretProtector _secretProtector;

    private readonly IStorageProvider _storage;

    public ProviderService(IStorageProvider storage, ISecretProtector secretProtector)
    {
        _storage = storage;
        _secretProtector = secretProtector;
    }

    public List<Provider> GetAllProviders()
    {
        return _storage.Load().Providers.OrderBy(p => p.SortOrder).ToList();
    }

    /// <summary>
    ///     Generate a default provider name (e.g. "New Provider 1")
    /// </summary>
    private string GenerateDefaultProviderName()
    {
        var config = _storage.Load();
        var counter = 1;
        var baseName = "New Provider";
        var name = $"{baseName} {counter}";

        // Find unused name
        while (config.Providers.Any(p => p.Name == name))
        {
            counter++;
            name = $"{baseName} {counter}";
        }

        return name;
    }

    /// <summary>
    ///     Create a provider with default configuration
    /// </summary>
    public Provider CreateProvider()
    {
        return CreateProvider(
            GenerateDefaultProviderName(),
            "OpenAI API"
        );
    }

    public Provider CreateProvider(string name, string type, string? baseUrl = null, string? description = null)
    {
        var config = _storage.Load();

        // 为新服务商分配排序位置（当前最大值 + 1）
        var maxSortOrder = config.Providers.Any() ? config.Providers.Max(p => p.SortOrder) : -1;

        var newProvider = new Provider
        {
            Name = name,
            Type = type,
            ApiBaseUrl = baseUrl,
            Description = description,
            SortOrder = maxSortOrder + 1
        };
        config.Providers.Add(newProvider);
        _storage.Save(config);
        return newProvider;
    }

    public void RemoveProvider(Guid id)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == id);
        if (provider != null)
        {
            // Delete icon file if exists
            if (!string.IsNullOrEmpty(provider.IconPath))
                try
                {
                    var iconFile = Path.Combine(
                        ApplicationData.Current.LocalFolder.Path,
                        provider.IconPath
                    );
                    if (File.Exists(iconFile)) File.Delete(iconFile);
                }
                catch
                {
                    // Ignore errors during icon deletion
                }

            config.Providers.Remove(provider);
            _storage.Save(config);
        }
    }

    public void RenameProvider(Guid id, string newName)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == id);
        if (provider != null)
        {
            provider.Name = newName;
            _storage.Save(config);
        }
    }

    public void UpdateProvider(Provider updatedProvider)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == updatedProvider.Id);
        if (provider != null)
        {
            provider.Name = updatedProvider.Name;
            provider.Type = updatedProvider.Type;
            provider.ApiBaseUrl = updatedProvider.ApiBaseUrl;
            provider.Description = updatedProvider.Description;
            provider.Currency = updatedProvider.Currency;

            // Update Models and ApiKeys lists
            provider.Models = updatedProvider.Models;
            provider.ApiKeys = updatedProvider.ApiKeys;
            provider.IconPath = updatedProvider.IconPath;

            _storage.Save(config);
        }
    }

    public void UpdateProviderIcon(Guid providerId, string? iconPath)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        if (provider != null)
        {
            provider.IconPath = iconPath;
            _storage.Save(config);
        }
    }

    public void AddApiKey(Guid providerId, string key)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        if (provider != null)
        {
            var encrypted = _secretProtector.Protect(key);
            provider.ApiKeys.Add(new ApiKey
            {
                ProviderId = providerId,
                EncryptedKey = encrypted,
                CreatedAt = DateTime.Now
            });
            _storage.Save(config);
        }
    }

    public void UpdateApiKeyTag(Guid providerId, Guid keyId, string? tag)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        var key = provider?.ApiKeys.FirstOrDefault(k => k.Id == keyId);

        if (key != null)
        {
            key.Tag = tag;
            _storage.Save(config);
        }
    }

    public void RemoveApiKey(Guid providerId, Guid keyId)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        if (provider != null)
        {
            var key = provider.ApiKeys.FirstOrDefault(k => k.Id == keyId);
            if (key != null)
            {
                provider.ApiKeys.Remove(key);
                provider.FavoriteApiKeyIds.Remove(keyId);
                _storage.Save(config);
            }
        }
    }

    public void ToggleFavoriteApiKey(Guid providerId, Guid keyId)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        if (provider != null)
        {
            var key = provider.ApiKeys.FirstOrDefault(k => k.Id == keyId);
            if (key != null)
            {
                key.IsFavorite = !key.IsFavorite;

                // Sync with ID list for backward compatibility if needed, or just rely on property
                if (key.IsFavorite && !provider.FavoriteApiKeyIds.Contains(keyId))
                    provider.FavoriteApiKeyIds.Add(keyId);
                else if (!key.IsFavorite && provider.FavoriteApiKeyIds.Contains(keyId))
                    provider.FavoriteApiKeyIds.Remove(keyId);

                _storage.Save(config);
            }
        }
    }

    public void ToggleDisableApiKey(Guid providerId, Guid keyId)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        if (provider != null)
        {
            var key = provider.ApiKeys.FirstOrDefault(k => k.Id == keyId);
            if (key != null)
            {
                key.IsDisabled = !key.IsDisabled;
                _storage.Save(config);
            }
        }
    }

    // --- Model Management ---

    public void AddModel(Guid providerId, ModelInfo model)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        if (provider != null)
        {
            // Avoid duplicates
            if (provider.Models.Any(m => m.Id == model.Id))
                return;

            model.ProviderId = providerId;
            provider.Models.Add(model);
            _storage.Save(config);
        }
    }

    public void RemoveModel(Guid providerId, string modelId)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        if (provider != null)
        {
            var model = provider.Models.FirstOrDefault(m => m.Id == modelId);
            if (model != null)
            {
                provider.Models.Remove(model);
                provider.FavoriteModelIds.Remove(modelId);
                _storage.Save(config);
            }
        }
    }

    public void ToggleFavoriteModel(Guid providerId, string modelId)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        if (provider != null)
        {
            var model = provider.Models.FirstOrDefault(m => m.Id == modelId);
            if (model != null)
            {
                model.IsFavorite = !model.IsFavorite;

                // Sync list
                if (model.IsFavorite && !provider.FavoriteModelIds.Contains(modelId))
                    provider.FavoriteModelIds.Add(modelId);
                else if (!model.IsFavorite && provider.FavoriteModelIds.Contains(modelId))
                    provider.FavoriteModelIds.Remove(modelId);

                _storage.Save(config);
            }
        }
    }

    public string GetDecryptedApiKey(Guid providerId, Guid keyId)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        var key = provider?.ApiKeys.FirstOrDefault(k => k.Id == keyId);

        if (key == null || string.IsNullOrEmpty(key.EncryptedKey))
            return string.Empty;

        return _secretProtector.Unprotect(key.EncryptedKey);
    }

    /// <summary>
    ///     重新排序服务商列表
    /// </summary>
    /// <param name="orderedProviderIds">按新顺序排列的服务商 ID 列表</param>
    public void ReorderProviders(List<Guid> orderedProviderIds)
    {
        var config = _storage.Load();
        for (var i = 0; i < orderedProviderIds.Count; i++)
        {
            var provider = config.Providers.FirstOrDefault(p => p.Id == orderedProviderIds[i]);
            if (provider != null) provider.SortOrder = i;
        }

        _storage.Save(config);
    }
}
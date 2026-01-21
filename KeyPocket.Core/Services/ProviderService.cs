using System;
using System.Collections.Generic;
using System.Linq;
using KeyPocket.Core.Models;
using KeyPocket.Core.Storage;
using KeyPocket.Core.Crypto;

namespace KeyPocket.Core.Services;

/// <summary>
/// 管理 Provider 的业务逻辑。
/// </summary>
public class ProviderService
{


    private readonly IStorageProvider _storage;
    private readonly ISecretProtector _secretProtector;

    public ProviderService(IStorageProvider storage, ISecretProtector secretProtector)
    {
        _storage = storage;
        _secretProtector = secretProtector;
    }

    public List<Provider> GetAllProviders()
    {
        return _storage.Load().Providers;
    }

    /// <summary>
    /// 生成默认的供应商名称（例如 "New Provider 1"）
    /// </summary>
    private string GenerateDefaultProviderName()
    {
        var config = _storage.Load();
        int counter = 1;
        string baseName = "New Provider";
        string name = $"{baseName} {counter}";
        
        // 查找未使用的名称
        while (config.Providers.Any(p => p.Name == name))
        {
            counter++;
            name = $"{baseName} {counter}";
        }
        
        return name;
    }

    /// <summary>
    /// 创建一个默认配置的供应商
    /// </summary>
    public Provider CreateProvider()
    {
        return CreateProvider(
            GenerateDefaultProviderName(),
            "OpenAI API",
            null,
            null
        );
    }

    public Provider CreateProvider(string name, string type, string? baseUrl = null, string? description = null)
    {
        var config = _storage.Load();
        var newProvider = new Provider
        {
            Name = name,
            Type = type,
            ApiBaseUrl = baseUrl,
            Description = description
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
            // 删除图标文件（如果存在）
            if (!string.IsNullOrEmpty(provider.IconPath))
            {
                try
                {
                    var iconFile = System.IO.Path.Combine(
                        Windows.Storage.ApplicationData.Current.LocalFolder.Path,
                        provider.IconPath
                    );
                    if (System.IO.File.Exists(iconFile))
                    {
                        System.IO.File.Delete(iconFile);
                    }
                }
                catch
                {
                    // 忽略删除图标文件的错误
                }
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
}

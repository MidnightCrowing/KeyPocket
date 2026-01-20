using System;
using System.Linq;
using KeyPocket.Core.Storage;

namespace KeyPocket.Core.Services;

/// <summary>
/// 管理收藏夹（模型与 API Key）的业务逻辑。
/// </summary>
public class FavoriteService
{
    private readonly IStorageProvider _storage;

    public FavoriteService(IStorageProvider storage)
    {
        _storage = storage;
    }

    public void ToggleModelFavorite(Guid providerId, string modelId)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        if (provider == null) return;

        if (provider.FavoriteModelIds.Contains(modelId))
        {
            provider.FavoriteModelIds.Remove(modelId);
        }
        else
        {
            provider.FavoriteModelIds.Add(modelId);
        }

        _storage.Save(config);
    }

    public void ToggleApiKeyFavorite(Guid providerId, Guid apiKeyId)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        if (provider == null) return;

        if (provider.FavoriteApiKeyIds.Contains(apiKeyId))
        {
            provider.FavoriteApiKeyIds.Remove(apiKeyId);
        }
        else
        {
            provider.FavoriteApiKeyIds.Add(apiKeyId);
        }

        _storage.Save(config);
    }
}

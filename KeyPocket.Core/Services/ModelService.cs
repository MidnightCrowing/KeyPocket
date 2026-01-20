using System;
using System.Collections.Generic;
using System.Linq;
using KeyPocket.Core.Models;
using KeyPocket.Core.Storage;

namespace KeyPocket.Core.Services;

/// <summary>
/// 管理 AI 模型的业务逻辑。
/// </summary>
public class ModelService
{
    private readonly IStorageProvider _storage;

    public ModelService(IStorageProvider storage)
    {
        _storage = storage;
    }

    public List<ModelInfo> GetModels(Guid providerId)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        return provider?.Models ?? new List<ModelInfo>();
    }

    public void AddModel(ModelInfo model)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == model.ProviderId);
        if (provider == null) throw new InvalidOperationException("Provider not found.");

        if (provider.Models.Any(m => m.Id == model.Id))
        {
            throw new InvalidOperationException("Model already exists.");
        }

        provider.Models.Add(model);
        _storage.Save(config);
    }

    public void RemoveModel(Guid providerId, string modelId)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        var model = provider?.Models.FirstOrDefault(m => m.Id == modelId);

        if (model != null)
        {
            provider!.Models.Remove(model);
            _storage.Save(config);
        }
    }

    public void UpdatePricing(Guid providerId, string modelId, decimal? inputPrice, decimal? outputPrice)
    {
        var config = _storage.Load();
        var provider = config.Providers.FirstOrDefault(p => p.Id == providerId);
        var model = provider?.Models.FirstOrDefault(m => m.Id == modelId);

        if (model != null)
        {
            model.InputPricePerMTokens = inputPrice;
            model.OutputPricePerMTokens = outputPrice;
            _storage.Save(config);
        }
    }
}

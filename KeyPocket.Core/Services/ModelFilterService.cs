using KeyPocket.Core.Models;

namespace KeyPocket.Core.Services;

/// <summary>
///     模型过滤和排序服务
/// </summary>
public class ModelFilterService
{
    /// <summary>
    ///     应用过滤和排序条件到模型列表
    /// </summary>
    public List<ModelInfo> ApplyFilters(
        List<ModelInfo> allModels,
        ModelFilterCriteria criteria)
    {
        var query = allModels.AsEnumerable();

        // 1. 搜索文本过滤
        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var lower = criteria.SearchText.ToLowerInvariant();
            query = query.Where(m =>
                m.DisplayName.ToLowerInvariant().Contains(lower) ||
                m.Id.ToLowerInvariant().Contains(lower));
        }

        // 2. 收藏过滤
        if (criteria.ShowFavoritesOnly)
            query = query.Where(m => m.Tags.Contains(ModelTags.Favorite));

        // 3. Capability 过滤
        // 如果所有 Capability 都选中或都未选中,则不应用过滤
        if (criteria.SelectedCapabilities.Count > 0 && criteria.SelectedCapabilities.Count < 6)
            query = query.Where(m =>
                (criteria.SelectedCapabilities.Contains("Text") && m.Tags.Contains(ModelTags.Text)) ||
                (criteria.SelectedCapabilities.Contains("File") && m.Tags.Contains(ModelTags.File)) ||
                (criteria.SelectedCapabilities.Contains("Image") && m.Tags.Contains(ModelTags.Image)) ||
                (criteria.SelectedCapabilities.Contains("Audio") && m.Tags.Contains(ModelTags.Audio)) ||
                (criteria.SelectedCapabilities.Contains("Video") && m.Tags.Contains(ModelTags.Video)) ||
                (criteria.SelectedCapabilities.Contains("Embeddings") && m.Tags.Contains(ModelTags.Embeddings))
            );

        // 4. 价格范围过滤
        if (criteria.MinPrice.HasValue || criteria.MaxPrice.HasValue)
            query = query.Where(m =>
            {
                var price = m.InputPricePerMTokens ?? 0;
                return price >= (criteria.MinPrice ?? 0) &&
                       price <= (criteria.MaxPrice ?? decimal.MaxValue);
            });

        // 5. 排序
        query = criteria.SortOption switch
        {
            ModelSortOption.NameAsc => query.OrderBy(m => m.DisplayName),
            ModelSortOption.NameDesc => query.OrderByDescending(m => m.DisplayName),
            ModelSortOption.PriceLowHigh => query.OrderBy(m => m.InputPricePerMTokens ?? decimal.MaxValue),
            ModelSortOption.PriceHighLow => query.OrderByDescending(m => m.InputPricePerMTokens ?? 0),
            _ => query
        };

        return query.ToList();
    }
}

/// <summary>
///     模型过滤条件
/// </summary>
public class ModelFilterCriteria
{
    public string SearchText { get; set; } = string.Empty;
    public bool ShowFavoritesOnly { get; set; }
    public List<string> SelectedCapabilities { get; set; } = new();
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ModelSortOption SortOption { get; set; } = ModelSortOption.NameAsc;
}

/// <summary>
///     模型排序选项
/// </summary>
public enum ModelSortOption
{
    NameAsc,
    NameDesc,
    PriceLowHigh,
    PriceHighLow
}
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KeyPocket.Core.Services;

namespace KeyPocket.UI.ViewModels;

public partial class ModelsViewModel
{
    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnShowFavoritesOnlyChanged(bool value) => ApplyFilters();
    partial void OnIsTextSelectedChanged(bool value) => ApplyFilters();
    partial void OnIsFileSelectedChanged(bool value) => ApplyFilters();
    partial void OnIsImageSelectedChanged(bool value) => ApplyFilters();
    partial void OnIsAudioSelectedChanged(bool value) => ApplyFilters();
    partial void OnIsVideoSelectedChanged(bool value) => ApplyFilters();
    partial void OnIsEmbeddingsSelectedChanged(bool value) => ApplyFilters();
    partial void OnMinPriceIndexChanged(int value) => ApplyFilters();
    partial void OnMaxPriceIndexChanged(int value) => ApplyFilters();
    partial void OnSortOptionChanged(ModelSortOption value) => ApplyFilters();

    private void ApplyFilters()
    {
        var criteria = new ModelFilterCriteria
        {
            SearchText = SearchText,
            ShowFavoritesOnly = ShowFavoritesOnly,
            SelectedCapabilities = GetSelectedCapabilities(),
            MinPrice = ConvertPriceIndexToValue(MinPriceIndex),
            MaxPrice = ConvertPriceIndexToValue(MaxPriceIndex),
            SortOption = SortOption
        };

        var allModelsData = _allModels.Select(vm => new Core.Models.ModelInfo
        {
            Id = vm.Id,
            DisplayName = vm.DisplayName,
            Tags = new HashSet<string>(vm._model.Tags),
            InputPricePerMTokens = vm.InputPrice
        }).ToList();

        var filtered = _filterService.ApplyFilters(allModelsData, criteria);
        var orderedVMs = filtered.Select(m => _allModels.First(vm => vm.Id == m.Id)).ToList();

        FilteredModels = new ObservableCollection<ModelItemViewModel>(orderedVMs);
        GenerateGroupedModels(orderedVMs);
    }

    private List<string> GetSelectedCapabilities()
    {
        var list = new List<string>();
        if (IsTextSelected) list.Add("Text");
        if (IsFileSelected) list.Add("File");
        if (IsImageSelected) list.Add("Image");
        if (IsAudioSelected) list.Add("Audio");
        if (IsVideoSelected) list.Add("Video");
        if (IsEmbeddingsSelected) list.Add("Embeddings");
        return list;
    }

    private decimal? ConvertPriceIndexToValue(int index)
    {
        return index switch
        {
            0 => 0m,
            1 => 0.1m,
            2 => 0.2m,
            3 => 0.5m,
            4 => 1m,
            5 => 5m,
            6 => null,
            _ => null
        };
    }

    public void ResetFilters()
    {
        IsTextSelected = true;
        IsFileSelected = true;
        IsImageSelected = true;
        IsAudioSelected = true;
        IsVideoSelected = true;
        IsEmbeddingsSelected = true;
        MinPriceIndex = 0;
        MaxPriceIndex = 6;
        SortOption = ModelSortOption.NameAsc;
    }
}

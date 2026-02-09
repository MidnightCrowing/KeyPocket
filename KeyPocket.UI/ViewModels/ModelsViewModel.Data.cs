using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KeyPocket.UI.Helpers;

namespace KeyPocket.UI.ViewModels;

public partial class ModelsViewModel
{
    private void UpdateCurrencySymbol()
    {
        var currency = SettingsHelper.Current.SelectedCurrency;
        var symbols = SettingsHelper.Current.CurrencySymbols;
        CurrencySymbol = symbols.TryGetValue(currency, out var symbol) ? symbol : currency;
    }

    public void LoadData()
    {
        _allModels.Clear();
        var providers = _providerService.GetAllProviders();

        foreach (var provider in providers)
        {
            if (provider.Models == null) continue;

            foreach (var model in provider.Models)
                _allModels.Add(new ModelItemViewModel(model, provider.Name, provider.IconPath, provider.Currency,
                    _providerService));
        }

        OnPropertyChanged(nameof(IsTextSelected));
        OnPropertyChanged(nameof(IsFileSelected));
        OnPropertyChanged(nameof(IsImageSelected));
        OnPropertyChanged(nameof(IsAudioSelected));
        OnPropertyChanged(nameof(IsVideoSelected));
        OnPropertyChanged(nameof(IsEmbeddingsSelected));

        ApplyFilters();
    }

    private void GenerateGroupedModels(List<ModelItemViewModel> models)
    {
        var providers = _providerService.GetAllProviders();
        var providerOrderMap = providers
            .Select((p, index) => new { p.Name, Index = index })
            .ToDictionary(x => x.Name, x => x.Index);

        var grouped = models
            .GroupBy(m => new { m.ProviderName, m.ProviderIcon })
            .OrderBy(g => providerOrderMap.TryGetValue(g.Key.ProviderName, out var index) ? index : int.MaxValue)
            .Select(g => new ProviderGroupViewModel
            {
                ProviderId = providers.FirstOrDefault(p => p.Name == g.Key.ProviderName)?.Id ?? Guid.Empty,
                ProviderName = g.Key.ProviderName,
                ProviderIcon = g.Key.ProviderIcon,
                Models = g.ToList()
            })
            .ToList();

        GroupedModels = new ObservableCollection<ProviderGroupViewModel>(grouped);
    }
}

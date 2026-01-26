using Microsoft.UI.Xaml.Controls;
using KeyPocket.UI.ViewModels;
using Windows.Globalization.NumberFormatting;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;

namespace KeyPocket.UI.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = new SettingsViewModel();
        InitializeComponent();
        ConfigureRateFormatter();

        // Initial render
        RenderRates();
        ViewModel.Rates.CollectionChanged += (s, e) => RenderRates();
    }

    private void RenderRates()
    {
        // SettingsExpander crashes when using ItemsSource with certain templates or wrappers.
        // We manually populate the Items collection with strictly Typed SettingsCards.
        
        if (RatesExpander == null) return;
        
        RatesExpander.Items.Clear();

        var rateTemplate = Resources["RateTemplate"] as DataTemplate;
        var footerTemplate = Resources["AddFooterTemplate"] as DataTemplate;

        foreach (var item in ViewModel.Rates)
        {
            DataTemplate? template = null;
            if (item is ExchangeRateItem) template = rateTemplate;
            else if (item is AddRatePlaceholder) template = footerTemplate;

            if (template != null)
            {
                var element = template.LoadContent() as FrameworkElement;
                if (element is SettingsCard card)
                {
                   card.DataContext = item;
                   RatesExpander.Items.Add(card);
                }
            }
        }
    }

    private void ConfigureRateFormatter()
    {
        if (Resources["RateFormatter"] is DecimalFormatter formatter)
        {
            var rounder = new IncrementNumberRounder();
            rounder.Increment = 0.01;
            rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp;
            formatter.NumberRounder = rounder;
        }
    }
}
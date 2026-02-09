using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.UI.Messages;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace KeyPocket.UI.Pages.ProviderSettings;

public sealed partial class ProviderSettingsModelsContent : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ProviderSettingsViewModel),
            typeof(ProviderSettingsModelsContent),
            new PropertyMetadata(null));

    public ProviderSettingsModelsContent()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public ProviderSettingsViewModel? ViewModel
    {
        get => (ProviderSettingsViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<CsvImportResultMessage>(this, OnCsvImportResult);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CsvImportResultMessage>(this);
    }

    private void OnCsvImportResult(object recipient, CsvImportResultMessage message)
    {
        var severity = InfoBarSeverity.Success;
        string title;
        string messageText;

        if (message.SuccessCount == 0 && message.SkipCount == 0)
        {
            severity = InfoBarSeverity.Warning;
            title = "No Data";
            messageText = "The CSV file is empty or has no valid data.";
        }
        else if (message.SuccessCount == 0)
        {
            severity = InfoBarSeverity.Error;
            title = "Import Failed";
            messageText = $"All {message.SkipCount} rows were skipped due to errors.";
        }
        else if (message.SkipCount > 0)
        {
            severity = InfoBarSeverity.Informational;
            title = "Import Completed";
            messageText = $"Successfully imported {message.SuccessCount} models, skipped {message.SkipCount} rows.";
        }
        else
        {
            severity = InfoBarSeverity.Success;
            title = "Import Successful";
            messageText = $"Successfully imported {message.SuccessCount} models.";
        }

        CsvImportInfoBar.Severity = severity;
        CsvImportInfoBar.Title = title;
        CsvImportInfoBar.Message = messageText;
        CsvImportInfoBar.IsOpen = true;
    }

    private void OnCopyModelIdClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string modelId)
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(modelId);
                Clipboard.SetContent(dataPackage);
            }
            catch
            {
                // Silently fail
            }
    }

    private void OnModelEditKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape && sender is FrameworkElement element &&
            element.DataContext is ModelWrapper wrapper)
        {
            wrapper.CancelAddCommand?.Execute(null);
            e.Handled = true;
        }
    }

    private void OnTokenItemAdding(CommunityToolkit.WinUI.Controls.TokenizingTextBox sender,
        CommunityToolkit.WinUI.Controls.TokenItemAddingEventArgs e)
    {
        var match = ModelWrapper.AvailableTagsList.FirstOrDefault(t =>
            t.Equals(e.TokenText, StringComparison.OrdinalIgnoreCase));

        if (match != null)
        {
            e.Item = match;
        }
        else
        {
            e.Cancel = true;
        }
    }

    private void OnTokenTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput &&
            sender.DataContext is ModelWrapper wrapper)
        {
            wrapper.UpdateSuggestions(sender.Text);
        }
    }

    private void OnPriceValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (sender.DataContext is ModelWrapper wrapper)
        {
            wrapper.RefreshCurrencySymbol();
        }
    }
}

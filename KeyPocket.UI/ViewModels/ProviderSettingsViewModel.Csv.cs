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
    [RelayCommand]
    public async Task GenerateCsvTemplate()
    {
        try
        {
            var savePicker = new FileSavePicker();
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(savePicker, hwnd);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("CSV File", new List<string> { ".csv" });
            savePicker.SuggestedFileName = $"{Provider.Name}_models_template";

            var file = await savePicker.PickSaveFileAsync();
            if (file == null) return;

            var csvContent = new StringBuilder();
            csvContent.AppendLine("ModelId,Name,InputPrice,OutputPrice,Tags");
            csvContent.AppendLine("eg:gpt-4-turbo,GPT-4 Turbo,0.010,0.030,Text");
            csvContent.AppendLine("eg:gpt-3.5-turbo,GPT-3.5 Turbo,0.001,0.002,Text");
            csvContent.AppendLine("eg:text-embedding-3-small,Text Embedding Small,0.000,,Embeddings");
            csvContent.AppendLine("eg:gpt-4-vision,GPT-4 Vision,0.010,0.030,\"Text,Image\"");

            await FileIO.WriteTextAsync(file, csvContent.ToString(), UnicodeEncoding.Utf8);
        }
        catch (Exception)
        {
            // Silently fail or show error dialog
        }
    }

    [RelayCommand]
    public async Task ImportCsv()
    {
        try
        {
            var openPicker = new FileOpenPicker();
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(openPicker, hwnd);

            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".csv");

            var file = await openPicker.PickSingleFileAsync();
            if (file == null) return;

            var csvText = await FileIO.ReadTextAsync(file, UnicodeEncoding.Utf8);
            var lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
            {
                ShowImportResult(0, 0);
                return;
            }

            // Parse header
            var header = ParseCsvLine(lines[0]);
            var modelIdIndex = Array.FindIndex(header, h => h.Equals("ModelId", StringComparison.OrdinalIgnoreCase));
            var nameIndex = Array.FindIndex(header, h => h.Equals("Name", StringComparison.OrdinalIgnoreCase));
            var inputPriceIndex =
                Array.FindIndex(header, h => h.Equals("InputPrice", StringComparison.OrdinalIgnoreCase));
            var outputPriceIndex =
                Array.FindIndex(header, h => h.Equals("OutputPrice", StringComparison.OrdinalIgnoreCase));
            var tagsIndex = Array.FindIndex(header, h => h.Equals("Tags", StringComparison.OrdinalIgnoreCase));

            if (modelIdIndex == -1)
            {
                ShowImportResult(0, 0);
                return;
            }

            var successCount = 0;
            var skipCount = 0;

            for (var i = 1; i < lines.Length; i++)
                try
                {
                    var fields = ParseCsvLine(lines[i]);
                    if (fields.Length <= modelIdIndex || string.IsNullOrWhiteSpace(fields[modelIdIndex]))
                    {
                        skipCount++;
                        continue;
                    }

                    var modelId = fields[modelIdIndex].Trim();

                    // Skip example data with eg: prefix
                    if (modelId.StartsWith("eg:", StringComparison.OrdinalIgnoreCase))
                    {
                        skipCount++;
                        continue;
                    }

                    var displayName = nameIndex >= 0 && nameIndex < fields.Length &&
                                      !string.IsNullOrWhiteSpace(fields[nameIndex])
                        ? fields[nameIndex].Trim()
                        : FormatDefaultModelName(modelId);

                    decimal? inputPrice = null;
                    if (inputPriceIndex >= 0 && inputPriceIndex < fields.Length)
                        if (decimal.TryParse(fields[inputPriceIndex], NumberStyles.Any, CultureInfo.InvariantCulture,
                                out var ip))
                            inputPrice = Math.Round(ip, 3);

                    decimal? outputPrice = null;
                    if (outputPriceIndex >= 0 && outputPriceIndex < fields.Length)
                        if (decimal.TryParse(fields[outputPriceIndex], NumberStyles.Any, CultureInfo.InvariantCulture,
                                out var op))
                            outputPrice = Math.Round(op, 3);

                    // 解析标签
                    var tags = new HashSet<string>();
                    if (tagsIndex >= 0 && tagsIndex < fields.Length && !string.IsNullOrWhiteSpace(fields[tagsIndex]))
                    {
                        var tagValues = fields[tagsIndex]
                            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var tag in tagValues)
                        {
                            var trimmedTag = tag.Trim();
                            // 验证标签是否为预定义标签
                            if (trimmedTag == ModelTags.Text || trimmedTag == ModelTags.File ||
                                trimmedTag == ModelTags.Image || trimmedTag == ModelTags.Audio ||
                                trimmedTag == ModelTags.Video || trimmedTag == ModelTags.Embeddings ||
                                trimmedTag == ModelTags.Deprecated)
                            {
                                tags.Add(trimmedTag);
                            }
                        }
                    }

                    // 如果没有标签,默认添加 Text 标签
                    if (tags.Count == 0)
                    {
                        tags.Add(ModelTags.Text);
                    }

                    // Upsert logic: check if model exists in Provider.Models
                    var existingModel =
                        Provider.Models.FirstOrDefault(m => m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));
                    if (existingModel != null)
                    {
                        // Update existing model
                        existingModel.DisplayName = displayName;
                        existingModel.InputPricePerMTokens = inputPrice;
                        existingModel.OutputPricePerMTokens = outputPrice;
                        existingModel.Tags = tags;
                    }
                    else
                    {
                        // Add new model to Provider.Models
                        var newModel = new ModelInfo
                        {
                            Id = modelId,
                            DisplayName = displayName,
                            ProviderId = Provider.Id,
                            InputPricePerMTokens = inputPrice,
                            OutputPricePerMTokens = outputPrice,
                            Tags = tags
                        };
                        Provider.Models.Add(newModel);
                    }

                    successCount++;
                }
                catch
                {
                    skipCount++;
                }

            // Save all changes at once
            _providerService.UpdateProvider(Provider);

            // Reload and refresh UI using incremental update
            RefreshModels();

            ShowImportResult(successCount, skipCount);
        }
        catch (Exception)
        {
            // Silently fail or show error dialog
        }
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var regex = new Regex(@"(?:^|,)(""(?:[^""]+|"""")*""|[^,]*)");
        var matches = regex.Matches(line);

        foreach (Match match in matches)
        {
            var value = match.Groups[1].Value;
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value.Substring(1, value.Length - 2).Replace("\"\"", "\"");
            result.Add(value);
        }

        return result.ToArray();
    }

    private void ShowImportResult(int successCount, int skipCount)
    {
        // Send message to show InfoBar in the page
        WeakReferenceMessenger.Default.Send(new CsvImportResultMessage(successCount, skipCount));
    }
}

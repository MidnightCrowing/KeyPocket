using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Services;
using KeyPocket.UI.Messages;

namespace KeyPocket.UI.ViewModels;

/// <summary>
///     主窗口的 ViewModel，管理侧边栏服务商列表
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ProviderService _providerService;

    public MainWindowViewModel(ProviderService providerService)
    {
        _providerService = providerService;

        // 加载服务商列表
        LoadProviders();

        // 订阅消息
        WeakReferenceMessenger.Default.Register<ProviderCreatedMessage>(this, OnProviderCreated);
        WeakReferenceMessenger.Default.Register<ProviderUpdatedMessage>(this, OnProviderUpdated);
        WeakReferenceMessenger.Default.Register<ProviderDeletedMessage>(this, OnProviderDeleted);
    }

    /// <summary>
    ///     侧边栏服务商列表
    /// </summary>
    public ObservableCollection<SidebarProviderItem> Providers { get; } = new();

    /// <summary>
    ///     从服务加载服务商列表
    /// </summary>
    public void LoadProviders()
    {
        Providers.Clear();
        var providers = _providerService.GetAllProviders();

        foreach (var provider in providers)
            Providers.Add(new SidebarProviderItem
            {
                Id = provider.Id,
                Name = provider.Name,
                IconPath = provider.IconPath,
                Type = provider.Type
            });
    }

    /// <summary>
    ///     处理服务商创建消息
    /// </summary>
    private void OnProviderCreated(object recipient, ProviderCreatedMessage message)
    {
        // 确保在 UI 线程上执行
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            var provider = _providerService.GetAllProviders()
                .FirstOrDefault(p => p.Id == message.ProviderId);

            if (provider != null)
                Providers.Add(new SidebarProviderItem
                {
                    Id = provider.Id,
                    Name = provider.Name,
                    IconPath = provider.IconPath,
                    Type = provider.Type
                });
        });
    }

    /// <summary>
    ///     处理服务商更新消息
    /// </summary>
    private void OnProviderUpdated(object recipient, ProviderUpdatedMessage message)
    {
        var existingItem = Providers.FirstOrDefault(p => p.Id == message.ProviderId);
        if (existingItem != null)
        {
            // 从服务获取最新数据
            var provider = _providerService.GetAllProviders()
                .FirstOrDefault(p => p.Id == message.ProviderId);

            if (provider != null)
            {
                // 更新属性（触发 UI 更新）
                existingItem.Name = provider.Name;
                existingItem.IconPath = provider.IconPath;
                existingItem.Type = provider.Type;
            }
        }
    }

    /// <summary>
    ///     处理服务商删除消息
    /// </summary>
    private void OnProviderDeleted(object recipient, ProviderDeletedMessage message)
    {
        var item = Providers.FirstOrDefault(p => p.Id == message.ProviderId);
        if (item != null) Providers.Remove(item);
    }
}
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Helpers;

/// <summary>
///     crash.log 日志文件管理辅助类
/// </summary>
public static class CrashLogHelper
{
    /// <summary>
    ///     获取 crash.log 文件的完整路径
    /// </summary>
    /// <returns>crash.log 文件路径</returns>
    public static string GetCrashLogPath()
    {
        var localFolder = ApplicationData.Current.LocalFolder;
        return Path.Combine(localFolder.Path, "crash.log");
    }

    /// <summary>
    ///     使用系统默认编辑器打开 crash.log 文件
    /// </summary>
    /// <param name="xamlRoot">用于显示错误对话框的 XamlRoot</param>
    /// <returns>异步任务</returns>
    public static async Task OpenCrashLogAsync(XamlRoot? xamlRoot = null)
    {
        var filePath = GetCrashLogPath();
        await OpenFileWithDefaultEditorAsync(filePath, xamlRoot);
    }

    /// <summary>
    ///     使用系统默认编辑器打开指定文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="xamlRoot">用于显示错误对话框的 XamlRoot</param>
    /// <returns>异步任务</returns>
    public static async Task OpenFileWithDefaultEditorAsync(string filePath, XamlRoot? xamlRoot = null)
    {
        try
        {
            // 如果文件不存在，创建空文件
            if (!File.Exists(filePath)) File.WriteAllText(filePath, "");

            var file = await StorageFile.GetFileFromPathAsync(filePath);
            await Launcher.LaunchFileAsync(file);
        }
        catch (Exception ex)
        {
            // 如果提供了 XamlRoot，显示错误对话框
            if (xamlRoot != null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Cannot open file: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = xamlRoot
                };
                await dialog.ShowAsync();
            }
            else
            {
                // 否则抛出异常
                throw;
            }
        }
    }

    /// <summary>
    ///     检查 crash.log 文件是否存在
    /// </summary>
    /// <returns>文件是否存在</returns>
    public static bool CrashLogExists()
    {
        var filePath = GetCrashLogPath();
        return File.Exists(filePath);
    }

    /// <summary>
    ///     清空 crash.log 文件内容
    /// </summary>
    public static void ClearCrashLog()
    {
        var filePath = GetCrashLogPath();
        if (File.Exists(filePath)) File.WriteAllText(filePath, "");
    }

    /// <summary>
    ///     记录异常到 crash.log
    /// </summary>
    /// <param name="source">异常来源</param>
    /// <param name="ex">异常对象</param>
    public static void LogException(string source, Exception? ex)
    {
        if (ex == null) return;

        try
        {
            var filePath = GetCrashLogPath();
            var logContent =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\nData: {ex.Data}\nMessage: {ex.Message}\nStack: {ex.StackTrace}\n\n";
            File.AppendAllText(filePath, logContent, Encoding.UTF8);
        }
        catch
        {
            // Suppress errors during logging
        }
    }
}
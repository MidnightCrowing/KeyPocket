using System.Text.Json;
using KeyPocket.Core.Models;

namespace KeyPocket.Core.Storage;

/// <summary>
///     基于 JSON 文件的存储实现。
/// </summary>
public class JsonFileStorageProvider : IStorageProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;

    public JsonFileStorageProvider(string filePath)
    {
        _filePath = filePath;
    }

    public KeyPocketConfig Load()
    {
        if (!File.Exists(_filePath)) return new KeyPocketConfig();

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<KeyPocketConfig>(json, JsonOptions) ?? new KeyPocketConfig();
        }
        catch (Exception)
        {
            // 考虑记录日志或备份损坏的文件
            return new KeyPocketConfig();
        }
    }

    public void Save(KeyPocketConfig config)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

        var tempPath = _filePath + ".tmp";
        try
        {
            // 原子写入的第一步：先写到临时文件
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(tempPath, json);

            // 原子写入的第二步：将临时文件重命名（覆盖）原文件
            if (File.Exists(_filePath))
                File.Replace(tempPath, _filePath, null);
            else
                File.Move(tempPath, _filePath);
        }
        catch (Exception)
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }
    }
}
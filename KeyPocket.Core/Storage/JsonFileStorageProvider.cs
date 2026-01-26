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
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var tempPath = _filePath + ".tmp";
        var backupPath = _filePath + ".bak";

        try
        {
            // 1. 先写临时文件
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(tempPath, json);

            // 2. 如果目标存在，先移到备份
            if (File.Exists(_filePath))
            {
                File.Move(_filePath, backupPath, true);
            }

            // 3. 将临时文件移正
            File.Move(tempPath, _filePath);

            // 4. 成功后删除备份
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
        catch (Exception)
        {
            // 如果出错了（比如第3步失败），尝试用备份恢复
            if (!File.Exists(_filePath) && File.Exists(backupPath))
            {
                try { File.Move(backupPath, _filePath); } catch { }
            }

            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }
    }
}
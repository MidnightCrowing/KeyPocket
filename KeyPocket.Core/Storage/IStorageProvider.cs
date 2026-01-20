using KeyPocket.Core.Models;

namespace KeyPocket.Core.Storage;

/// <summary>
/// 存储抽象。
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// 加载配置。
    /// </summary>
    /// <returns>配置对象，如果文件不存在则返回新对象</returns>
    KeyPocketConfig Load();

    /// <summary>
    /// 保存配置。
    /// </summary>
    /// <param name="config">要保存的配置对象</param>
    void Save(KeyPocketConfig config);
}

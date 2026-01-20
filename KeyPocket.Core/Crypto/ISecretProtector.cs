namespace KeyPocket.Core.Crypto;

/// <summary>
/// 提供敏感信息的保护（加密）与解（解密）能力。
/// </summary>
public interface ISecretProtector
{
    /// <summary>
    /// 加密明文。
    /// </summary>
    /// <param name="plainText">明文字符串</param>
    /// <returns>加密后的 Base64 字符串</returns>
    string Protect(string plainText);

    /// <summary>
    /// 解密密文。
    /// </summary>
    /// <param name="cipherText">加密后的 Base64 字符串</param>
    /// <returns>解密后的明文字符串</returns>
    string Unprotect(string cipherText);
}

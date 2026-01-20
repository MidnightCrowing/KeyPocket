using System.Security.Cryptography;
using System.Text;

namespace KeyPocket.Core.Crypto;

/// <summary>
/// 基于 Windows DPAPI 的加密实现。
/// </summary>
public class DpapiSecretProtector : ISecretProtector
{
    // 可选的附加熵（Entropy），增加安全性
    private static readonly byte[]? OptionalEntropy = null;

    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = ProtectedData.Protect(
            plainBytes, 
            OptionalEntropy, 
            DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(cipherBytes);
    }

    public string Unprotect(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;

        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] plainBytes = ProtectedData.Unprotect(
                cipherBytes, 
                OptionalEntropy, 
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException)
        {
            // 如果解密失败（可能是由于不同的用户或环境），返回空字符串或抛出自定义异常
            // 这里简单处理，实际可根据业务需求完善
            return string.Empty;
        }
    }
}

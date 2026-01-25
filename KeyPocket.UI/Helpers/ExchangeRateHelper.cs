using System.Collections.Generic;

namespace KeyPocket.UI.Helpers;

public static class ExchangeRateHelper
{
    /// <summary>
    /// 将金额从一种货币转换为另一种货币。
    /// </summary>
    /// <param name="amount">原始金额</param>
    /// <param name="fromCurrency">源货币代码 (e.g. "USD")</param>
    /// <param name="toCurrency">目标货币代码 (e.g. "CNY")</param>
    /// <returns>转换后的金额。如果无法转换，返回 null。</returns>
    public static decimal? Convert(decimal amount, string fromCurrency, string toCurrency)
    {
        // 1. 同一种货币，直接返回
        if (string.Equals(fromCurrency, toCurrency, System.StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        // 2. 查找直接汇率 (Source -> Target)
        var rates = SettingsHelper.Current.ExchangeRates;
        string directKey = $"{fromCurrency.ToUpper()}_{toCurrency.ToUpper()}";
        if (rates.TryGetValue(directKey, out decimal rate))
        {
            return amount * rate;
        }

        // 3. 查找反向汇率 (Target -> Source)，取倒数
        string reverseKey = $"{toCurrency.ToUpper()}_{fromCurrency.ToUpper()}";
        if (rates.TryGetValue(reverseKey, out decimal reverseRate) && reverseRate != 0)
        {
            return amount / reverseRate;
        }

        // 4. (可选) 尝试通过中间货币转换？目前暂不实现，Keep It Simple.
        
        return null;
    }

    /// <summary>
    /// 获取货币符号
    /// </summary>
    public static string GetCurrencySymbol(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode)) return string.Empty;
        var code = currencyCode.ToUpper();
        if (SettingsHelper.Current.CurrencySymbols.TryGetValue(code, out var symbol))
        {
            return symbol;
        }
        return code + " ";
    }
}

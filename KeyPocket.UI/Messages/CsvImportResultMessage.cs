namespace KeyPocket.UI.Messages;

public class CsvImportResultMessage
{
    public int SuccessCount { get; }
    public int SkipCount { get; }

    public CsvImportResultMessage(int successCount, int skipCount)
    {
        SuccessCount = successCount;
        SkipCount = skipCount;
    }
}

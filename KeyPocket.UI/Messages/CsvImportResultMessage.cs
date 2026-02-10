namespace KeyPocket.UI.Messages;

public class CsvImportResultMessage
{
    public CsvImportResultMessage(int successCount, int skipCount)
    {
        SuccessCount = successCount;
        SkipCount = skipCount;
    }

    public int SuccessCount { get; }
    public int SkipCount { get; }
}
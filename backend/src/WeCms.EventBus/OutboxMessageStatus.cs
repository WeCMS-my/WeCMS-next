namespace WeCms.EventBus;

public static class OutboxMessageStatus
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Processed = "processed";
    public const string Failed = "failed";
}

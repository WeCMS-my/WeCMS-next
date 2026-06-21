namespace WeCms.Data.SqlSugar;

public sealed class DatabaseConfigurationException : InvalidOperationException
{
    public DatabaseConfigurationException(string message)
        : base(message)
    {
    }
}

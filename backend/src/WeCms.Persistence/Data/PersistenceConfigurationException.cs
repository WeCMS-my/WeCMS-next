namespace WeCms.Persistence.Data;

public sealed class PersistenceConfigurationException : InvalidOperationException
{
    public PersistenceConfigurationException(string message)
        : base(message)
    {
    }
}

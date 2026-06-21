namespace WeCms.EventBus;

public interface IOutboxLockTokenProvider
{
    string CreateLockToken();
}

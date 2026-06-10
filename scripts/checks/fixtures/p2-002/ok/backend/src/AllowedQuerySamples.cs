public class AllowedQuerySamples
{
    public string SqlWithoutDynamic() => "";

    public void Execute()
    {
        var query = "SELECT id, username FROM sys_user";
        _ = query;
    }
}

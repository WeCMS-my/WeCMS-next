namespace WeCms.Data.SqlSugar;

public enum DatabaseConnectionRole
{
    Main = 0,
    Log = 1,
    Audit = 2,
    File = 3,
    Tenant = 4
}

namespace WeCms.Persistence.Migration;

public sealed record DbMigrationScript(
    string Version,
    string Name,
    string Sql,
    string Checksum)
{
    public static DbMigrationScript Create(string version, string name, string sql)
    {
        return new DbMigrationScript(version, name, sql, ComputeChecksum(sql));
    }

    public static string ComputeChecksum(string sql)
    {
        var bytes = global::System.Security.Cryptography.SHA256.HashData(
            global::System.Text.Encoding.UTF8.GetBytes(sql));
        return Convert.ToHexStringLower(bytes);
    }
}

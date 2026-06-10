public class InvalidDynamicQuery
{
    public void Execute(dynamic repo, string sql)
    {
        var row = repo.Query<dynamic>(sql);
        var row2 = repo.QueryAsync < dynamic >(sql);
        var row3 = repo.Query<   dynamic >(sql);
    }
}

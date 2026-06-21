namespace WeCms.Data.SqlSugar;

public interface ICodeFirstModelProvider
{
    IReadOnlyCollection<Type> GetModelTypes();
}

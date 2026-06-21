namespace WeCms.Data.SqlSugar;

public interface ICodeFirstModelRegistry
{
    IReadOnlyList<Type> GetModelTypes();
}

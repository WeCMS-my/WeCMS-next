namespace WeCms.Data.SqlSugar;

public interface ICodeFirstRunner
{
    IReadOnlyList<Type> CollectModelTypes();

    Task InitializeDevelopmentAsync(CancellationToken cancellationToken = default);
}

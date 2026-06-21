using System.Linq.Expressions;
using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Data.SqlSugar;

public sealed class QueryFilterRegistrar : IQueryFilterRegistrar
{
    private readonly ICodeFirstModelRegistry _modelRegistry;
    private readonly IQueryFilterContextAccessor _contextAccessor;
    private readonly QueryFilterBypass _bypass;

    public QueryFilterRegistrar(
        ICodeFirstModelRegistry modelRegistry,
        IQueryFilterContextAccessor contextAccessor,
        QueryFilterBypass bypass)
    {
        _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _bypass = bypass ?? throw new ArgumentNullException(nameof(bypass));
    }

    public void Register(SqlSugarScopeProvider db)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (_bypass.IsBypassed)
        {
            return;
        }

        var context = _contextAccessor.Current;
        foreach (var modelType in _modelRegistry.GetModelTypes())
        {
            if (typeof(ISoftDeleteEntity).IsAssignableFrom(modelType))
            {
                db.QueryFilter.AddTableFilter(modelType, BuildSoftDeleteFilter(modelType), QueryFilterProvider.FilterJoinPosition.Where);
            }

            if (context.TenantId is not null && typeof(ITenantEntity).IsAssignableFrom(modelType))
            {
                db.QueryFilter.AddTableFilter(modelType, BuildTenantFilter(modelType, context.TenantId.Value), QueryFilterProvider.FilterJoinPosition.Where);
            }

            if (typeof(IDataScopedEntity).IsAssignableFrom(modelType))
            {
                db.QueryFilter.AddTableFilter(modelType, BuildDataScopeFilter(modelType, context.DataScopeUserIds), QueryFilterProvider.FilterJoinPosition.Where);
            }
        }
    }

    private static LambdaExpression BuildSoftDeleteFilter(Type modelType)
    {
        var parameter = Expression.Parameter(modelType, "entity");
        var deletedAt = Expression.Property(parameter, nameof(ISoftDeleteEntity.DeletedAt));
        var body = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTime?)));
        return Expression.Lambda(body, parameter);
    }

    private static LambdaExpression BuildTenantFilter(Type modelType, long tenantId)
    {
        var parameter = Expression.Parameter(modelType, "entity");
        var tenantProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
        var body = Expression.Equal(tenantProperty, Expression.Constant(tenantId));
        return Expression.Lambda(body, parameter);
    }

    private static LambdaExpression BuildDataScopeFilter(Type modelType, IReadOnlyCollection<long> dataScopeUserIds)
    {
        var parameter = Expression.Parameter(modelType, "entity");
        var createdByUserId = Expression.Property(parameter, nameof(IDataScopedEntity.CreatedByUserId));
        Expression body = dataScopeUserIds.Count == 0
            ? Expression.Constant(false)
            : Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                [typeof(long)],
                Expression.Constant(dataScopeUserIds.ToArray()),
                createdByUserId);

        return Expression.Lambda(body, parameter);
    }
}

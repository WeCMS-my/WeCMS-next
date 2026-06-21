using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.Modules.FileCenter.Files;
using WeCms.Modules.FileCenter.SqlSugar.Entities;
using WeCms.Modules.FileCenter.SqlSugar.Repositories;

namespace WeCms.Modules.FileCenter.SqlSugar;

public static class FileCenterSqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsFileCenterSqlSugar(this IServiceCollection services)
    {
        services.AddSingleton<ICodeFirstModelProvider, FileCenterCodeFirstModelProvider>();
        services.AddScoped<IFileRepository, FileRepository>();

        return services;
    }

    private sealed class FileCenterCodeFirstModelProvider : ICodeFirstModelProvider
    {
        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return [typeof(FileEntity)];
        }
    }
}

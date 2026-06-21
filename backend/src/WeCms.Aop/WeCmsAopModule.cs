using System.Reflection;
using Autofac;
using Autofac.Extras.DynamicProxy;

namespace WeCms.Aop;

public sealed class WeCmsAopModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<TransactionInterceptor>().InstancePerLifetimeScope();
        builder.RegisterType<CacheInterceptor>().InstancePerLifetimeScope();
        builder.RegisterType<ApplicationServiceAopInterceptor>().InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(ApplicationServiceAssemblies())
            .Where(type => IsApplicationService(type) && !IsRepositoryType(type) && !IsEndpointType(type) && !IsInfrastructureType(type))
            .AsImplementedInterfaces()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(ApplicationServiceAopInterceptor))
            .InstancePerLifetimeScope();
    }

    private static Assembly[] ApplicationServiceAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly =>
            {
                var name = assembly.GetName().Name;
                return name is not null
                    && name.StartsWith("WeCms.Modules.", StringComparison.Ordinal)
                    && !name.EndsWith(".SqlSugar", StringComparison.Ordinal);
            })
            .ToArray();
    }

    private static bool IsApplicationService(Type type)
    {
        return type is { IsClass: true, IsAbstract: false }
            && type.Name.EndsWith("Service", StringComparison.Ordinal)
            && type.GetInterfaces().Any(IsApplicationServiceInterface);
    }

    private static bool IsApplicationServiceInterface(Type type)
    {
        return type.IsInterface
            && type.Name.StartsWith("I", StringComparison.Ordinal)
            && type.Name.EndsWith("Service", StringComparison.Ordinal);
    }

    private static bool IsRepositoryType(Type type)
    {
        return type.Name.Contains("Repository", StringComparison.Ordinal)
            || type.GetInterfaces().Any(candidate => candidate.Name.Contains("Repository", StringComparison.Ordinal));
    }

    private static bool IsEndpointType(Type type)
    {
        return type.Name.Contains("Endpoint", StringComparison.Ordinal)
            || type.Namespace?.Contains(".Endpoints", StringComparison.Ordinal) == true;
    }

    private static bool IsInfrastructureType(Type type)
    {
        return type.Namespace?.Contains(".Infrastructure", StringComparison.Ordinal) == true
            || type.Namespace?.Contains(".SqlSugar", StringComparison.Ordinal) == true
            || type.Namespace?.Contains(".Entities", StringComparison.Ordinal) == true;
    }
}

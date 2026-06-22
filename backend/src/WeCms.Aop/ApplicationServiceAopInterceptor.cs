using System.Diagnostics;
using System.Reflection;
using Castle.DynamicProxy;
using WeCms.Shared.Data;
using WeCms.Shared.Endpoints;
using WeCms.Shared.Id;

namespace WeCms.Aop;

public sealed class ApplicationServiceAopInterceptor : IInterceptor
{
    private const string AuditRequestMethod = "SERVICE";

    private readonly TransactionInterceptor transactionInterceptor;
    private readonly CacheInterceptor cacheInterceptor;
    private readonly IAuditWriter auditWriter;
    private readonly ICacheTenantAccessor cacheTenantAccessor;
    private readonly IIdGenerator idGenerator;

    public ApplicationServiceAopInterceptor(
        TransactionInterceptor transactionInterceptor,
        CacheInterceptor cacheInterceptor,
        IAuditWriter auditWriter,
        ICacheTenantAccessor cacheTenantAccessor,
        IIdGenerator idGenerator)
    {
        this.transactionInterceptor = transactionInterceptor ?? throw new ArgumentNullException(nameof(transactionInterceptor));
        this.cacheInterceptor = cacheInterceptor ?? throw new ArgumentNullException(nameof(cacheInterceptor));
        this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
        this.cacheTenantAccessor = cacheTenantAccessor ?? throw new ArgumentNullException(nameof(cacheTenantAccessor));
        this.idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    public void Intercept(IInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var plan = BuildInvocationPlan(invocation);
        var returnType = invocation.Method.ReturnType;
        var cancellationToken = GetCancellationToken(invocation);

        if (returnType == typeof(Task))
        {
            invocation.ReturnValue = InterceptTaskAsync(invocation, plan, cancellationToken);
            return;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var method = typeof(ApplicationServiceAopInterceptor).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(static method => method.Name == nameof(InterceptTaskAsync) && method.IsGenericMethodDefinition);
            var generic = method.MakeGenericMethod(returnType.GetGenericArguments()[0]);
            invocation.ReturnValue = generic.Invoke(this, [invocation, plan, cancellationToken]);
            return;
        }

        throw new NotSupportedException($"Application service AOP supports only Task or Task<T> return types. Unsupported return type '{returnType}'.");
    }

    private Task InterceptTaskAsync(
        IInvocation invocation,
        InvocationPlan plan,
        CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task> operation = _ => ProceedTaskAsync(invocation);
        operation = ApplyCacheEviction(operation, invocation, plan);
        operation = ApplyTransaction(operation, plan);

        return ExecuteWithAuditAsync(plan, operation, cancellationToken);
    }

    private Task<TResult> InterceptTaskAsync<TResult>(
        IInvocation invocation,
        InvocationPlan plan,
        CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task<TResult>> operation = _ => ProceedTaskAsync<TResult>(invocation);
        operation = ApplyCacheableOrEvict(operation, invocation, plan);
        operation = ApplyTransaction(operation, plan);

        return ExecuteWithAuditAsync(plan, operation, cancellationToken);
    }

    private Func<CancellationToken, Task> ApplyCacheEviction(
        Func<CancellationToken, Task> operation,
        IInvocation invocation,
        InvocationPlan plan)
    {
        if (plan.CacheableAttributes.Count > 0)
        {
            throw new NotSupportedException($"{nameof(CacheableAttribute)} is only supported for Task<T> methods.");
        }

        if (plan.CacheEvictAttributes.Count == 0)
        {
            return operation;
        }

        var context = BuildCacheContext(invocation);

        foreach (var cacheEvict in plan.CacheEvictAttributes)
        {
            var current = operation;
            operation = token => cacheInterceptor.InvokeEvictAsync(cacheEvict, context, _ => current(token), token);
        }

        return operation;
    }

    private Func<CancellationToken, Task<TResult>> ApplyCacheableOrEvict<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IInvocation invocation,
        InvocationPlan plan)
    {
        if (plan.CacheableAttributes.Count == 0 && plan.CacheEvictAttributes.Count == 0)
        {
            return operation;
        }

        var context = BuildCacheContext(invocation);

        foreach (var cacheEvict in plan.CacheEvictAttributes)
        {
            var current = operation;
            operation = token => cacheInterceptor.InvokeEvictAsync(cacheEvict, context, _ => current(token), token);
        }

        foreach (var cacheable in plan.CacheableAttributes)
        {
            var current = operation;
            operation = token => ApplyCacheableAsync(cacheable, context, current, token);
        }

        return operation;
    }

    private async Task<TResult> ApplyCacheableAsync<TResult>(
        CacheableAttribute cacheable,
        CacheInvocationContext context,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        return (await cacheInterceptor.InvokeCacheableAsync(
            cacheable,
            context,
            _ => AsNullable(operation, cancellationToken),
            options: null,
            cancellationToken: cancellationToken))!;
    }

    private static async Task<TResult?> AsNullable<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        return await operation(cancellationToken);
    }

    private Func<CancellationToken, Task<TResult>> ApplyTransaction<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        InvocationPlan plan)
    {
        if (plan.UnitOfWork is null)
        {
            return operation;
        }

        return token => transactionInterceptor.InvokeAsync(_ => operation(token), token);
    }

    private Func<CancellationToken, Task> ApplyTransaction(
        Func<CancellationToken, Task> operation,
        InvocationPlan plan)
    {
        if (plan.UnitOfWork is null)
        {
            return operation;
        }

        return token => transactionInterceptor.InvokeAsync(_ => operation(token), token);
    }

    private async Task<TResult> ExecuteWithAuditAsync<TResult>(
        InvocationPlan plan,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        if (plan.Audited is null)
        {
            return await operation(cancellationToken);
        }

        await auditWriter.WriteAsync(CreateAuditRecord(plan, AuditWriteStatus.Started, string.Empty), cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await auditWriter.WriteAsync(CreateAuditRecord(plan, AuditWriteStatus.Completed, string.Empty), cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            await auditWriter.WriteAsync(CreateAuditRecord(plan, AuditWriteStatus.Failed, exception.Message), cancellationToken);
            throw;
        }
    }

    private async Task ExecuteWithAuditAsync(
        InvocationPlan plan,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        if (plan.Audited is null)
        {
            await operation(cancellationToken);
            return;
        }

        await auditWriter.WriteAsync(CreateAuditRecord(plan, AuditWriteStatus.Started, string.Empty), cancellationToken);
        try
        {
            await operation(cancellationToken);
            await auditWriter.WriteAsync(CreateAuditRecord(plan, AuditWriteStatus.Completed, string.Empty), cancellationToken);
        }
        catch (Exception exception)
        {
            await auditWriter.WriteAsync(CreateAuditRecord(plan, AuditWriteStatus.Failed, exception.Message), cancellationToken);
            throw;
        }
    }

    private CacheInvocationContext BuildCacheContext(IInvocation invocation)
    {
        return new CacheInvocationContext(cacheTenantAccessor.GetCurrentTenantId(), [.. invocation.Arguments]);
    }

    private static Task ProceedTaskAsync(IInvocation invocation)
    {
        invocation.Proceed();
        return invocation.ReturnValue is Task result
            ? result
            : throw new InvalidOperationException($"Expected method '{invocation.Method.Name}' to return Task.");
    }

    private static Task<TResult> ProceedTaskAsync<TResult>(IInvocation invocation)
    {
        invocation.Proceed();
        return invocation.ReturnValue is Task<TResult> result
            ? result
            : throw new InvalidOperationException($"Expected method '{invocation.Method.Name}' to return Task<{typeof(TResult).Name}>.");
    }

    private AuditWriteRecord CreateAuditRecord(InvocationPlan plan, AuditWriteStatus status, string detail)
    {
        return new AuditWriteRecord(
            plan.AuditMetadata.Module,
            plan.AuditMetadata.Resource,
            plan.AuditMetadata.Action,
            status,
            AuditRequestMethod,
            plan.RequestPath,
            Activity.Current?.TraceId.ToString() ?? idGenerator.NewId(),
            detail);
    }

    private static InvocationPlan BuildInvocationPlan(IInvocation invocation)
    {
        var audited = GetAttribute<AuditedAttribute>(invocation);
        var (module, resource, action) = ResolveAuditMetadata(audited, invocation.Method);

        return new InvocationPlan(
            GetAttribute<UnitOfWorkAttribute>(invocation),
            GetAttributes<CacheableAttribute>(invocation)
                .OrderBy(static attribute => attribute.Order)
                .ToList(),
            GetAttributes<CacheEvictAttribute>(invocation)
                .OrderBy(static attribute => attribute.Order)
                .ToList(),
            audited,
            new AuditMetadata(module, resource, action),
            $"{invocation.Method.DeclaringType?.FullName}.{invocation.Method.Name}");
    }

    private static (string Module, string Resource, string Action) ResolveAuditMetadata(
        AuditedAttribute? auditedAttribute,
        MethodInfo method)
    {
        if (!string.IsNullOrWhiteSpace(auditedAttribute?.Operation))
        {
            var parts = auditedAttribute.Operation
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return parts.Length switch
            {
                1 => ("system", parts[0], method.Name),
                2 => (parts[0], parts[1], method.Name),
                _ => (parts[0], parts[1], string.Join('.', parts.Skip(2))),
            };
        }

        return ("system", method.DeclaringType?.Name ?? "system", method.Name);
    }

    private static TAttribute? GetAttribute<TAttribute>(IInvocation invocation)
        where TAttribute : Attribute
    {
        return GetAttributes<TAttribute>(invocation).FirstOrDefault();
    }

    private static IReadOnlyList<TAttribute> GetAttributes<TAttribute>(IInvocation invocation)
        where TAttribute : Attribute
    {
        var attributes = new List<TAttribute>();
        AddAttributes(attributes, invocation.Method);
        if (invocation.MethodInvocationTarget is not null)
        {
            AddAttributes(attributes, invocation.MethodInvocationTarget);
        }

        if (invocation.Method.DeclaringType is not null)
        {
            AddAttributes(attributes, invocation.Method.DeclaringType);
        }

        if (invocation.MethodInvocationTarget?.DeclaringType is not null)
        {
            AddAttributes(attributes, invocation.MethodInvocationTarget.DeclaringType);
        }

        return attributes;
    }

    private static void AddAttributes<TAttribute>(ICollection<TAttribute> attributes, ICustomAttributeProvider provider)
        where TAttribute : Attribute
    {
        foreach (var attribute in provider.GetCustomAttributes(typeof(TAttribute), inherit: true).OfType<TAttribute>())
        {
            attributes.Add(attribute);
        }
    }

    private static CancellationToken GetCancellationToken(IInvocation invocation)
    {
        var parameters = invocation.Method.GetParameters();
        for (var index = parameters.Length - 1; index >= 0; index--)
        {
            if (parameters[index].ParameterType == typeof(CancellationToken) &&
                invocation.Arguments[index] is CancellationToken token)
            {
                return token;
            }
        }

        return CancellationToken.None;
    }

    private sealed record InvocationPlan(
        UnitOfWorkAttribute? UnitOfWork,
        IReadOnlyList<CacheableAttribute> CacheableAttributes,
        IReadOnlyList<CacheEvictAttribute> CacheEvictAttributes,
        AuditedAttribute? Audited,
        AuditMetadata AuditMetadata,
        string RequestPath);

    private sealed record AuditMetadata(string Module, string Resource, string Action);
}

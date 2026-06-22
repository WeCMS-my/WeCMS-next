using System.Reflection;
using Castle.DynamicProxy;

namespace WeCms.Aop;

public sealed class ApplicationServiceAopInterceptor : IInterceptor
{
    private readonly TransactionInterceptor transactionInterceptor;
    private readonly CacheInterceptor cacheInterceptor;

    public ApplicationServiceAopInterceptor(
        TransactionInterceptor transactionInterceptor,
        CacheInterceptor cacheInterceptor)
    {
        this.transactionInterceptor = transactionInterceptor ?? throw new ArgumentNullException(nameof(transactionInterceptor));
        this.cacheInterceptor = cacheInterceptor ?? throw new ArgumentNullException(nameof(cacheInterceptor));
    }

    public void Intercept(IInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var invocationPlan = InvocationPlan.Create(invocation);
        if (!invocationPlan.HasAopMetadata)
        {
            invocation.Proceed();
            return;
        }

        invocation.ReturnValue = invocationPlan.ReturnKind switch
        {
            InvocationReturnKind.Task => InvokeTaskAsync(invocation, invocationPlan),
            InvocationReturnKind.TaskOfT => InvokeTaskOfT(invocation, invocationPlan),
            InvocationReturnKind.ValueTask => new ValueTask(InvokeTaskAsync(invocation, invocationPlan)),
            InvocationReturnKind.ValueTaskOfT => InvokeValueTaskOfT(invocation, invocationPlan),
            _ => throw new NotSupportedException("AOP application service methods must return Task, Task<T>, ValueTask, or ValueTask<T>.")
        };
    }

    private async Task InvokeTaskAsync(IInvocation invocation, InvocationPlan invocationPlan)
    {
        await InvokeWithTransactionAsync(invocationPlan, token =>
            InvokeWithCacheEvictionAsync(invocation, invocationPlan, token), invocationPlan.CancellationToken);
    }

    private object InvokeTaskOfT(IInvocation invocation, InvocationPlan invocationPlan)
    {
        var resultType = invocationPlan.ReturnType.GetGenericArguments()[0];
        var method = typeof(ApplicationServiceAopInterceptor)
            .GetMethod(nameof(InvokeTaskOfTAsync), BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ApplicationServiceAopInterceptor), nameof(InvokeTaskOfTAsync));

        return method.MakeGenericMethod(resultType).Invoke(this, [invocation, invocationPlan])
            ?? throw new InvalidOperationException("AOP generic Task invocation did not return a task.");
    }

    private object InvokeValueTaskOfT(IInvocation invocation, InvocationPlan invocationPlan)
    {
        var resultType = invocationPlan.ReturnType.GetGenericArguments()[0];
        var method = typeof(ApplicationServiceAopInterceptor)
            .GetMethod(nameof(InvokeValueTaskOfTAsync), BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ApplicationServiceAopInterceptor), nameof(InvokeValueTaskOfTAsync));

        return method.MakeGenericMethod(resultType).Invoke(this, [invocation, invocationPlan])
            ?? throw new InvalidOperationException("AOP generic ValueTask invocation did not return a value task.");
    }

    private async Task<TResult?> InvokeTaskOfTAsync<TResult>(IInvocation invocation, InvocationPlan invocationPlan)
    {
        return await InvokeWithTransactionAsync(invocationPlan, token =>
            InvokeWithCacheAsync<TResult>(invocation, invocationPlan, token), invocationPlan.CancellationToken);
    }

    private ValueTask<TResult?> InvokeValueTaskOfTAsync<TResult>(IInvocation invocation, InvocationPlan invocationPlan)
    {
        return new ValueTask<TResult?>(InvokeTaskOfTAsync<TResult>(invocation, invocationPlan));
    }

    private Task InvokeWithTransactionAsync(
        InvocationPlan invocationPlan,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        if (invocationPlan.UnitOfWork is null)
        {
            return operation(cancellationToken);
        }

        return transactionInterceptor.InvokeAsync(operation, cancellationToken);
    }

    private Task<TResult?> InvokeWithTransactionAsync<TResult>(
        InvocationPlan invocationPlan,
        Func<CancellationToken, Task<TResult?>> operation,
        CancellationToken cancellationToken)
    {
        if (invocationPlan.UnitOfWork is null)
        {
            return operation(cancellationToken);
        }

        return transactionInterceptor.InvokeAsync(operation, cancellationToken);
    }

    private async Task InvokeWithCacheEvictionAsync(
        IInvocation invocation,
        InvocationPlan invocationPlan,
        CancellationToken cancellationToken)
    {
        await ProceedTaskAsync(invocation, invocationPlan);

        foreach (var attribute in invocationPlan.CacheEvictions)
        {
            await cacheInterceptor.InvokeEvictAsync(
                attribute,
                invocationPlan.CacheContext,
                _ => Task.CompletedTask,
                cancellationToken);
        }
    }

    private async Task<TResult?> InvokeWithCacheAsync<TResult>(
        IInvocation invocation,
        InvocationPlan invocationPlan,
        CancellationToken cancellationToken)
    {
        async Task<TResult?> InvokeAndEvictAsync(CancellationToken token)
        {
            var result = await ProceedTaskOfTAsync<TResult>(invocation, invocationPlan);
            foreach (var attribute in invocationPlan.CacheEvictions)
            {
                await cacheInterceptor.InvokeEvictAsync(
                    attribute,
                    invocationPlan.CacheContext,
                    _ => Task.CompletedTask,
                    token);
            }

            return result;
        }

        if (invocationPlan.Cacheable is null)
        {
            return await InvokeAndEvictAsync(cancellationToken);
        }

        if (invocationPlan.CacheEvictions.Count > 0)
        {
            throw new InvalidOperationException("Cacheable and CacheEvict cannot be applied to the same application service method.");
        }

        return await cacheInterceptor.InvokeCacheableAsync(
            invocationPlan.Cacheable,
            invocationPlan.CacheContext,
            InvokeAndEvictAsync,
            cancellationToken: cancellationToken);
    }

    private static async Task ProceedTaskAsync(IInvocation invocation, InvocationPlan invocationPlan)
    {
        invocation.Proceed();

        switch (invocationPlan.ReturnKind)
        {
            case InvocationReturnKind.Task:
                await (Task)invocation.ReturnValue!;
                return;
            case InvocationReturnKind.ValueTask:
                await (ValueTask)invocation.ReturnValue!;
                return;
            default:
                throw new InvalidOperationException("Expected a non-generic asynchronous application service method.");
        }
    }

    private static async Task<TResult?> ProceedTaskOfTAsync<TResult>(IInvocation invocation, InvocationPlan invocationPlan)
    {
        invocation.Proceed();

        return invocationPlan.ReturnKind switch
        {
            InvocationReturnKind.TaskOfT => await (Task<TResult?>)invocation.ReturnValue!,
            InvocationReturnKind.ValueTaskOfT => await (ValueTask<TResult?>)invocation.ReturnValue!,
            _ => throw new InvalidOperationException("Expected a generic asynchronous application service method.")
        };
    }

    private sealed record InvocationPlan(
        Type ReturnType,
        InvocationReturnKind ReturnKind,
        UnitOfWorkAttribute? UnitOfWork,
        CacheableAttribute? Cacheable,
        IReadOnlyList<CacheEvictAttribute> CacheEvictions,
        AuditedAttribute? Audited,
        CacheInvocationContext CacheContext,
        CancellationToken CancellationToken)
    {
        public bool HasAopMetadata =>
            UnitOfWork is not null
            || Cacheable is not null
            || CacheEvictions.Count > 0
            || Audited is not null;

        public static InvocationPlan Create(IInvocation invocation)
        {
            var method = invocation.MethodInvocationTarget ?? invocation.Method;
            var interfaceMethod = invocation.Method;
            var returnType = interfaceMethod.ReturnType;

            return new InvocationPlan(
                returnType,
                GetReturnKind(returnType),
                GetAttribute<UnitOfWorkAttribute>(interfaceMethod, method),
                GetAttribute<CacheableAttribute>(interfaceMethod, method),
                GetAttributes<CacheEvictAttribute>(interfaceMethod, method),
                GetAttribute<AuditedAttribute>(interfaceMethod, method),
                new CacheInvocationContext("global", invocation.Arguments.ToArray()),
                invocation.Arguments.OfType<CancellationToken>().LastOrDefault());
        }

        private static TAttribute? GetAttribute<TAttribute>(MethodInfo interfaceMethod, MethodInfo implementationMethod)
            where TAttribute : Attribute
        {
            return interfaceMethod.GetCustomAttribute<TAttribute>(inherit: true)
                ?? implementationMethod.GetCustomAttribute<TAttribute>(inherit: true)
                ?? interfaceMethod.DeclaringType?.GetCustomAttribute<TAttribute>(inherit: true)
                ?? implementationMethod.DeclaringType?.GetCustomAttribute<TAttribute>(inherit: true);
        }

        private static IReadOnlyList<TAttribute> GetAttributes<TAttribute>(MethodInfo interfaceMethod, MethodInfo implementationMethod)
            where TAttribute : Attribute
        {
            return interfaceMethod.GetCustomAttributes<TAttribute>(inherit: true)
                .Concat(implementationMethod.GetCustomAttributes<TAttribute>(inherit: true))
                .Concat(interfaceMethod.DeclaringType?.GetCustomAttributes<TAttribute>(inherit: true) ?? [])
                .Concat(implementationMethod.DeclaringType?.GetCustomAttributes<TAttribute>(inherit: true) ?? [])
                .ToArray();
        }

        private static InvocationReturnKind GetReturnKind(Type returnType)
        {
            if (returnType == typeof(Task))
            {
                return InvocationReturnKind.Task;
            }

            if (returnType == typeof(ValueTask))
            {
                return InvocationReturnKind.ValueTask;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return InvocationReturnKind.TaskOfT;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                return InvocationReturnKind.ValueTaskOfT;
            }

            return InvocationReturnKind.Unsupported;
        }
    }

    private enum InvocationReturnKind
    {
        Unsupported,
        Task,
        TaskOfT,
        ValueTask,
        ValueTaskOfT
    }
}

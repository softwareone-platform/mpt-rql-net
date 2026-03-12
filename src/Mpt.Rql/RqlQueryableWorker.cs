using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;

namespace Mpt.Rql;

internal class RqlQueryableWorker<TStorage>(IServiceProvider rootProvider) : RqlQueryableLinqWorker<TStorage, TStorage>(rootProvider), IRqlQueryable<TStorage>
{
}

/// <summary>
/// Thread-safe, pooled alternative to <see cref="RqlQueryableLinq{TStorage, TView}"/>.
/// Extends <see cref="RqlQueryableLinq{TStorage, TView}"/> and overrides scope management
/// to pool and reuse DI scopes across calls. Between calls only the stateful
/// <see cref="IResettable"/> services are reset — all resolved service instances are reused.
///
/// <para><b>Per-call cost at steady state:</b></para>
/// <list type="bullet">
///   <item>Reset 4 fields via <see cref="IResettable"/> (QueryContext, BuilderContext, RqlSettingsAccessor, ExternalServiceAccessor)</item>
///   <item>1 new RqlNode root (cheap, single small object)</item>
///   <item>1 new RqlResponse (or zero with the reusable response overload)</item>
///   <item>Zero scope creation, zero service resolution, zero dictionary lookups</item>
/// </list>
///
/// <para><b>Thread safety:</b> Fully thread-safe. Each concurrent call rents its own
/// isolated scope from a lock-free <see cref="ConcurrentBag{T}"/>.
/// The pool self-sizes to actual concurrency.</para>
/// </summary>
internal class RqlQueryableLinqWorker<TStorage, TView> : RqlQueryableLinq<TStorage, TView>, IDisposable
{
    private readonly IServiceProvider _rootProvider;
    private readonly ConcurrentBag<PooledScope> _pool = new();
    private volatile bool _disposed;

    public RqlQueryableLinqWorker(IServiceProvider rootProvider) : base(rootProvider)
    {
        _rootProvider = rootProvider;
    }

    protected override RqlResponse<TView> TransformInternal(
        IQueryable<TStorage> source,
        RqlRequest request,
        Action<IRqlSettings> configure,
        bool skipTransformStage)
    {
        var pooled = Rent();
        try
        {
            pooled.Services.ExternalServiceAccessor.SetServiceProvider(_rootProvider);
            return ExecutePipeline(source, request, configure, skipTransformStage, pooled.Services);
        }
        finally
        {
            Return(pooled);
        }
    }

    private PooledScope Rent()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_pool.TryTake(out var scope))
        {
            scope.Reset();
            return scope;
        }
        return new PooledScope(_rootProvider);
    }

    private void Return(PooledScope scope)
    {
        if (_disposed)
        {
            scope.Dispose();
            return;
        }

        _pool.Add(scope);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        while (_pool.TryTake(out var scope))
        {
            scope.Dispose();
        }
    }

    /// <summary>
    /// Holds a single DI scope with pre-resolved services and collected <see cref="IResettable"/> references.
    /// </summary>
    private sealed class PooledScope : IDisposable
    {
        private readonly IServiceScope _scope;
        private readonly IResettable[] _resettables;

        public ResolvedServices Services { get; }

        public PooledScope(IServiceProvider rootProvider)
        {
            _scope = rootProvider.CreateScope();
            Services = ResolveServices(_scope.ServiceProvider);

            var builderContext = _scope.ServiceProvider.GetRequiredService<IBuilderContext>();
            _resettables = CollectResettables(
                Services.SettingsAccessor,
                Services.ExternalServiceAccessor,
                Services.Context,
                builderContext);
        }

        public void Reset()
        {
            for (var i = 0; i < _resettables.Length; i++)
            {
                _resettables[i].Reset();
            }
        }

        public void Dispose() => _scope.Dispose();

        private static IResettable[] CollectResettables(params object[] services)
        {
            var list = new List<IResettable>(services.Length);
            foreach (var service in services)
            {
                if (service is IResettable resettable)
                    list.Add(resettable);
            }
            return list.ToArray();
        }
    }
}

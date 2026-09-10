# `first()` Ordering Function Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an RQL ordering function `first(<collection>, [<predicate>,] <path>)` that sorts entities by a value read from the first matching element of a child collection, built entirely on the library's existing filtering, path-building and graph machinery.

**Architecture:** `OrderingService` dispatches `RqlGenericGroup` order items to an internal `OrderingFunctionRegistry`; the built-in `FirstOrderingFunction` resolves the collection with the ordering path builder, builds the predicate with the **filtering** `IExpressionBuilder` in element scope (so constants, operators and permissions are reused), and emits `Where().Select().FirstOrDefault()`. `OrderingGraphBuilder` gains a hook so the projection graph includes exactly the columns the key reads (collection → predicate props → selector). No parser or Abstractions changes.

**Tech Stack:** .NET 8, C# 12, `System.Linq.Expressions`, xUnit 2.8 + Moq 4.20 + FluentAssertions 6.12 (unit), xUnit 2.7 (integration, LINQ-to-Objects).

**Spec:** `docs/superpowers/specs/2026-09-10-first-ordering-function-design.md`

## Global Constraints

- No changes under `src/Mpt.Rql.Abstractions/` and none under `src/Mpt.Rql/Parsers/`.
- `src/Mpt.Rql/Core/PathInfoBuilder.cs` is not modified (no collection pivot).
- Everything new under `src/Mpt.Rql/Services/Ordering/Functions/` is `internal`.
- Error codes follow the repo convention `order:<sub>` (as produced by `RqlService.MakeErrorCode`).
- All failures are collected `Error.Validation(...)` results; no exception may escape to the caller for malformed RQL.
- Nullable reference types enabled; match surrounding style (file-scoped namespaces, `var`, expression-bodied members where the neighbours use them).
- Commit after every task with the `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>` trailer. Never pass `--no-verify` or alter `core.hooksPath`.
- Work on branch `feature/first-ordering-function` in the current worktree. Do not push.

**Run commands (from repo root):**

```bash
dotnet build
dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~<ClassName>"
dotnet test tests/Rql.Tests.Integration --filter "FullyQualifiedName~<ClassName>"
dotnet test
```

**Existing code you will rely on (read these first, they are short):**

- `src/Mpt.Rql/Services/Ordering/OrderingService.cs` — the service you extend.
- `src/Mpt.Rql/Services/Graph/GraphBuilder.cs` and `src/Mpt.Rql/Services/Ordering/OrderingGraphBuilder.cs`.
- `src/Mpt.Rql/Services/Context/IBuilderContext.cs`, `BuilderContext.cs`.
- `src/Mpt.Rql/Services/Filtering/Builders/IExpressionBuilder.cs` (`Result<Expression> Build(ParameterExpression pe, RqlExpression node)`).
- `src/Mpt.Rql/Services/Filtering/Builders/CollectionExpressionBuilder.cs` — the pattern for descending into a collection scope.
- `src/Mpt.Rql/Core/StringHelper.cs` (`ExtractSign`), `src/Mpt.Rql/Core/Result.cs`, `src/Mpt.Rql.Abstractions/Result/Error.cs` (`Error.Validation(message, code?, path?)`).
- `src/Mpt.Rql/RqlNode.cs` (`IncludeChild`, `TryGetChild`, `IncludeReason`).
- Unit test models: `tests/Rql.Tests.Unit/Services/Models/{Product,Item,Category}.cs`. `Product.Items : List<Item>`; `Item { Id, Name (core), Description }`; `Product.Category.Products : List<Product>`.

---

### Task 1: `IBuilderContext.TryGoToChild(string)` + spec convention fixes

**Files:**
- Modify: `src/Mpt.Rql/Services/Context/IBuilderContext.cs`
- Modify: `src/Mpt.Rql/Services/Context/BuilderContext.cs`
- Modify: `docs/superpowers/specs/2026-09-10-first-ordering-function-design.md`
- Test: `tests/Rql.Tests.Unit/Services/BuilderContextTests.cs` (create)

**Interfaces:**
- Produces: `bool IBuilderContext.TryGoToChild(string name)` — descends `CurrentNode` to the child graph node with that RQL display name; returns `false` (and stays) when there is no current node or no such child.

- [ ] **Step 1: Write the failing test**

Create `tests/Rql.Tests.Unit/Services/BuilderContextTests.cs`:

```csharp
using FluentAssertions;
using Mpt.Rql;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using Xunit;

namespace Rql.Tests.Unit.Services;

public class BuilderContextTests
{
    private static (BuilderContext context, RqlNode root) MakeGraph()
    {
        // TryGetPropertyByDisplayName is an explicit interface implementation — declare as IMetadataProvider.
        IMetadataProvider metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        var root = RqlNode.MakeRoot();
        metadata.TryGetPropertyByDisplayName(typeof(Product), "category", out var category);
        metadata.TryGetPropertyByDisplayName(typeof(Category), "products", out var products);
        var categoryNode = root.IncludeChild(category!, IncludeReasons.Hierarchy);
        categoryNode.IncludeChild(products!, IncludeReasons.Hierarchy);

        var context = new BuilderContext();
        context.SetNode(root);
        return (context, root);
    }

    [Fact]
    public void TryGoToChild_ByName_DescendsAndPrefixesPaths()
    {
        var (context, _) = MakeGraph();

        context.TryGoToChild("category").Should().BeTrue();
        context.TryGoToChild("products").Should().BeTrue();

        context.GetFullPath("id").Should().Be("category.products.id");
    }

    [Fact]
    public void TryGoToChild_ByName_UnknownChild_ReturnsFalseAndStays()
    {
        var (context, root) = MakeGraph();

        context.TryGoToChild("nope").Should().BeFalse();

        context.CurrentNode.Should().BeSameAs(root);
    }

    [Fact]
    public void TryGoToChild_ByName_WithoutCurrentNode_ReturnsFalse()
    {
        var context = new BuilderContext();

        context.TryGoToChild("category").Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~BuilderContextTests"`
Expected: build error `CS1503`/`CS1501` — no overload of `TryGoToChild` takes a `string`.

- [ ] **Step 3: Implement**

In `src/Mpt.Rql/Services/Context/IBuilderContext.cs` add one member after `bool TryGoToChild(IRqlPropertyInfo rqlProperty);`:

```csharp
    bool TryGoToChild(string name);
```

In `src/Mpt.Rql/Services/Context/BuilderContext.cs` add after the existing `TryGoToChild(IRqlPropertyInfo)` method:

```csharp
    public bool TryGoToChild(string name)
    {
        if (CurrentNode?.TryGetChild(name, out var child) != true)
            return false;

        CurrentNode = child as RqlNode;
        return true;
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~BuilderContextTests"`
Expected: 3 passed.

- [ ] **Step 5: Align the spec with repo conventions**

Run from repo root (fixes error-code format everywhere in the spec):

```bash
sed -i 's/order_unknown_func/order:unknown_func/g; s/order_func_args/order:func_args/g; s/order_not_collection/order:not_collection/g; s/order_not_primitive/order:not_primitive/g; s/order_no_props/order:no_props/g' docs/superpowers/specs/2026-09-10-first-ordering-function-design.md
```

Then make two text edits in the spec (exact strings):

Replace
```
`OrderingGraphBuilder` overrides it (the `ProcessNode` overloads it needs become
`protected` on the base). When the sign-stripped, case-insensitive name is a registered
ordering function name (`first`):
```
with
```
`OrderingGraphBuilder` overrides it (the `ProcessNode` overloads it needs become
`protected` on the base). In an order string every generic group with a non-empty name
is a function call, so the override claims **every** named group (returns `true`). For a
name that is not registered it performs no graph mutation and leaves the
`order:unknown_func` error to the expression stage — the base fallback, which would treat
the arguments as root-level property names, never runs for named groups in the ordering
builder. When the sign-stripped, case-insensitive name is registered (`first`):
```

Replace
```
  against in-memory objects); every row of the error table; nullable lifting for `int`,
  `DateTime`, and already-nullable types; safe-navigation wrap present iff `Safe`; Guid
  and enum predicate values (proves reuse of the filter pipeline); dotted collection and
  selector paths; `GoToRoot` called on error paths.
```
with
```
  against in-memory objects); every row of the error table; nullable lifting for `int`
  and already-nullable types; safe-navigation wrap present iff `Safe`; dotted collection
  paths; `GoToRoot` called on error paths. The filtering `IExpressionBuilder` is mocked
  here; Guid and enum predicate values are covered by the integration tests, which run
  the real filtering pipeline.
```

- [ ] **Step 6: Commit**

```bash
git add src/Mpt.Rql/Services/Context/IBuilderContext.cs src/Mpt.Rql/Services/Context/BuilderContext.cs tests/Rql.Tests.Unit/Services/BuilderContextTests.cs docs/superpowers/specs/2026-09-10-first-ordering-function-design.md
git commit -m "Add IBuilderContext.TryGoToChild(string) for segment-wise descent

Also aligns the first() spec with the repo's order:<sub> error-code
convention and the ordering graph builder's handling of unknown functions.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 2: `CollectionValueMethods` — cached, typed `MethodInfo` capture

**Files:**
- Create: `src/Mpt.Rql/Services/Ordering/Functions/CollectionValueMethods.cs`
- Test: `tests/Rql.Tests.Unit/Ordering/CollectionValueMethodsTests.cs` (create)

**Interfaces:**
- Produces:
  - `internal interface ICollectionValueMethods { MethodInfo Where { get; } MethodInfo Select { get; } MethodInfo FirstOrDefault { get; } }`
  - `internal static class CollectionValueMethods { static ICollectionValueMethods For(Type elementType, Type resultType); }`
  - `Where` = `Enumerable.Where<TElement>(IEnumerable<TElement>, Func<TElement,bool>)`, `Select` = `Enumerable.Select<TElement,TResult>(IEnumerable<TElement>, Func<TElement,TResult>)`, `FirstOrDefault` = `Enumerable.FirstOrDefault<TResult>(IEnumerable<TResult>)`.

- [ ] **Step 1: Write the failing test**

Create `tests/Rql.Tests.Unit/Ordering/CollectionValueMethodsTests.cs`:

```csharp
using FluentAssertions;
using Mpt.Rql.Services.Ordering.Functions;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

public class CollectionValueMethodsTests
{
    [Fact]
    public void For_ReturnsClosedGenericEnumerableMethods()
    {
        var methods = CollectionValueMethods.For(typeof(string), typeof(int?));

        methods.Where.GetGenericArguments().Should().Equal(typeof(string));
        methods.Where.GetParameters().Should().HaveCount(2);
        methods.Where.GetParameters()[1].ParameterType.Should().Be(typeof(Func<string, bool>));

        methods.Select.GetGenericArguments().Should().Equal(typeof(string), typeof(int?));
        methods.Select.GetParameters()[1].ParameterType.Should().Be(typeof(Func<string, int?>));

        methods.FirstOrDefault.GetGenericArguments().Should().Equal(typeof(int?));
        methods.FirstOrDefault.GetParameters().Should().HaveCount(1);
    }

    [Fact]
    public void For_CachesOneInstancePerTypePair()
    {
        CollectionValueMethods.For(typeof(string), typeof(int))
            .Should().BeSameAs(CollectionValueMethods.For(typeof(string), typeof(int)));

        CollectionValueMethods.For(typeof(string), typeof(int))
            .Should().NotBeSameAs(CollectionValueMethods.For(typeof(string), typeof(long)));
    }

    [Fact]
    public void Methods_ComposeIntoAWorkingWhereSelectFirstOrDefaultChain()
    {
        var methods = CollectionValueMethods.For(typeof(string), typeof(int?));
        var source = Expression.Constant(new List<string> { "a", "bb", "ccc" });
        var element = Expression.Parameter(typeof(string), "e");
        var length = Expression.Property(element, nameof(string.Length));

        var where = Expression.Call(methods.Where, source,
            Expression.Lambda(Expression.GreaterThan(length, Expression.Constant(1)), element));
        var select = Expression.Call(methods.Select, where,
            Expression.Lambda(Expression.Convert(length, typeof(int?)), element));
        var first = Expression.Call(methods.FirstOrDefault, select);

        Expression.Lambda<Func<int?>>(first).Compile()().Should().Be(2);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~CollectionValueMethodsTests"`
Expected: build error — `CollectionValueMethods` does not exist.

- [ ] **Step 3: Implement**

Create `src/Mpt.Rql/Services/Ordering/Functions/CollectionValueMethods.cs`:

```csharp
using System.Collections.Concurrent;
using System.Reflection;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Closed-generic <see cref="MethodInfo"/>s for <c>Enumerable.Where</c>, <c>Enumerable.Select</c>
/// and <c>Enumerable.FirstOrDefault</c>, used to build the <c>collection.Where(..).Select(..).FirstOrDefault()</c>
/// sort key of an ordering function.
/// </summary>
internal interface ICollectionValueMethods
{
    /// <summary><c>Enumerable.Where&lt;TElement&gt;(IEnumerable&lt;TElement&gt;, Func&lt;TElement, bool&gt;)</c></summary>
    MethodInfo Where { get; }

    /// <summary><c>Enumerable.Select&lt;TElement, TResult&gt;(IEnumerable&lt;TElement&gt;, Func&lt;TElement, TResult&gt;)</c></summary>
    MethodInfo Select { get; }

    /// <summary><c>Enumerable.FirstOrDefault&lt;TResult&gt;(IEnumerable&lt;TResult&gt;)</c></summary>
    MethodInfo FirstOrDefault { get; }
}

internal static class CollectionValueMethods
{
    private static readonly ConcurrentDictionary<(Type Element, Type Result), ICollectionValueMethods> _cache = new();

    /// <summary>Returns the (cached) method set for the given element and result types.</summary>
    public static ICollectionValueMethods For(Type elementType, Type resultType)
        => _cache.GetOrAdd((elementType, resultType), static key =>
            (ICollectionValueMethods)Activator.CreateInstance(
                typeof(CollectionValueMethods<,>).MakeGenericType(key.Element, key.Result))!);
}

/// <summary>
/// Obtains the method infos by assigning method groups to typed delegates, so overload
/// resolution happens at compile time and no name-based reflection scanning is needed.
/// Mirrors <c>CollectionFunctions&lt;T&gt;</c> and <c>OrderingFunctions&lt;TItem, TKey&gt;</c>.
/// </summary>
internal sealed class CollectionValueMethods<TElement, TResult> : ICollectionValueMethods
{
    private static readonly MethodInfo _where =
        ((Func<IEnumerable<TElement>, Func<TElement, bool>, IEnumerable<TElement>>)Enumerable.Where).Method;

    private static readonly MethodInfo _select =
        ((Func<IEnumerable<TElement>, Func<TElement, TResult>, IEnumerable<TResult>>)Enumerable.Select).Method;

    private static readonly MethodInfo _firstOrDefault =
        ((Func<IEnumerable<TResult>, TResult?>)Enumerable.FirstOrDefault).Method;

    public MethodInfo Where => _where;

    public MethodInfo Select => _select;

    public MethodInfo FirstOrDefault => _firstOrDefault;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~CollectionValueMethodsTests"`
Expected: 3 passed.

- [ ] **Step 5: Commit**

```bash
git add src/Mpt.Rql/Services/Ordering/Functions/CollectionValueMethods.cs tests/Rql.Tests.Unit/Ordering/CollectionValueMethodsTests.cs
git commit -m "Add CollectionValueMethods: cached typed capture of Where/Select/FirstOrDefault

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 3: Ordering-function contract, error codes and registry

**Files:**
- Create: `src/Mpt.Rql/Services/Ordering/Functions/IOrderingFunction.cs`
- Create: `src/Mpt.Rql/Services/Ordering/Functions/OrderingFunctionContext.cs`
- Create: `src/Mpt.Rql/Services/Ordering/Functions/OrderingErrorCodes.cs`
- Create: `src/Mpt.Rql/Services/Ordering/Functions/OrderingFunctionRegistry.cs`
- Test: `tests/Rql.Tests.Unit/Ordering/OrderingFunctionRegistryTests.cs` (create)

**Interfaces:**
- Produces:
  - `internal interface IOrderingFunction { string Name { get; } Result<Expression> Build(OrderingFunctionContext context); }`
  - `internal sealed record OrderingFunctionContext(ParameterExpression Root, IReadOnlyList<RqlExpression> Arguments, IOrderingPathInfoBuilder PathBuilder, IExpressionBuilder FilterBuilder, IBuilderContext BuilderContext, IRqlSettings Settings)`
  - `internal static class OrderingErrorCodes { UnknownFunction = "order:unknown_func"; FunctionArguments = "order:func_args"; NotCollection = "order:not_collection"; NotPrimitive = "order:not_primitive"; }`
  - `internal sealed class OrderingFunctionRegistry(IEnumerable<IOrderingFunction>)` with `bool TryGet(string name, out IOrderingFunction? function)` and `bool Contains(string name)`; case-insensitive; duplicate names: last registration wins.

- [ ] **Step 1: Write the failing test**

Create `tests/Rql.Tests.Unit/Ordering/OrderingFunctionRegistryTests.cs`:

```csharp
using FluentAssertions;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Ordering.Functions;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

public class OrderingFunctionRegistryTests
{
    private sealed class StubFunction(string name) : IOrderingFunction
    {
        public string Name => name;
        public Result<Expression> Build(OrderingFunctionContext context) => throw new NotSupportedException();
    }

    [Fact]
    public void TryGet_IsCaseInsensitive()
    {
        var first = new StubFunction("first");
        var registry = new OrderingFunctionRegistry([first]);

        registry.TryGet("FIRST", out var resolved).Should().BeTrue();
        resolved.Should().BeSameAs(first);
        registry.Contains("First").Should().BeTrue();
    }

    [Fact]
    public void DuplicateName_LastRegistrationWins_AndDoesNotThrow()
    {
        var older = new StubFunction("first");
        var newer = new StubFunction("first");

        var registry = new OrderingFunctionRegistry([older, newer]);

        registry.TryGet("first", out var resolved).Should().BeTrue();
        resolved.Should().BeSameAs(newer);
    }

    [Fact]
    public void UnknownName_IsNotFound()
    {
        var registry = new OrderingFunctionRegistry([new StubFunction("first")]);

        registry.TryGet("nope", out var resolved).Should().BeFalse();
        resolved.Should().BeNull();
        registry.Contains("nope").Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~OrderingFunctionRegistryTests"`
Expected: build error — types do not exist.

- [ ] **Step 3: Implement the four files**

`src/Mpt.Rql/Services/Ordering/Functions/IOrderingFunction.cs`:

```csharp
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// An ordering function usable in RQL order strings as <c>+name(arg1,arg2,...)</c>.
/// Implementations are registered in DI as <see cref="IOrderingFunction"/> and resolved by name
/// through <see cref="OrderingFunctionRegistry"/>.
/// </summary>
internal interface IOrderingFunction
{
    /// <summary>Function name as written in the order string (matched case-insensitively).</summary>
    string Name { get; }

    /// <summary>
    /// Builds the sort-key expression for one root entity. Any problem with the arguments must be
    /// reported as validation errors in the result, never thrown.
    /// </summary>
    Result<Expression> Build(OrderingFunctionContext context);
}
```

`src/Mpt.Rql/Services/Ordering/Functions/OrderingFunctionContext.cs`:

```csharp
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering.Builders;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Everything an <see cref="IOrderingFunction"/> may use while building its key.
/// </summary>
/// <param name="Root">Parameter representing the root entity (<c>TView</c>).</param>
/// <param name="Arguments">The parsed function arguments, in order.</param>
/// <param name="PathBuilder">Ordering path builder (validates the <c>Order</c> action, applies ordering navigation).</param>
/// <param name="FilterBuilder">Filtering expression builder used for predicate arguments.</param>
/// <param name="BuilderContext">Shared builder context; drives error-path prefixes.</param>
/// <param name="Settings">Effective settings for the current request.</param>
internal sealed record OrderingFunctionContext(
    ParameterExpression Root,
    IReadOnlyList<RqlExpression> Arguments,
    IOrderingPathInfoBuilder PathBuilder,
    IExpressionBuilder FilterBuilder,
    IBuilderContext BuilderContext,
    IRqlSettings Settings);
```

`src/Mpt.Rql/Services/Ordering/Functions/OrderingErrorCodes.cs`:

```csharp
namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>Error codes raised by ordering functions. Same <c>order:&lt;sub&gt;</c> shape as <c>RqlService.MakeErrorCode</c>.</summary>
internal static class OrderingErrorCodes
{
    public const string UnknownFunction = "order:unknown_func";
    public const string FunctionArguments = "order:func_args";
    public const string NotCollection = "order:not_collection";
    public const string NotPrimitive = "order:not_primitive";
}
```

`src/Mpt.Rql/Services/Ordering/Functions/OrderingFunctionRegistry.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Resolves <see cref="IOrderingFunction"/>s by name, case-insensitively. When several registrations
/// share a name the last one wins, so a duplicate <c>AddRql()</c> call cannot break ordering.
/// </summary>
internal sealed class OrderingFunctionRegistry
{
    private readonly Dictionary<string, IOrderingFunction> _functions = new(StringComparer.OrdinalIgnoreCase);

    public OrderingFunctionRegistry(IEnumerable<IOrderingFunction> functions)
    {
        foreach (var function in functions)
            _functions[function.Name] = function;
    }

    public bool TryGet(string name, [NotNullWhen(true)] out IOrderingFunction? function)
        => _functions.TryGetValue(name, out function);

    public bool Contains(string name) => _functions.ContainsKey(name);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~OrderingFunctionRegistryTests"`
Expected: 3 passed.

- [ ] **Step 5: Commit**

```bash
git add src/Mpt.Rql/Services/Ordering/Functions/IOrderingFunction.cs src/Mpt.Rql/Services/Ordering/Functions/OrderingFunctionContext.cs src/Mpt.Rql/Services/Ordering/Functions/OrderingErrorCodes.cs src/Mpt.Rql/Services/Ordering/Functions/OrderingFunctionRegistry.cs tests/Rql.Tests.Unit/Ordering/OrderingFunctionRegistryTests.cs
git commit -m "Add internal ordering-function contract, error codes and registry

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 4: `FirstOrderingFunction`

**Files:**
- Create: `src/Mpt.Rql/Services/Ordering/Functions/FirstOrderingFunction.cs`
- Test: `tests/Rql.Tests.Unit/Ordering/FirstOrderingFunctionTests.cs` (create)

**Interfaces:**
- Consumes: `OrderingFunctionContext`, `OrderingErrorCodes`, `CollectionValueMethods.For`, `IBuilderContext.TryGoToChild(string)` / `TryGoToChild(IRqlPropertyInfo)` / `GoToRoot()` / `GetFullPath(string)`.
- Produces: `internal sealed class FirstOrderingFunction : IOrderingFunction` with `public const string FunctionName = "first"`. `Build` returns the key expression (type = selector type, value types lifted to `Nullable<T>`), or validation errors as listed in spec §6.

The unit tests mock the filtering `IExpressionBuilder` (it needs the whole DI graph to run for real); the real pipeline is exercised in Task 7.

- [ ] **Step 1: Write the failing tests**

Create `tests/Rql.Tests.Unit/Ordering/FirstOrderingFunctionTests.cs`:

```csharp
using FluentAssertions;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Parsers.Linear.Services;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering.Builders;
using Mpt.Rql.Services.Ordering;
using Mpt.Rql.Services.Ordering.Functions;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

public class FirstOrderingFunctionTests
{
    /// <summary>
    /// Builds a context for the given order string. The graph contains only a root with an
    /// <c>items</c> child (so descent into the collection scope works); the filtering builder is a
    /// mock that always yields <c>e.Name == "x"</c> against whatever element parameter it receives.
    /// </summary>
    private static (FirstOrderingFunction Function, OrderingFunctionContext Context, BuilderContext BuilderContext) Make(
        string order,
        NavigationStrategy navigation = NavigationStrategy.Default)
    {
        var validator = new Mock<IActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);

        IMetadataProvider metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));

        var root = RqlNode.MakeRoot();
        metadata.TryGetPropertyByDisplayName(typeof(Product), "items", out var items);
        root.IncludeChild(items!, IncludeReasons.Hierarchy);
        var builderContext = new BuilderContext();
        builderContext.SetNode(root);

        var settings = new RqlSettings { Ordering = { Navigation = navigation } };
        var pathBuilder = new OrderingPathInfoBuilder(validator.Object, metadata, builderContext, settings, Mock.Of<IExternalServiceAccessor>());

        var filterBuilder = new Mock<IExpressionBuilder>();
        filterBuilder
            .Setup(b => b.Build(It.IsAny<ParameterExpression>(), It.IsAny<RqlExpression>()))
            .Returns((ParameterExpression e, RqlExpression _) =>
                new Result<Expression>(Expression.Equal(Expression.Property(e, nameof(Item.Name)), Expression.Constant("x"))));

        var group = (RqlGenericGroup)new RqlParser().Parse(order);
        var context = new OrderingFunctionContext(
            Expression.Parameter(typeof(Product), "p"),
            group.Items!,
            pathBuilder,
            filterBuilder.Object,
            builderContext,
            settings);

        return (new FirstOrderingFunction(), context, builderContext);
    }

    private static Func<Product, TKey> Compile<TKey>(Expression key, OrderingFunctionContext context)
        => Expression.Lambda<Func<Product, TKey>>(key, context.Root).Compile();

    [Fact]
    public void Name_IsFirst()
        => new FirstOrderingFunction().Name.Should().Be("first");

    [Fact]
    public void Build_ThreeArguments_ReturnsSelectorOfFirstMatchingElement()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),id)");

        var result = function.Build(context);

        result.IsError.Should().BeFalse();
        var key = Compile<int?>(result.Value!, context);
        key(new Product { Items = [new Item { Id = 5, Name = "x" }, new Item { Id = 7, Name = "x" }] }).Should().Be(5);
        key(new Product { Items = [new Item { Id = 5, Name = "y" }] }).Should().BeNull();   // no match -> null
        key(new Product { Items = [] }).Should().BeNull();                                     // empty -> null
    }

    [Fact]
    public void Build_TwoArguments_UsesFirstElementWithoutPredicate()
    {
        var (function, context, _) = Make("+first(items,description)");

        var result = function.Build(context);

        result.IsError.Should().BeFalse();
        result.Value!.Type.Should().Be(typeof(string));
        var key = Compile<string?>(result.Value!, context);
        key(new Product { Items = [new Item { Description = "b" }, new Item { Description = "a" }] }).Should().Be("b");
        key(new Product { Items = [] }).Should().BeNull();
    }

    [Fact]
    public void Build_ValueTypeSelector_IsLiftedToNullable()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),id)");

        var result = function.Build(context);

        result.Value!.Type.Should().Be(typeof(int?));
    }

    [Fact]
    public void Build_DefaultNavigation_DoesNotWrapInNullCheck()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),id)");

        var result = function.Build(context);

        result.Value.Should().BeOfType<MethodCallExpression>();
    }

    [Fact]
    public void Build_SafeNavigation_ReturnsNullForNullCollection()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),id)", NavigationStrategy.Safe);

        var result = function.Build(context);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeOfType<ConditionalExpression>();
        var key = Compile<int?>(result.Value!, context);
        key(new Product { Items = null! }).Should().BeNull();
        key(new Product { Items = [new Item { Id = 3, Name = "x" }] }).Should().Be(3);
    }

    [Theory]
    [InlineData("+first(items)", 1)]
    [InlineData("+first(items,eq(name,x),id,extra)", 4)]
    public void Build_WrongArity_ReturnsFunctionArgumentsError(string order, int count)
    {
        var (function, context, _) = Make(order);

        var result = function.Build(context);

        result.IsError.Should().BeTrue();
        var error = result.Errors.Single();
        error.Code.Should().Be(OrderingErrorCodes.FunctionArguments);
        error.Message.Should().Be($"'first' requires 2 or 3 arguments: (collection, [predicate,] path). Got {count}.");
    }

    [Fact]
    public void Build_CollectionArgumentNotAPath_ReturnsFunctionArgumentsError()
    {
        var (function, context, _) = Make("+first(eq(name,x),id)");

        var result = function.Build(context);

        result.Errors.Single().Code.Should().Be(OrderingErrorCodes.FunctionArguments);
        result.Errors.Single().Message.Should().Be("'first': collection argument must be a property path.");
    }

    [Fact]
    public void Build_PathArgumentNotAPath_ReturnsFunctionArgumentsError()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),eq(id,1))");

        var result = function.Build(context);

        result.Errors.Single().Code.Should().Be(OrderingErrorCodes.FunctionArguments);
        result.Errors.Single().Message.Should().Be("'first': path argument must be a property path.");
    }

    [Fact]
    public void Build_CollectionIsNotACollection_ReturnsNotCollectionError()
    {
        var (function, context, _) = Make("+first(name,eq(name,x),id)");

        var result = function.Build(context);

        var error = result.Errors.Single();
        error.Code.Should().Be(OrderingErrorCodes.NotCollection);
        error.Message.Should().Be("'name' is not a collection property.");
        error.Path.Should().Be("name");
    }

    [Fact]
    public void Build_UnknownCollection_PropagatesPathBuilderError()
    {
        var (function, context, _) = Make("+first(nope,eq(name,x),id)");

        var result = function.Build(context);

        result.Errors.Single().Message.Should().Be("Invalid property path.");
        result.Errors.Single().Path.Should().Be("nope");
    }

    [Fact]
    public void Build_UnknownSelector_PropagatesPathBuilderErrorWithCollectionPrefix()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),nope)");

        var result = function.Build(context);

        result.Errors.Single().Message.Should().Be("Invalid property path.");
        result.Errors.Single().Path.Should().Be("items.nope");
    }

    [Theory]
    [InlineData("+first(category.products,eq(name,x),coreCategory)")] // reference
    [InlineData("+first(category.products,eq(name,x),items)")]        // collection
    public void Build_SelectorNotPrimitive_ReturnsNotPrimitiveError(string order)
    {
        var (function, context, _) = Make(order);

        var result = function.Build(context);

        var error = result.Errors.Single();
        error.Code.Should().Be(OrderingErrorCodes.NotPrimitive);
        error.Message.Should().Be("'first': path must resolve to a primitive property.");
    }

    [Fact]
    public void Build_DottedCollectionPath_Works()
    {
        var (function, context, _) = Make("+first(category.products,eq(name,x),description)");

        var result = function.Build(context);

        result.IsError.Should().BeFalse();
        var key = Compile<string?>(result.Value!, context);
        var product = new Product
        {
            Category = new Category { Products = [new Product { Name = "x", Description = "inner" }] }
        };
        key(product).Should().Be("inner");
    }

    [Theory]
    [InlineData("+first(items,eq(name,x),id)")]
    [InlineData("+first(items,eq(name,x),nope)")]
    [InlineData("+first(name,eq(name,x),id)")]
    public void Build_AlwaysReturnsBuilderContextToRoot(string order)
    {
        var (function, context, builderContext) = Make(order);

        function.Build(context);

        builderContext.CurrentNode.Should().NotBeNull();
        builderContext.CurrentNode!.Parent.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~FirstOrderingFunctionTests"`
Expected: build error — `FirstOrderingFunction` does not exist.

- [ ] **Step 3: Implement**

Create `src/Mpt.Rql/Services/Ordering/Functions/FirstOrderingFunction.cs`:

```csharp
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Built-in ordering function: <c>first(&lt;collection&gt;, [&lt;predicate&gt;,] &lt;path&gt;)</c>.
/// </summary>
/// <remarks>
/// <para>
/// Sort key = <c>collection.Where(e =&gt; predicate).Select(e =&gt; path).FirstOrDefault()</c>; the
/// <c>Where</c> is omitted in the two-argument form. Value-type selectors are lifted to
/// <see cref="Nullable{T}"/> so "no matching element" and "empty collection" both yield <c>null</c>.
/// </para>
/// <para>
/// The predicate is an ordinary RQL filter expression built by the filtering pipeline against an
/// element-scope parameter, so constant conversion, SQL parameterization, operators and
/// <c>Filter</c> permissions are all the filtering pipeline's. The collection and the selector are
/// resolved by the ordering path builder (<c>Order</c> permissions, ordering navigation).
/// </para>
/// <para>
/// Which element is "first" is provider-defined when several elements match; the sort key of an
/// entity with no match is <c>null</c>, whose placement follows the provider.
/// </para>
/// </remarks>
internal sealed class FirstOrderingFunction : IOrderingFunction
{
    public const string FunctionName = "first";

    public string Name => FunctionName;

    public Result<Expression> Build(OrderingFunctionContext context)
    {
        var args = context.Arguments;

        if (args.Count is not (2 or 3))
            return Error.Validation(
                $"'{FunctionName}' requires 2 or 3 arguments: (collection, [predicate,] path). Got {args.Count}.",
                OrderingErrorCodes.FunctionArguments);

        if (args[0] is not RqlConstant collectionArg)
            return Error.Validation($"'{FunctionName}': collection argument must be a property path.", OrderingErrorCodes.FunctionArguments);

        if (args[^1] is not RqlConstant pathArg)
            return Error.Validation($"'{FunctionName}': path argument must be a property path.", OrderingErrorCodes.FunctionArguments);

        var predicateArg = args.Count == 3 ? args[1] : null;

        var collection = context.PathBuilder.Build(context.Root, collectionArg.Value);
        if (collection.IsError)
            return collection.Errors;

        var collectionInfo = collection.Value!.PropertyInfo;
        if (collectionInfo.Type != RqlPropertyType.Collection || collectionInfo.ElementType is null)
            return Error.Validation(
                $"'{collectionArg.Value}' is not a collection property.",
                OrderingErrorCodes.NotCollection,
                context.BuilderContext.GetFullPath(collectionArg.Value));

        var elementType = collectionInfo.ElementType;
        var element = Expression.Parameter(elementType, "e");

        // Enter the collection's graph scope so predicate/selector errors are reported as "collection.prop".
        DescendInto(context.BuilderContext, collectionArg.Value, collectionInfo);
        try
        {
            Expression? predicate = null;
            if (predicateArg is not null)
            {
                var predicateResult = context.FilterBuilder.Build(element, predicateArg);
                if (predicateResult.IsError)
                    return predicateResult.Errors;
                predicate = predicateResult.Value!;
            }

            var selector = context.PathBuilder.Build(element, pathArg.Value);
            if (selector.IsError)
                return selector.Errors;

            var selectorInfo = selector.Value!.PropertyInfo;
            if ((selectorInfo.TypeOverride ?? selectorInfo.Type) != RqlPropertyType.Primitive)
                return Error.Validation(
                    $"'{FunctionName}': path must resolve to a primitive property.",
                    OrderingErrorCodes.NotPrimitive,
                    context.BuilderContext.GetFullPath(pathArg.Value));

            return BuildKey(collection.Value.Expression, elementType, element, predicate, selector.Value.Expression, context.Settings);
        }
        finally
        {
            context.BuilderContext.GoToRoot();
        }
    }

    /// <summary>
    /// Walks the builder context down the collection path segment by segment. The graph stage has
    /// already created these nodes; if any step is missing we stop and later error paths simply lack
    /// the prefix.
    /// </summary>
    private static void DescendInto(IBuilderContext builderContext, string collectionPath, RqlPropertyInfo collectionInfo)
    {
        var segments = collectionPath.Split('.');
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (!builderContext.TryGoToChild(segments[i]))
                return;
        }

        builderContext.TryGoToChild(collectionInfo);
    }

    private static Expression BuildKey(
        Expression collection,
        Type elementType,
        ParameterExpression element,
        Expression? predicate,
        Expression selector,
        IRqlSettings settings)
    {
        // Lift value types so a missing element yields null rather than default(T).
        var resultType = selector.Type;
        var selectorBody = selector;
        if (resultType.IsValueType && Nullable.GetUnderlyingType(resultType) is null)
        {
            resultType = typeof(Nullable<>).MakeGenericType(resultType);
            selectorBody = Expression.Convert(selector, resultType);
        }

        var methods = CollectionValueMethods.For(elementType, resultType);

        var source = collection;
        if (predicate is not null)
            source = Expression.Call(methods.Where, source, Expression.Lambda(predicate, element));

        var selected = Expression.Call(methods.Select, source, Expression.Lambda(selectorBody, element));
        Expression key = Expression.Call(methods.FirstOrDefault, selected);

        if (settings.Ordering.Navigation == NavigationStrategy.Safe)
        {
            key = Expression.Condition(
                Expression.Equal(collection, Expression.Constant(null, collection.Type)),
                Expression.Constant(null, resultType),
                key);
        }

        return key;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~FirstOrderingFunctionTests"`
Expected: all passed (18 test cases).

If `Build_UnknownSelector_PropagatesPathBuilderErrorWithCollectionPrefix` fails with path `nope`: the descent did not happen — check that `DescendInto` is called before the selector is built and that `Make` included the `items` node under the root.

- [ ] **Step 5: Commit**

```bash
git add src/Mpt.Rql/Services/Ordering/Functions/FirstOrderingFunction.cs tests/Rql.Tests.Unit/Ordering/FirstOrderingFunctionTests.cs
git commit -m "Add FirstOrderingFunction: first(collection, [predicate,] path) sort key

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 5: Graph builder hook for ordering functions

**Files:**
- Modify: `src/Mpt.Rql/Services/Graph/GraphBuilder.cs`
- Modify: `src/Mpt.Rql/Services/Ordering/OrderingGraphBuilder.cs`
- Modify: `tests/Rql.Tests.Unit/Services/GraphBuilderTests.cs` (constructor call only)
- Test: `tests/Rql.Tests.Unit/Ordering/OrderingGraphBuilderFunctionTests.cs` (create)

**Interfaces:**
- Consumes: `OrderingFunctionRegistry.Contains(string)`, `IFilteringGraphBuilder<TView>.TraverseRqlExpression(RqlNode?, RqlExpression?)`, `StringHelper.ExtractSign`.
- Produces:
  - `GraphBuilder<TView>`: `protected virtual bool TryTraverseFunctionGroup(RqlNode target, RqlGenericGroup group) => false;` invoked for every `RqlGenericGroup` before the existing name/items processing; `protected RqlNode? ProcessNode(RqlNode parentNode, RqlExpression constant, bool hierarchyOnly = false)` (was private).
  - `OrderingGraphBuilder<TView>` constructor: `(IMetadataProvider, IActionValidator, IBuilderContext, IFilteringGraphBuilder<TView> filteringGraphBuilder, OrderingFunctionRegistry functions)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/Rql.Tests.Unit/Ordering/OrderingGraphBuilderFunctionTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Parsers.Linear.Services;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Ordering;
using Mpt.Rql.Services.Ordering.Functions;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

public class OrderingGraphBuilderFunctionTests
{
    private readonly QueryContext<Product> _queryContext;
    private readonly OrderingGraphBuilder<Product> _ordering;
    private readonly RqlParser _parser = new();

    public OrderingGraphBuilderFunctionTests()
    {
        var validator = new Mock<IActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);

        var accessor = new ExternalServiceAccessor();
        accessor.SetServiceProvider(new ServiceCollection().BuildServiceProvider());
        _queryContext = new QueryContext<Product>(accessor);

        var metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        var builderContext = new BuilderContext();
        var filtering = new FilteringGraphBuilder<Product>(metadata, validator.Object, builderContext);
        var registry = new OrderingFunctionRegistry([new FirstOrderingFunction()]);

        _ordering = new OrderingGraphBuilder<Product>(metadata, validator.Object, builderContext, filtering, registry);
    }

    private IRqlNode Traverse(string order)
    {
        _ordering.TraverseRqlExpression(_queryContext.Graph, _parser.Parse(order));
        return _queryContext.Graph;
    }

    private static IRqlNode Child(IRqlNode node, string name)
    {
        node.TryGetChild(name, out var child).Should().BeTrue($"expected child '{name}' under '{node.GetFullPath()}'");
        return child!;
    }

    [Fact]
    public void First_WithPredicate_IncludesCollectionPredicateAndSelector_UnderTheCollection()
    {
        var root = Traverse("+first(items,eq(name,x),description)");

        var items = Child(root, "items");
        items.IncludeReason.Should().HaveFlag(IncludeReasons.Hierarchy);

        Child(items, "name").IncludeReason.Should().HaveFlag(IncludeReasons.Filter);
        Child(items, "description").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
    }

    [Fact]
    public void First_DoesNotResolveArgumentsAgainstTheRoot()
    {
        var root = Traverse("+first(items,eq(name,x),description)");

        root.TryGetChild("name", out _).Should().BeFalse();
        root.TryGetChild("description", out _).Should().BeFalse();
        root.TryGetChild("first", out _).Should().BeFalse();
    }

    [Fact]
    public void First_TwoArguments_IncludesCollectionAndSelector()
    {
        var root = Traverse("-first(items,description)");

        var items = Child(root, "items");
        items.IncludeReason.Should().HaveFlag(IncludeReasons.Hierarchy);
        Child(items, "description").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
        items.TryGetChild("name", out _).Should().BeFalse();
    }

    [Fact]
    public void First_DottedCollectionPath_IncludesEverySegmentAsHierarchy()
    {
        var root = Traverse("+first(category.products,eq(name,x),description)");

        var category = Child(root, "category");
        category.IncludeReason.Should().HaveFlag(IncludeReasons.Hierarchy);
        var products = Child(category, "products");
        products.IncludeReason.Should().HaveFlag(IncludeReasons.Hierarchy);
        Child(products, "name").IncludeReason.Should().HaveFlag(IncludeReasons.Filter);
        Child(products, "description").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
    }

    [Fact]
    public void First_CombinedWithScalarItem_IncludesBoth()
    {
        var root = Traverse("+first(items,eq(name,x),description),-id");

        Child(root, "id").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
        Child(Child(root, "items"), "description").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
    }

    [Fact]
    public void UnknownFunction_DoesNotMutateTheGraph()
    {
        var root = Traverse("+nope(items,description)");

        root.Count.Should().Be(0);
    }

    [Fact]
    public void First_UnknownCollection_DoesNotMutateTheGraph()
    {
        var root = Traverse("+first(nope,eq(name,x),description)");

        root.Count.Should().Be(0);
    }

    [Fact]
    public void PlainOrderItems_StillWork()
    {
        var root = Traverse("+name,-id");

        Child(root, "name").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
        Child(root, "id").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~OrderingGraphBuilderFunctionTests"`
Expected: build error — `OrderingGraphBuilder<Product>` has no 5-argument constructor.

- [ ] **Step 3: Add the hook to `GraphBuilder`**

In `src/Mpt.Rql/Services/Graph/GraphBuilder.cs`:

Replace the `case RqlGroup group:` block's opening

```csharp
            case RqlGroup group:
                {
                    var currentTarget = target;
                    if (group is RqlGenericGroup genericGroup)
                    {
```

with

```csharp
            case RqlGroup group:
                {
                    if (group is RqlGenericGroup functionGroup && TryTraverseFunctionGroup(target, functionGroup))
                        break;

                    var currentTarget = target;
                    if (group is RqlGenericGroup genericGroup)
                    {
```

Change the visibility of the `RqlExpression` overload of `ProcessNode`:

```csharp
    protected RqlNode? ProcessNode(RqlNode parentNode, RqlExpression constant, bool hierarchyOnly = false)
```

(it was `private`). Add the hook next to the other `protected virtual` members at the bottom of the class:

```csharp
    /// <summary>
    /// Gives derived builders a chance to interpret a named generic group as a function call
    /// (e.g. ordering's <c>first(...)</c>). Return <c>true</c> when the group has been handled; the
    /// base traversal — which treats the group's items as property paths — is then skipped.
    /// </summary>
    protected virtual bool TryTraverseFunctionGroup(RqlNode target, RqlGenericGroup group) => false;
```

- [ ] **Step 4: Override it in `OrderingGraphBuilder`**

Replace the whole content of `src/Mpt.Rql/Services/Ordering/OrderingGraphBuilder.cs` with:

```csharp
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Graph;
using Mpt.Rql.Services.Ordering.Functions;

namespace Mpt.Rql.Services.Ordering;

internal interface IOrderingGraphBuilder<TView> : IGraphBuilder<TView> { }

internal class OrderingGraphBuilder<TView> : GraphBuilder<TView>, IOrderingGraphBuilder<TView>
{
    private readonly IFilteringGraphBuilder<TView> _filteringGraphBuilder;
    private readonly OrderingFunctionRegistry _functions;

    public OrderingGraphBuilder(
        IMetadataProvider metadataProvider,
        IActionValidator actionValidator,
        IBuilderContext builderContext,
        IFilteringGraphBuilder<TView> filteringGraphBuilder,
        OrderingFunctionRegistry functions)
        : base(metadataProvider, actionValidator, builderContext)
    {
        _filteringGraphBuilder = filteringGraphBuilder;
        _functions = functions;
    }

    protected override RqlActions Action => RqlActions.Order;

    protected override RqlNode AddNodeToGraph(RqlNode parentNode, RqlPropertyInfo rqlProperty, bool sign)
        => parentNode.IncludeChild(rqlProperty, IncludeReasons.Order);

    /// <summary>
    /// In an order string every named group is a function call. Registered functions have the
    /// shape <c>name(collection, [predicate,] path)</c>: the collection path is included as
    /// hierarchy, the predicate is traversed by the filtering builder under the collection node
    /// (exactly like <c>any()</c>), and the selector is included under it with the Order reason.
    /// Unknown names are claimed too (no graph mutation) so that arguments are never resolved as
    /// root-level properties; the expression stage reports the unknown function.
    /// </summary>
    protected override bool TryTraverseFunctionGroup(RqlNode target, RqlGenericGroup group)
    {
        if (group.Name.Length == 0)
            return false;

        var (name, _) = StringHelper.ExtractSign(group.Name);
        if (!_functions.Contains(name.ToString()))
            return true;

        var args = group.Items ?? [];
        if (args.Count is not (2 or 3))
            return true;

        var collectionNode = ProcessNode(target, args[0], hierarchyOnly: true);
        if (collectionNode is null)
            return true;

        if (args.Count == 3)
            _filteringGraphBuilder.TraverseRqlExpression(collectionNode, args[1]);

        ProcessNode(collectionNode, args[^1]);
        return true;
    }
}
```

- [ ] **Step 5: Fix the existing `GraphBuilderTests` constructor call**

In `tests/Rql.Tests.Unit/Services/GraphBuilderTests.cs` the constructor currently ends with:

```csharp
        _filteringBuilder = new FilteringGraphBuilder<Product>(metadataProvider, actionValidatorMock.Object, builderContext);
        _orderingBuilder = new OrderingGraphBuilder<Product>(metadataProvider, actionValidatorMock.Object, builderContext);
```

Change the second line to:

```csharp
        _orderingBuilder = new OrderingGraphBuilder<Product>(metadataProvider, actionValidatorMock.Object, builderContext, _filteringBuilder, new OrderingFunctionRegistry([new FirstOrderingFunction()]));
```

and add `using Mpt.Rql.Services.Ordering.Functions;` to that file's usings.

Then check nothing else constructs the builder directly:

```bash
grep -rn "new OrderingGraphBuilder<" --include=*.cs src tests
```

Expected: only the two test files.

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~OrderingGraphBuilderFunctionTests|FullyQualifiedName~GraphBuilderTests"`
Expected: all passed (the pre-existing `GraphBuilderTests` must still pass unchanged).

- [ ] **Step 7: Commit**

```bash
git add src/Mpt.Rql/Services/Graph/GraphBuilder.cs src/Mpt.Rql/Services/Ordering/OrderingGraphBuilder.cs tests/Rql.Tests.Unit/Services/GraphBuilderTests.cs tests/Rql.Tests.Unit/Ordering/OrderingGraphBuilderFunctionTests.cs
git commit -m "Teach OrderingGraphBuilder to include the columns an ordering function reads

Function arguments are no longer resolved as root-level properties; the
collection is included as hierarchy, the predicate via the filtering
graph builder, and the selector with the Order reason.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 6: `OrderingService` dispatch and DI wiring

**Files:**
- Modify: `src/Mpt.Rql/Services/Ordering/OrderingService.cs`
- Modify: `src/Mpt.Rql/RqlExtensions.cs` (DI registrations around line 61-63)
- Test: `tests/Rql.Tests.Unit/Ordering/OrderingServiceFunctionTests.cs` (create)

**Interfaces:**
- Consumes: `OrderingFunctionRegistry.TryGet`, `IOrderingFunction.Build(OrderingFunctionContext)`, `OrderingErrorCodes.UnknownFunction`.
- Produces: `OrderingService<TView>` constructor `(IQueryContext<TView> context, IOrderingGraphBuilder<TView> graphBuilder, IRqlParser parser, IOrderingPathInfoBuilder pathBuilder, IExpressionBuilder filterBuilder, IBuilderContext builderContext, IRqlSettings settings, OrderingFunctionRegistry functions)`. DI: `IOrderingFunction → FirstOrderingFunction` (scoped), `OrderingFunctionRegistry` (scoped).

- [ ] **Step 1: Write the failing tests**

Create `tests/Rql.Tests.Unit/Ordering/OrderingServiceFunctionTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Parsers.Linear.Services;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Filtering.Builders;
using Mpt.Rql.Services.Ordering;
using Mpt.Rql.Services.Ordering.Functions;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

/// <summary>
/// Drives <see cref="OrderingService{TView}"/> end to end with real graph/path builders and a mocked
/// filtering builder (predicate is always <c>e.Name == "x"</c>), then applies the produced
/// transformations to in-memory data.
/// </summary>
public class OrderingServiceFunctionTests
{
    private readonly QueryContext<Product> _queryContext;
    private readonly OrderingService<Product> _service;

    public OrderingServiceFunctionTests()
    {
        var validator = new Mock<IActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);

        var accessor = new ExternalServiceAccessor();
        accessor.SetServiceProvider(new ServiceCollection().BuildServiceProvider());
        _queryContext = new QueryContext<Product>(accessor);

        var metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        var builderContext = new BuilderContext();
        var settings = new RqlSettings();
        var registry = new OrderingFunctionRegistry([new FirstOrderingFunction()]);

        var filteringGraph = new FilteringGraphBuilder<Product>(metadata, validator.Object, builderContext);
        var orderingGraph = new OrderingGraphBuilder<Product>(metadata, validator.Object, builderContext, filteringGraph, registry);
        var pathBuilder = new OrderingPathInfoBuilder(validator.Object, metadata, builderContext, settings, Mock.Of<IExternalServiceAccessor>());

        var filterBuilder = new Mock<IExpressionBuilder>();
        filterBuilder
            .Setup(b => b.Build(It.IsAny<ParameterExpression>(), It.IsAny<RqlExpression>()))
            .Returns((ParameterExpression e, RqlExpression _) =>
                new Result<Expression>(Expression.Equal(Expression.Property(e, nameof(Item.Name)), Expression.Constant("x"))));

        _service = new OrderingService<Product>(_queryContext, orderingGraph, new RqlParser(), pathBuilder, filterBuilder.Object, builderContext, settings, registry);
    }

    /// <summary>
    /// P1: matching item id 30 · P2: matching item id 10 · P3: no match (null key) · P4: no items (null key).
    /// </summary>
    private static IQueryable<Product> Data() => new List<Product>
    {
        new() { Id = 1, Items = [new Item { Id = 30, Name = "x" }] },
        new() { Id = 2, Items = [new Item { Id = 10, Name = "x" }] },
        new() { Id = 3, Items = [new Item { Id = 20, Name = "y" }] },
        new() { Id = 4, Items = [] },
    }.AsQueryable();

    private List<int> Run(string order)
    {
        _service.Process(order);
        _queryContext.HasErrors.Should().BeFalse(string.Join("; ", _queryContext.GetErrors()));
        return _queryContext.ApplyTransformations(Data()).Select(p => p.Id).ToList();
    }

    [Fact]
    public void FunctionThenScalar_OrdersByKeyAscThenTieBreaks()
        // null keys first (P3, P4 ordered by -id => 4, 3), then key 10 (P2), key 30 (P1)
        => Run("+first(items,eq(name,x),id),-id").Should().Equal(4, 3, 2, 1);

    [Fact]
    public void FunctionDescending_PutsNullKeysLast()
        => Run("-first(items,eq(name,x),id)").Should().Equal(1, 2, 3, 4);

    [Fact]
    public void NoSign_MeansAscending()
        => Run("first(items,eq(name,x),id)").Should().Equal(3, 4, 2, 1);

    [Fact]
    public void ScalarThenFunction_UsesThenBy()
        // all ids distinct, so the scalar decides; the function must still build (ThenBy path)
        => Run("-id,+first(items,eq(name,x),id)").Should().Equal(4, 3, 2, 1);

    [Fact]
    public void TwoArgumentForm_Works()
        // first item id: P1 30, P2 10, P3 20, P4 null
        => Run("+first(items,id)").Should().Equal(4, 2, 3, 1);

    [Fact]
    public void UnknownFunction_ReportsUnknownFunctionCode()
    {
        _service.Process("+nope(items,id)");

        var error = _queryContext.GetErrors().Single();
        error.Code.Should().Be(OrderingErrorCodes.UnknownFunction);
        error.Message.Should().Be("Unknown ordering function 'nope'.");
    }

    [Fact]
    public void WrongArity_ReportsFunctionArgumentsCode()
    {
        _service.Process("+first(items)");

        _queryContext.GetErrors().Single().Code.Should().Be(OrderingErrorCodes.FunctionArguments);
    }

    [Fact]
    public void UnknownSelector_ReportsFullPath()
    {
        _service.Process("+first(items,eq(name,x),nope)");

        var error = _queryContext.GetErrors().Single();
        error.Message.Should().Be("Invalid property path.");
        error.Path.Should().Be("items.nope");
    }

    [Fact]
    public void FailingFunction_DoesNotStopOtherItems()
    {
        _service.Process("+nope(items,id),-id");

        _queryContext.GetErrors().Should().ContainSingle(e => e.Code == OrderingErrorCodes.UnknownFunction);
        _queryContext.ApplyTransformations(Data()).Select(p => p.Id).Should().Equal(4, 3, 2, 1);
    }

    [Fact]
    public void PlainOrderString_StillWorks()
        => Run("-id").Should().Equal(4, 3, 2, 1);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Rql.Tests.Unit --filter "FullyQualifiedName~OrderingServiceFunctionTests"`
Expected: build error — `OrderingService<Product>` has no 8-argument constructor.

- [ ] **Step 3: Rewrite `OrderingService`**

Replace the whole content of `src/Mpt.Rql/Services/Ordering/OrderingService.cs` with:

```csharp
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering.Builders;
using Mpt.Rql.Services.Ordering.Functions;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Ordering;

internal sealed class OrderingService<TView> : RqlService, IOrderingService<TView>
{
    private readonly IQueryContext<TView> _context;
    private readonly IOrderingGraphBuilder<TView> _graphBuilder;
    private readonly IOrderingPathInfoBuilder _pathBuilder;
    private readonly IRqlParser _parser;
    private readonly IExpressionBuilder _filterBuilder;
    private readonly IBuilderContext _builderContext;
    private readonly IRqlSettings _settings;
    private readonly OrderingFunctionRegistry _functions;

    public OrderingService(
        IQueryContext<TView> context,
        IOrderingGraphBuilder<TView> graphBuilder,
        IRqlParser parser,
        IOrderingPathInfoBuilder pathBuilder,
        IExpressionBuilder filterBuilder,
        IBuilderContext builderContext,
        IRqlSettings settings,
        OrderingFunctionRegistry functions) : base()
    {
        _context = context;
        _graphBuilder = graphBuilder;
        _parser = parser;
        _pathBuilder = pathBuilder;
        _filterBuilder = filterBuilder;
        _builderContext = builderContext;
        _settings = settings;
        _functions = functions;
    }

    protected override string ErrorPrefix => "order";

    public void Process(string? order)
    {
        if (string.IsNullOrEmpty(order))
            return;

        var node = _parser.Parse(order);

        _graphBuilder.TraverseRqlExpression(_context.Graph, node);

        // A single function call (e.g. "+first(...)") parses to a named RqlGenericGroup at the root;
        // anything else parses to a group whose items are the individual order terms.
        List<RqlExpression> orderItems = node is RqlGenericGroup { Name.Length: > 0 }
            ? [node]
            : node.Items!.Where(item => item is RqlConstant or RqlGenericGroup).ToList();

        if (orderItems.Count == 0)
        {
            _context.AddError(Error.Validation("No valid ordering properties were detected", MakeErrorCode("no_props")));
            return;
        }

        var isFirst = true;
        var param = Expression.Parameter(typeof(TView));

        foreach (var item in orderItems)
        {
            var resolved = item switch
            {
                RqlConstant constant => ResolveConstantOrder(constant, param),
                RqlGenericGroup group => ResolveFunctionOrder(group, param),
                _ => null
            };

            if (resolved is null)
                continue;

            var (keyExpression, isAsc) = resolved.Value;

            var method = MakeOrderingMethod(keyExpression, isAsc, isFirst);
            var expression = Expression.Lambda(keyExpression, param);

            _context.AddTransformation(q => (IQueryable<TView>)method.Invoke(null, [q, expression])!);
            isFirst = false;
        }
    }

    private (Expression KeyExpression, bool IsAsc)? ResolveConstantOrder(RqlConstant constant, ParameterExpression param)
    {
        var (path, isAsc) = StringHelper.ExtractSign(constant.Value);

        var member = _pathBuilder.Build(param, path.ToString());
        if (member.IsError)
        {
            _context.AddErrors(member.Errors);
            return null;
        }

        return (member.Value!.Expression, isAsc);
    }

    private (Expression KeyExpression, bool IsAsc)? ResolveFunctionOrder(RqlGenericGroup group, ParameterExpression param)
    {
        var (nameMemory, isAsc) = StringHelper.ExtractSign(group.Name);
        var name = nameMemory.ToString();

        if (!_functions.TryGet(name, out var function))
        {
            _context.AddError(Error.Validation($"Unknown ordering function '{name}'.", OrderingErrorCodes.UnknownFunction));
            return null;
        }

        var context = new OrderingFunctionContext(param, group.Items ?? [], _pathBuilder, _filterBuilder, _builderContext, _settings);

        var key = function.Build(context);
        if (key.IsError)
        {
            _context.AddErrors(key.Errors);
            return null;
        }

        return (key.Value!, isAsc);
    }

    private static MethodInfo MakeOrderingMethod(Expression member, bool isAsc, bool isFirst)
    {
        var functions = (IOrderingFunctions)Activator.CreateInstance(typeof(OrderingFunctions<,>).MakeGenericType(typeof(TView), member.Type))!;

        if (isAsc)
            return isFirst ? functions.GetOrderBy() : functions.GetThenBy();
        else
            return isFirst ? functions.GetOrderByDescending() : functions.GetThenByDescending();
    }
}
```

- [ ] **Step 4: Register in DI**

In `src/Mpt.Rql/RqlExtensions.cs` add `using Mpt.Rql.Services.Ordering.Functions;` to the usings, and directly after

```csharp
        services.AddScoped(typeof(IOrderingGraphBuilder<>), typeof(OrderingGraphBuilder<>));
```

add

```csharp
        services.AddScoped<IOrderingFunction, FirstOrderingFunction>();
        services.AddScoped<OrderingFunctionRegistry>();
```

- [ ] **Step 5: Build and run the whole unit suite**

Run: `dotnet build` then `dotnet test tests/Rql.Tests.Unit`
Expected: build succeeds; all unit tests pass (new `OrderingServiceFunctionTests` 10 passed; nothing pre-existing broken).

- [ ] **Step 6: Run the existing integration suite to confirm no regression before adding new tests**

Run: `dotnet test tests/Rql.Tests.Integration`
Expected: all passed (DI resolves `OrderingService` with its new dependencies; `BasicOrderTests` unchanged).

- [ ] **Step 7: Commit**

```bash
git add src/Mpt.Rql/Services/Ordering/OrderingService.cs src/Mpt.Rql/RqlExtensions.cs tests/Rql.Tests.Unit/Ordering/OrderingServiceFunctionTests.cs
git commit -m "Dispatch ordering function calls from OrderingService and register first()

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 7: Integration tests (real DI, real filtering pipeline)

**Files:**
- Create: `tests/Rql.Tests.Integration/Core/SupportCase.cs`
- Create: `tests/Rql.Tests.Integration/Tests/Functionality/FirstOrderTests.cs`
- Create: `tests/Rql.Tests.Integration/Tests/Functionality/FirstParameterValueTests.cs`

**Interfaces:**
- Consumes: the public surface only — `RqlFactory.Make<T>(...)`, `RqlRequest`, `RqlResponse<T>`; `Product`/`ProductOrder` from `Core/Product.cs`.
- Produces: `SupportCase`, `CaseParameter`, `ParameterKind` test models (element properties other than `Name` are deliberately **not** core, so the mapping-enabled tests prove the graph fix).

- [ ] **Step 1: Add the test models**

Create `tests/Rql.Tests.Integration/Core/SupportCase.cs`:

```csharp
using Mpt.Rql;

namespace Rql.Tests.Integration.Core;

/// <summary>An entity carrying a keyed "parameter bag" — the primary use case for <c>first()</c>.</summary>
public class SupportCase
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public string Title { get; set; } = null!;

    public List<CaseParameter> Parameters { get; set; } = null!;
}

public enum ParameterKind
{
    Text = 0,
    Choice = 1,
    Number = 2,
}

/// <summary>Only <see cref="Name"/> is core: the other properties must reach the projection via the ordering graph.</summary>
public class CaseParameter
{
    [RqlProperty(IsCore = true)]
    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public Guid Key { get; set; }

    public ParameterKind Kind { get; set; }

    public int Rank { get; set; }
}
```

- [ ] **Step 2: Write `FirstOrderTests` (Product / Orders)**

Create `tests/Rql.Tests.Integration/Tests/Functionality/FirstOrderTests.cs`:

```csharp
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// <c>first(&lt;collection&gt;, [&lt;predicate&gt;,] &lt;path&gt;)</c> against Product.Orders, LINQ-to-Objects.
/// </summary>
public class FirstOrderTests
{
    private static IRqlQueryable<Product, Product> Make(NavigationStrategy navigation = NavigationStrategy.Default) =>
        RqlFactory.Make<Product>(services => { }, rql =>
        {
            // Transparent mapping skips the projection step, so inline data needs no unrelated navigations.
            rql.Settings.Mapping.Transparent = true;
            rql.Settings.Ordering.Navigation = navigation;
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        });

    /// <summary>
    /// Id=1 → Michael order id 30 · Id=2 → Michael order id 10 · Id=3 → Michael order id 20 ·
    /// Id=4 → Tony order only (no Michael match) · Id=5 → no orders.
    /// </summary>
    private static IQueryable<Product> Data() => new List<Product>
    {
        new() { Id = 1, Name = "A", Category = "X", Orders = [new ProductOrder { Id = 30, ClientName = "Michael" }] },
        new() { Id = 2, Name = "B", Category = "X", Orders = [new ProductOrder { Id = 10, ClientName = "Michael" }] },
        new() { Id = 3, Name = "C", Category = "X", Orders = [new ProductOrder { Id = 20, ClientName = "Michael" }] },
        new() { Id = 4, Name = "D", Category = "X", Orders = [new ProductOrder { Id = 99, ClientName = "Tony" }] },
        new() { Id = 5, Name = "E", Category = "X", Orders = [] },
    }.AsQueryable();

    private static List<int> Ids(RqlResponse<Product> result)
    {
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        return result.Query.Select(p => p.Id).ToList();
    }

    [Fact]
    public void Ascending_NullKeysFirst_ThenByMatchedValue()
        => Assert.Equal([4, 5, 2, 3, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(clientName,Michael),id)" })));

    [Fact]
    public void Descending_NullKeysLast()
        => Assert.Equal([1, 3, 2, 4, 5], Ids(Make().Transform(Data(), new RqlRequest { Order = "-first(orders,eq(clientName,Michael),id)" })));

    [Fact]
    public void NoSign_IsAscending()
        => Assert.Equal([4, 5, 2, 3, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "first(orders,eq(clientName,Michael),id)" })));

    [Fact]
    public void QuotedPredicateValue_Works()
        => Assert.Equal([4, 5, 2, 3, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(clientName,'Michael'),id)" })));

    [Fact]
    public void TwoArguments_UsesFirstElement()
        // first order id: 1→30, 2→10, 3→20, 4→99, 5→null
        => Assert.Equal([5, 2, 3, 1, 4], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,id)" })));

    [Fact]
    public void CompoundPredicate_Works()
        // Michael AND id>15: 1→30, 2→null, 3→20, 4→null, 5→null
        => Assert.Equal([2, 4, 5, 3, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,and(eq(clientName,Michael),gt(id,15)),id)" })));

    [Fact]
    public void InPredicate_Works()
        // Michael or Tony: 1→30, 2→10, 3→20, 4→99, 5→null
        => Assert.Equal([5, 2, 3, 1, 4], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,in(clientName,(Michael,Tony)),id)" })));

    [Fact]
    public void IntPredicateValue_StringResult()
    {
        var products = Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(id,10),clientName)" }).Query.ToList();

        // only product 2 has an order with id 10 → the single non-null key sorts last ascending
        Assert.Equal(2, products.Last().Id);
    }

    [Fact]
    public void CombinedWithScalarSort_TieBreaksNullKeys()
        => Assert.Equal([4, 5, 2, 3, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(clientName,Michael),id),+id" })));

    [Fact]
    public void ScalarThenFunction_Works()
        => Assert.Equal([5, 4, 3, 2, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "-id,+first(orders,eq(clientName,Michael),id)" })));

    [Fact]
    public void FirstMatch_NotMinOrMax_InMemorySemantics()
    {
        // In LINQ-to-Objects "first" is positional. A SQL provider gives no such guarantee — see the README caveat.
        var data = new List<Product>
        {
            new() { Id = 1, Name = "Multi", Category = "X", Orders = [new ProductOrder { Id = 99, ClientName = "Michael" }, new ProductOrder { Id = 1, ClientName = "Michael" }] },
            new() { Id = 2, Name = "Single", Category = "X", Orders = [new ProductOrder { Id = 50, ClientName = "Michael" }] },
        }.AsQueryable();

        Assert.Equal([2, 1], Ids(Make().Transform(data, new RqlRequest { Order = "+first(orders,eq(clientName,Michael),id)" })));
    }

    [Fact]
    public void NullCollection_SafeNavigation_YieldsNullKey()
    {
        var data = new List<Product> { new() { Id = 9, Name = "Z", Category = "X", Orders = null! } }.Concat(Data()).AsQueryable();

        var result = Make(NavigationStrategy.Safe).Transform(data, new RqlRequest { Order = "+first(orders,eq(clientName,Michael),id)" });

        Assert.Equal([9, 4, 5, 2, 3, 1], Ids(result));
    }

    // ── Errors ────────────────────────────────────────────────────────────────

    [Fact]
    public void UnknownFunction_IsAValidationError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+nope(orders,eq(clientName,Michael),id)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "order:unknown_func" && e.Message == "Unknown ordering function 'nope'.");
    }

    [Fact]
    public void WrongArity_IsAValidationError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(orders)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "order:func_args");
    }

    [Fact]
    public void NonCollection_IsAValidationError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(name,eq(clientName,Michael),id)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "order:not_collection" && e.Path == "name");
    }

    [Fact]
    public void UnknownPredicateProperty_ReportsPrefixedPath()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(nonExistent,x),id)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path." && e.Path == "orders.nonExistent");
    }

    [Fact]
    public void UnknownSelector_ReportsPrefixedPath()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(clientName,Michael),nonExistent)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path." && e.Path == "orders.nonExistent");
    }

    [Fact]
    public void IncompatiblePredicateValue_IsAValidationError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(id,not-a-number),clientName)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Cannot convert value"));
    }

    // ── Regression: the dot-notation collection pivot from PR #27 is not part of this feature ──

    [Fact]
    public void DottedCollectionPath_InOrder_IsStillAnError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+orders.clientName" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path.");
    }

    [Fact]
    public void DottedCollectionPath_InFilter_IsStillAnError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Filter = "eq(orders.clientName,Michael)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path.");
    }
}
```

- [ ] **Step 3: Write `FirstParameterValueTests` (SupportCase / Parameters, incl. mapping enabled)**

Create `tests/Rql.Tests.Integration/Tests/Functionality/FirstParameterValueTests.cs`:

```csharp
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// The primary use case: <c>+first(parameters,eq(name,priority),value)</c> over a keyed parameter bag.
/// Values sort lexicographically ("critical" &lt; "high" &lt; "low" &lt; "medium").
/// </summary>
public class FirstParameterValueTests
{
    private static readonly Guid PriorityKey = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid StatusKey = new("22222222-2222-2222-2222-222222222222");

    private static IRqlQueryable<SupportCase, SupportCase> MakeTransparent(NavigationStrategy navigation = NavigationStrategy.Default) =>
        RqlFactory.Make<SupportCase>(services => { }, rql =>
        {
            rql.Settings.Mapping.Transparent = true;
            rql.Settings.Ordering.Navigation = navigation;
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 5;
        });

    /// <summary>Mapping ON with Core-only selection: the sort key's columns must reach the projection via the graph.</summary>
    private static IRqlQueryable<SupportCase, SupportCase> MakeMapped() =>
        RqlFactory.Make<SupportCase>(services => { }, rql =>
        {
            rql.Settings.Mapping.Transparent = false;
            rql.Settings.Select.Implicit = RqlSelectModes.Core;
            rql.Settings.Select.Explicit = RqlSelectModes.Core;
            rql.Settings.Select.MaxDepth = 5;
        });

    /// <summary>
    /// Id=1 critical (rank -2) · Id=2 high (rank -1) + status · Id=3 low (rank 1) · Id=4 medium (rank 2) ·
    /// Id=5 no priority parameter · Id=6 empty bag.
    /// Ranks straddle zero on purpose: a missing element must sort as <c>null</c>, not as <c>default(int)</c> = 0.
    /// </summary>
    private static IQueryable<SupportCase> Data() => new List<SupportCase>
    {
        new() { Id = 1, Title = "A", Parameters = [Priority("critical", -2)] },
        new() { Id = 2, Title = "B", Parameters = [Priority("high", -1), new CaseParameter { Name = "status", Key = StatusKey, Kind = ParameterKind.Text, Value = "open" }] },
        new() { Id = 3, Title = "C", Parameters = [Priority("low", 1)] },
        new() { Id = 4, Title = "D", Parameters = [Priority("medium", 2)] },
        new() { Id = 5, Title = "E", Parameters = [new CaseParameter { Name = "status", Key = StatusKey, Kind = ParameterKind.Text, Value = "closed" }] },
        new() { Id = 6, Title = "F", Parameters = [] },
    }.AsQueryable();

    private static CaseParameter Priority(string value, int rank)
        => new() { Name = "priority", Key = PriorityKey, Kind = ParameterKind.Choice, Value = value, Rank = rank };

    private static List<int> Ids(RqlResponse<SupportCase> result)
    {
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        return result.Query.Select(c => c.Id).ToList();
    }

    [Fact]
    public void ByName_Ascending()
        => Assert.Equal([5, 6, 1, 2, 3, 4], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority),value)" })));

    [Fact]
    public void ByName_Descending()
        => Assert.Equal([4, 3, 2, 1, 5, 6], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = "-first(parameters,eq(name,priority),value)" })));

    [Fact]
    public void GuidPredicateValue_UsesTheFilterPipelineConverter()
        => Assert.Equal([5, 6, 1, 2, 3, 4], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = $"+first(parameters,eq(key,{PriorityKey}),value)" })));

    [Fact]
    public void EnumPredicateValue_UsesTheFilterPipelineConverter()
        => Assert.Equal([5, 6, 1, 2, 3, 4], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(kind,Choice),value)" })));

    [Fact]
    public void ValueTypeSelector_MissingElementSortsAsNull_NotZero()
        // rank: 1→-2, 2→-1, 3→1, 4→2, 5→null, 6→null.
        // Lifted:   null, null, -2, -1, 1, 2  → [5, 6, 1, 2, 3, 4]
        // Unlifted: -2, -1, 0, 0, 1, 2        → [1, 2, 5, 6, 3, 4]  (the bug this guards against)
        => Assert.Equal([5, 6, 1, 2, 3, 4], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority),rank)" })));

    [Fact]
    public void CombinedWithScalar_TieBreaksNullKeys()
        => Assert.Equal([6, 5, 1, 2, 3, 4], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority),value),-id" })));

    [Fact]
    public void NullBag_SafeNavigation_YieldsNullKey()
    {
        var data = new List<SupportCase> { new() { Id = 9, Title = "Z", Parameters = null! } }.Concat(Data()).AsQueryable();

        Assert.Equal([9, 5, 6, 1, 2, 3, 4], Ids(MakeTransparent(NavigationStrategy.Safe).Transform(data, new RqlRequest { Order = "+first(parameters,eq(name,priority),value)" })));
    }

    // ── Mapping enabled: the graph must carry parameters.name and parameters.value into the projection ──

    [Fact]
    public void MappingEnabled_CoreOnlySelection_StillSortsCorrectly()
    {
        var result = MakeMapped().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority),value)" });

        Assert.Equal([5, 6, 1, 2, 3, 4], Ids(result));
    }

    [Fact]
    public void MappingEnabled_ProjectsExactlyTheColumnsTheKeyReads()
    {
        var cases = MakeMapped().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority),value)" }).Query.ToList();

        var critical = cases.Single(c => c.Id == 1).Parameters.Single();
        Assert.Equal("priority", critical.Name);      // predicate column
        Assert.Equal("critical", critical.Value);     // selector column
        Assert.Equal(Guid.Empty, critical.Key);       // not requested → not projected
        Assert.Equal(0, critical.Rank);               // not requested → not projected
    }

    [Fact]
    public void MappingEnabled_ArgumentsDoNotLeakIntoTheRootProjection()
    {
        // 'value' and 'name' are also plausible root-level names; with the old design they would have been
        // pulled into the root projection. Here only Id/Title (core) and parameters (hierarchy) are projected.
        var result = MakeMapped().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority),value)" });

        Assert.True(result.IsSuccess);
        Assert.False(result.Graph.TryGetChild("value", out _));
        Assert.False(result.Graph.TryGetChild("name", out _));
        Assert.True(result.Graph.TryGetChild("parameters", out var parameters));
        Assert.True(parameters!.TryGetChild("name", out _));
        Assert.True(parameters.TryGetChild("value", out _));
    }
}
```

- [ ] **Step 4: Run the new integration tests**

Run: `dotnet test tests/Rql.Tests.Integration --filter "FullyQualifiedName~FirstOrderTests|FullyQualifiedName~FirstParameterValueTests"`
Expected: all passed (22 + 10).

Troubleshooting guide, if a specific test fails:
- `MappingEnabled_*` unsorted / `Value` null → the graph branch (Task 5) did not include `parameters.value`, or `OrderingGraphBuilder` is not the instance DI resolves; check `RqlExtensions` registrations and that `IFilteringGraphBuilder<>` is registered *before* it is needed (it is, line 59).
- `GuidPredicateValue_*` / `EnumPredicateValue_*` → confirm the predicate goes through `IExpressionBuilder` (real DI) and not a hardcoded equality.
- `*_ReportsPrefixedPath` path is `nonExistent` → `DescendInto` did not find the `orders` node; check the graph stage ran before the expression stage and that `TryGoToChild(collectionInfo)` uses the metadata display name.
- `ValueTypeSelector_*` returns `[1,2,5,6,3,4]` → the selector was not lifted to `int?` (missing elements sorted as `0`); check the `Nullable<>` lift in `FirstOrderingFunction.BuildKey`.

- [ ] **Step 5: Run everything**

Run: `dotnet test`
Expected: all unit and integration tests pass.

- [ ] **Step 6: Commit**

```bash
git add tests/Rql.Tests.Integration/Core/SupportCase.cs tests/Rql.Tests.Integration/Tests/Functionality/FirstOrderTests.cs tests/Rql.Tests.Integration/Tests/Functionality/FirstParameterValueTests.cs
git commit -m "Add integration tests for first(), including mapping-enabled projection

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 8: README documentation

**Files:**
- Modify: `README.md` — insert a subsection before the line `## Using RQL mapping`.

- [ ] **Step 1: Insert the documentation**

Insert the following block immediately before `## Using RQL mapping` in `README.md`:

````markdown
### Ordering by a collection value with `first()`

To sort by a value that lives inside a child collection — for example the `value` of the parameter whose `name` is `priority` — use the `first()` ordering function:

```
order=+first(<collection>, <predicate>, <path>)
order=+first(<collection>, <path>)              # no predicate: the first element
```

- `<collection>` — a path to a collection property of the entity (dotted paths allowed).
- `<predicate>` — any RQL filter expression, evaluated per element (`eq`, `ne`, `in`, `like`, `and`, `or`, `not`, quoted values). Element properties must permit filtering.
- `<path>` — a path to a primitive property of the element, used as the sort key. Must permit ordering.

Examples:

```
order=+first(parameters,eq(name,priority),value)
order=-first(parameters,eq(externalId,sla),displayValue)
order=+first(parameters,and(eq(name,priority),ne(value,null)),value),-id
order=+first(orders,id)
```

The sort key is `collection.Where(e => predicate).Select(e => path).FirstOrDefault()`. Entities with no matching element (or an empty collection) get a `null` key.

Caveats:

- **"First" is provider-defined** when several elements match — SQL gives no ordering inside the subquery. Use a predicate that identifies one element (a key or externalId).
- **Null placement follows the provider** (SQL Server and LINQ-to-Objects: nulls first ascending; PostgreSQL: nulls last).
- **Cost**: the key is a correlated subquery in `ORDER BY`; it cannot use an index and paging forces a full sort of the filtered set. For very large tables prefer a denormalized sort column.
- For **filtering** by a collection element use `any(collection, predicate)`, e.g. `any(parameters,and(eq(name,priority),eq(value,high)))`.

````

- [ ] **Step 2: Verify the README renders sanely**

Run: `grep -n "first()" README.md`
Expected: the new heading line is listed and appears before `## Using RQL mapping`.

- [ ] **Step 3: Final full verification**

Run: `dotnet build -warnaserror:CS8600,CS8602,CS8603,CS8604 2>&1 | tail -5` then `dotnet test`
Expected: build succeeds with no new warnings in the touched files; all tests pass.

- [ ] **Step 4: Commit**

```bash
git add README.md
git commit -m "Document the first() ordering function

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Out of scope (tracked in spec §11)

- EF Core / SQLite translation test (needs a provider dependency decision).
- `min()` / `max()` ordering functions.
- Public ordering-function extension point.
- Architect sign-off on function-in-`order`; path-shaped fallback `collection[keyProp:keyValue].valueProp`.

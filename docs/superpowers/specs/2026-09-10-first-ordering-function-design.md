# `first()` ordering function — design

**Status:** approved design, pending implementation plan
**Date:** 2026-09-10
**Supersedes:** PR #27 "Introduce orderby() ordering function for collection-based sort keys"

## 1. Context

Consumers need to sort entities by a value held inside a child collection, where the
element to read from is identified at request time — e.g. "sort orders by the `value` of
the parameter whose `name` is `priority`". Both the matching property and the value
property must be chosen by the RQL caller; they cannot be fixed in code.

PR #27 implemented this as `+orderby(collection,filterProp,filterValue,resultProp)`.
Review of that PR found structural defects that all trace to one cause: the function
received a flat list of strings and resolved paths on its own, outside the machinery
that keeps the rest of the library correct.

| Finding (PR #27) | Root cause |
|---|---|
| Sort key silently `null` whenever mapping runs (no ordering at all, `IsSuccess=true`) | Graph builder treated function arguments as root-level property names; inner collection columns never reached the projection graph, so mapping projected them away. |
| Arguments matching root fields leaked into the response projection | Same. |
| `ArgumentNullException` on a null collection even under `NavigationStrategy.Safe` | Function never consulted `Settings.Ordering.Navigation`. |
| Guid/enum filter values rejected; SQL not parameterized | Reimplemented value conversion with `Convert.ChangeType` and inlined `Expression.Constant` instead of reusing `ConstantHelper` / `ConstantBuilder`. |
| Filtering on `orders.clientName` silently changed from an error to first-element-only comparison | A collection pivot was added to the shared `PathInfoBuilder` base and therefore applied to filtering too. |
| Error paths lost their prefix (`nonExistent` instead of `orders.nonExistent`) | Descent never went through `IBuilderContext`. |

A platform architect also objected on the PR that the syntax should be path-shaped
(`order=parameters.<externalId>`). That shorthand implies a key property fixed in code,
which does not meet the requirement above, so it was rejected. Building on
`IRqlCustomPropertyResolver` (PR #28) was evaluated and rejected for the same reason.

## 2. Goals and non-goals

**Goals**

- RQL-expressible: collection, matching predicate, and value path are all in the request.
- Consistent with existing RQL vocabulary: `first(<collection>, <predicate>, <path>)` is
  the value-returning sibling of `any(<collection>, <predicate>)` / `all(...)`.
- Structurally correct: graph inclusion, error paths, permissions, safe navigation,
  constant conversion and SQL parameterization all come from existing machinery.
- Full predicate power (`eq`, `ne`, `in`, `like`, `and`/`or`, `not`, quoted values), not a
  single hardcoded equality.
- No parser changes. No changes to `Mpt.Rql.Abstractions`.

**Non-goals**

- Using `first()` inside `filter`. Filtering by a collection element is already
  expressible with `any(collection, and(eq(name,priority), eq(value,high)))`.
- A public extension point for custom ordering functions. The function abstraction is
  internal in this release; it can be made public later without a breaking change.
- Dot-notation collection pivot (`+orders.clientName`). Dropped; may return as its own
  design.
- Deterministic selection among several matching elements (`min`/`max` variants).
  Listed as a follow-up.
- An EF Core / SQL translation test. The repository has no EF dependency; listed as a
  follow-up.

## 3. Syntax and semantics

```
order=+first(<collection>, <predicate>, <path>)
order=+first(<collection>, <path>)
```

| Part | Kind | Rules |
|---|---|---|
| sign | `+` / `-` / none | On the function name, as for any order item. Default ascending. |
| `<collection>` | RQL path (constant) | Resolved from the root entity with the **ordering** path builder. Dotted paths through references are allowed (`customer.parameters`). Must resolve to a `Collection` property with a known element type and must permit `Order`. |
| `<predicate>` | RQL filter expression | Optional (3-argument form only). Built with the **filtering** expression builder against an element-scope parameter. Element properties it references must permit `Filter`. |
| `<path>` | RQL path (constant) | Resolved from the element type with the ordering path builder. Dotted paths allowed. Must resolve to a `Primitive` property and permit `Order`. |

The parser already produces the required tree with no changes: `+first(a,eq(b,c),d)`
parses to `RqlGenericGroup { Name = "+first", Items = [Constant a, RqlEqual(b,c), Constant d] }`,
and `+first(a,d)` to a two-item group. Combined order strings (`+first(...),-id`) parse
to an `RqlAnd` of items. (Verified against master.)

**Semantics.** The sort key is

```
collection.Where(e => <predicate>).Select(e => <path>).FirstOrDefault()
```

with the `Where` omitted in the two-argument form. When `<path>` is a non-nullable value
type the selector is lifted to `Nullable<T>` so that "no matching element" and "empty
collection" both yield `null` rather than `default(T)`.

**Examples**

```
order=+first(parameters,eq(name,priority),value)
order=-first(parameters,eq(externalId,sla),displayValue)
order=+first(parameters,and(eq(name,priority),ne(value,null)),value),-audit.created.at
order=+first(orders,id)                          # first order's id, no predicate
order=+first(parameters,eq(name,'high priority'),value)
```

**Documented caveats**

- *Which element is "first"* is provider-defined when several elements match. In SQL there
  is no ordering inside the subquery. Callers should use predicates that identify one
  element (a key/externalId). `min(...)` / `max(...)` variants are the future answer for
  deterministic aggregation.
- *Null placement* follows the provider: LINQ-to-Objects and SQL Server sort nulls first
  ascending; PostgreSQL sorts them last. The library does not normalize this.
- *Cost*: see **Performance** below.

**Performance**

Compared with PR #27's `orderby()`, the database does the same work; the surrounding
layers get cheaper or stop being wrong.

| Layer | `orderby()` (PR #27) | `first()` | Verdict |
|---|---|---|---|
| SQL executed | correlated scalar subquery in `ORDER BY` | identical shape for the same predicate | parity |
| Plan cache / query compilation | filter value inlined as `Expression.Constant` → distinct SQL text, EF compiled-query entry and DB plan **per distinct value**, driven by user input | predicate built by the filtering pipeline → `ConstantBuilder` emits a **parameter**; one SQL text, one plan | better (production-visible) |
| Expression construction (.NET, per request) | `typeof(Enumerable).GetMethods()` scan per build, `Activator.CreateInstance` and delegate allocations per method lookup | closed `MethodInfo`s cached per `(TElement, TResult)`; predicate costs what any `filter=eq(...)` costs | parity / slightly better; microseconds either way |
| Data fetched | arguments matching root properties leaked into the projection (extra columns / `Include`s); with mapping enabled the key was all-null (fast, wrong) | exactly the columns the key reads | better |
| Safe navigation | none (threw in memory) | `CASE WHEN collection IS NULL` wrapper, only when `Ordering.Navigation = Safe` | trivial, opt-in |

Intrinsic cost, identical in both designs: the subquery runs once per candidate row, the
sort cannot use an index on that key, and `Skip/Take` forces a full sort of the filtered
set first. With an index on the child table's `(ParentId, <key column>)` each probe is a
seek, so cost is roughly O(N·log M) probes plus an O(N·log N) sort — fine at thousands of
rows, painful at millions with paging. No expression shape avoids this; for large tables
the answer is a denormalized sort column at the schema level.

Richer predicates (`and`/`or`/`in`/`like`) make the subquery correspondingly heavier;
that is the caller's choice and a simple key match costs exactly what it did before.

Inherited behaviour (not a regression): because the predicate is a real filter it picks
up the service's `Settings.Filter.*` — e.g. a configured `Strings.Comparison` yields the
same `string.Equals(x, StringComparison)` shape, with the same EF translatability, as the
service's `eq` filters already produce. `first()` is exactly as fast and as translatable
as the consumer's filters, no more and no less.

## 4. Architecture

### Unchanged

- `Mpt.Rql.Parsers.Linear` — no changes.
- `Mpt.Rql.Abstractions` — no changes.
- `PathInfoBuilder` (master version, including `IRqlCustomPropertyResolver` support) —
  no changes. The PR #27 collection pivot is **not** carried over.
- `FilteringService`, `ProjectionService`, `MappingService` — no changes.

### Added

```
src/Mpt.Rql/Services/Ordering/Functions/
    IOrderingFunction.cs            internal interface
    OrderingFunctionContext.cs      internal record: what a function may use
    OrderingFunctionRegistry.cs     internal name -> function lookup
    FirstOrderingFunction.cs        the built-in
    CollectionValueMethods.cs       typed-delegate MethodInfo capture (from PR #27's WhereSelectMethods), cached
```

Plus targeted edits to `OrderingService`, `OrderingGraphBuilder` / `GraphBuilder`, and
`RqlExtensions` (DI).

### Not carried over from PR #27

The work branches from `master`, not from PR #27, so nothing is deleted; the following
PR #27 pieces are intentionally **not** re-created: the public `IOrderingFunction`
contract and `IOrderingFunctionProvider` / `OrderingFunctionProvider` (replaced by the
internal interface and registry above), `OrderByOrderingFunction`,
`WhereSelectMethods.cs` (its typed-delegate technique is reused inside
`CollectionValueMethods`), the `PathInfoBuilder` pivot and its `Get*Method` helpers, and
all tests for `orderby()` and the pivot (`OrderByOrderTests`, `OrderByParameterValueTests`,
`CollectionOrderTests`, `OrderByOrderingFunctionTests`,
`OrderByOrderingFunction_ParameterValueTests`, the `PathInfoBuilderTests` additions).
The `SupportCase` / `CaseParameter` test models from PR #27 are re-created and reused.

## 5. Detailed behaviour

### 5.1 `OrderingService.Process`

1. Parse the order string (unchanged).
2. `_graphBuilder.TraverseRqlExpression(_context.Graph, node)` (unchanged call; see 5.3
   for the new branch it takes).
3. Normalize items: if the root node is an `RqlGenericGroup` with a non-empty name it is
   the single item; otherwise take the root's `Items` that are `RqlConstant` or
   `RqlGenericGroup`. Empty → existing `order_no_props` error.
4. For each item:
   - `RqlConstant` → existing path behaviour (unchanged).
   - `RqlGenericGroup` → `StringHelper.ExtractSign(group.Name)`; look the name up in
     `OrderingFunctionRegistry` (case-insensitive). Unknown → `order_unknown_func`
     validation error, continue. Otherwise call `function.Build(context)` where the
     context carries the root `ParameterExpression`, the group's `Items`, and the
     services in 5.2. Errors → `_context.AddErrors`, continue. Success → key expression.
5. `MakeOrderingMethod(key, isAsc, isFirst)` → `OrderBy/ThenBy/...` (unchanged).

Cognitive-complexity extraction into `ResolveConstantOrder` / `ResolveFunctionOrder`
from PR #27 is kept.

### 5.2 `FirstOrderingFunction.Build`

Inputs via `OrderingFunctionContext`: root parameter, `IReadOnlyList<RqlExpression>` args,
`IOrderingPathInfoBuilder`, filtering `IExpressionBuilder`, `IBuilderContext`,
`IRqlSettings`.

1. **Arity.** `args.Count` must be 2 or 3 → else `order_func_args`:
   `"'first' requires 2 or 3 arguments: (collection, [predicate,] path). Got N."`
2. **Collection.** `args[0]` must be `RqlConstant` → else `order_func_args`
   `"'first': collection argument must be a property path."`.
   `pathBuilder.Build(root, path)`; errors propagate (they already carry full paths and
   Order-permission checks). `PropertyInfo.Type` must be `Collection` and `ElementType`
   non-null → else validation error `order_not_collection`
   `"'<path>' is not a collection property."`, path = `builderContext.GetFullPath(<path>)`.
3. **Scope.** Descend the builder context along the collection path **segment by
   segment** using a new internal overload `IBuilderContext.TryGoToChild(string name)`
   (the graph stage, 5.3, has already created these nodes as `Hierarchy`). This is what
   makes predicate/selector error paths read `customer.parameters.value` for a dotted
   collection path; the existing `CollectionExpressionBuilder` only descends one level
   and is not changed here. If a step fails (e.g. the graph stage rejected the path) the
   walk stops and later error paths simply lack the prefix — never an exception.
   Create `elementParam = Expression.Parameter(elementType)`.
4. **Predicate** (3-arg form). `filterBuilder.Build(elementParam, args[1])`. Errors
   propagate (paths are prefixed by the current builder-context node, so they read
   `parameters.value`, not `value`). Result is a `bool` expression.
5. **Selector.** The last argument (`args[1]` in the two-argument form, `args[2]` in the
   three-argument form) must be `RqlConstant` → else `order_func_args`
   `"'first': path argument must be a property path."`.
   `pathBuilder.Build(elementParam, path)`; errors propagate. Effective type
   (`TypeOverride ?? Type`) must be `Primitive` → else validation error
   `order_not_primitive` `"'first': path must resolve to a primitive property."` with the
   full path.
6. `builderContext.GoToRoot()` (also on every early return after step 3 — use
   try/finally).
7. **Lift.** If the selector type is a non-nullable value type, `Expression.Convert` to
   `Nullable<T>`; `resultType` is the lifted type.
8. **Chain.** Using `CollectionValueMethods.For(elementType, resultType)`:
   `source = collectionExpr`; if predicate: `source = Where(source, Lambda(pred, e))`;
   `selected = Select(source, Lambda(selector, e))`; `key = FirstOrDefault(selected)`.
9. **Safe navigation.** If `settings.Ordering.Navigation == Safe`:
   `key = collectionExpr == null ? (resultType)null : key`. (`collectionExpr` may already
   be a conditional chain for a dotted prefix; it is used as-is.)
10. Return `key`.

Constant handling inside the predicate is entirely the filtering pipeline's
(`ConstantHelper.ChangeType`, `ConstantBuilder.Build`, `WithNullSafetyIfEnabled` per
`Settings.Filter`). The function converts nothing itself.

### 5.3 Graph: `OrderingGraphBuilder`

`GraphBuilder.TraverseRqlExpression` gains a protected virtual hook invoked for
`RqlGenericGroup` before the existing name/items processing:

```csharp
protected virtual bool TryTraverseFunctionGroup(RqlNode target, RqlGenericGroup group) => false;
```

`OrderingGraphBuilder` overrides it (the `ProcessNode` overloads it needs become
`protected` on the base). When the sign-stripped, case-insensitive name is a registered
ordering function name (`first`):

1. `collectionNode = ProcessNode(target, args[0], hierarchyOnly: true)` — includes the
   collection path as `Hierarchy` (same call the `RqlCollection` case uses). If it returns
   `null` (unknown/ignored/unpermitted), return `true` (handled; the expression stage
   will report the error).
2. If 3 arguments: `_filteringGraphBuilder.TraverseRqlExpression(collectionNode, args[1])`
   — the predicate is traversed by the **filtering** graph builder, so its properties are
   included with the `Filter` reason and validated against the `Filter` action, exactly
   as `any()` does and exactly as the expression stage (5.2 step 4) will validate them.
   Using the ordering builder here would validate `Order` instead and could silently drop
   a filterable-but-not-orderable property from the graph, yielding a null column and a
   wrong sort with no error.
3. `ProcessNode(collectionNode, lastArg)` — includes the selector path under the
   collection node with the `Order` reason.
4. Return `true` so the base class does **not** process the arguments as root-level
   property names.

Filtering and projection graph builders keep the base (no-op) hook. Result: the
projection graph contains `parameters` (Hierarchy) → `name` (Filter) and `value` (Order),
so mapping materializes exactly the columns the key reads, and no root property is
spuriously included.

### 5.4 `OrderingFunctionRegistry`

`internal sealed class OrderingFunctionRegistry(IEnumerable<IOrderingFunction> functions)`
building a `Dictionary<string, IOrderingFunction>(StringComparer.OrdinalIgnoreCase)` with
**last-wins** assignment (a duplicate registration — e.g. `AddRql()` called twice — must
not throw). Exposes `TryGet(name, out function)` and `Contains(name)` (used by the graph
builder).

### 5.5 `CollectionValueMethods`

`internal interface ICollectionValueMethods { MethodInfo Where; MethodInfo Select; MethodInfo FirstOrDefault; }`
and `internal sealed class CollectionValueMethods<TElement, TResult>` obtaining the three
closed `MethodInfo`s by assigning method groups to typed delegates (compile-time overload
resolution; no reflection scanning). `static ICollectionValueMethods For(Type element, Type result)`
caches instances in a `ConcurrentDictionary<(Type, Type), ICollectionValueMethods>`.

### 5.6 DI (`RqlExtensions.AddRql`)

```csharp
services.AddScoped<IOrderingFunction, FirstOrderingFunction>();
services.AddScoped<OrderingFunctionRegistry>();
```

`OrderingService` and `OrderingGraphBuilder` take `OrderingFunctionRegistry`;
`OrderingGraphBuilder` additionally takes `IFilteringGraphBuilder<TView>` (already
registered, scoped) for predicate traversal. `FirstOrderingFunction` takes nothing;
everything it needs arrives in the context. `IBuilderContext` gains the internal
`TryGoToChild(string name)` overload used in 5.2 step 3.

## 6. Error handling

All conditions produce collected validation errors; no exceptions escape to the caller.

| Condition | Code | Message | Path |
|---|---|---|---|
| Unknown function name | `order_unknown_func` | `Unknown ordering function 'x'.` | — |
| Arity not 2 or 3 | `order_func_args` | `'first' requires 2 or 3 arguments: (collection, [predicate,] path). Got N.` | — |
| Collection/path argument not a constant | `order_func_args` | `'first': <collection or path> argument must be a property path.` | — |
| Collection path invalid / not permitted | (from path builder) | `Invalid property path.` / `Ordering is not permitted.` | full path |
| Collection path not a collection or element type unknown | `order_not_collection` | `'<p>' is not a collection property.` | full path |
| Predicate errors (unknown property, bad value, operator not allowed, filter not permitted) | (from filter builder) | existing messages | `parameters.<prop>` |
| Selector path invalid / not permitted | (from path builder) | existing messages | `parameters.<prop>` |
| Selector not primitive | `order_not_primitive` | `'first': path must resolve to a primitive property.` | `parameters.<prop>` |
| No valid order items at all | `order_no_props` (existing) | existing | — |

## 7. Settings interaction

- `Settings.Ordering.Navigation`: `Safe` wraps the key in a null check on the collection
  (and the path builder already guards dotted prefixes). `Default` emits no guard —
  consistent with the rest of the library ("provider decides").
- `Settings.Filter.*` (`Navigation`, `Strings.Comparison`, allowed operators) apply to the
  predicate because it is built by the filtering pipeline.
- `Settings.Select.*` / `Mapping.Transparent`: the graph branch (5.3) includes the columns
  the key reads, so the feature works with mapping enabled regardless of `Select.Implicit`
  / `Explicit` modes.

## 8. Testing

### Unit (`tests/Rql.Tests.Unit`)

- `FirstOrderingFunctionTests`: 2- and 3-arg happy paths (compile and evaluate the key
  against in-memory objects); every row of the error table; nullable lifting for `int`,
  `DateTime`, and already-nullable types; safe-navigation wrap present iff `Safe`; Guid
  and enum predicate values (proves reuse of the filter pipeline); dotted collection and
  selector paths; `GoToRoot` called on error paths.
- `OrderingGraphBuilderTests`: graph contains collection (Hierarchy), predicate properties
  and selector (Order) under the collection node; **no** root-level node for any argument;
  2-arg form; unknown collection produces no graph mutation.
- `OrderingServiceTests` additions: unknown function code; sign handling `+`/`-`/none;
  `first()` combined with scalar item in both orders (`OrderBy` then `ThenBy`);
  duplicate registration does not throw.
- `CollectionValueMethodsTests`: correct closed generics; cache returns same instance.

### Integration (`tests/Rql.Tests.Integration`, LINQ-to-Objects as today)

- `FirstOrderTests` (Product/Orders) and `FirstParameterValueTests` (SupportCase/Parameters):
  exact-order assertions ascending/descending, null keys for no-match and empty
  collection, first-match (not min/max) pinned as *in-memory* behaviour with a comment,
  compound predicate, quoted value, `in` predicate, combination with scalar sort, all
  error cases.
- **Mapping enabled**: the same sorts with `Mapping.Transparent = false` and
  `Select.Implicit = Select.Explicit = Core` on models whose element properties are *not*
  core — asserts correct order and that the projected elements carry the columns.
- Null collection with `Ordering.Navigation = Safe` → no exception, null key; with
  `Default` → documented provider behaviour (not asserted).
- Regression: `+orders.clientName` and `eq(orders.clientName,x)` are validation errors
  (pivot removed).

### Not in scope

EF Core translation test — follow-up (needs a provider dependency decision).

## 9. Documentation

README: new subsection under *Usage* — "Ordering by a collection value with `first()`":
syntax, the two-argument form, three examples, the determinism and null-placement
caveats, and a pointer to `any()` for filtering. XML docs on the internal types.

## 10. Delivery

- Branch `feature/first-ordering-function` from `master` (`3b5312d`).
- New PR referencing #27 and the architect's comment, explaining why the function form
  was kept (caller-chosen key property) and framing `first` as the sibling of `any`/`all`.
  Closing #27 and pushing happen only on explicit go-ahead.
- Commits: spec; registry + methods + function (TDD); graph branch; service wiring; test
  removals; integration tests; README.

## 11. Follow-ups (not in this PR)

1. Architect sign-off on a function inside `order`; if refused, the path-shaped
   alternative `collection[keyProp:keyValue].valueProp` was evaluated and is the fallback.
2. `min()` / `max()` ordering functions for deterministic aggregation.
3. EF Core (SQLite) translation test once a provider dependency is agreed.
4. Public ordering-function extension point, if a consumer needs it.

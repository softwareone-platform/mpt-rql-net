# Filter optimization

`Filter.Optimization` controls optional full-filter pushdown.
By default, scalar predicates remain on the mapped view.
`RemoveRedundantNullChecks` enables full-filter pushdown when both navigation
strategies are `Default`.

When collection scopes are registered, Any/All predicates and their associated
collection guards are normalized through mapping projections independently
of this setting. Applicable scope predicates are retained, while unrelated
scalar predicates remain on the mapped view unless full-filter pushdown
is enabled.

Without collection scopes, the new collection normalizer is not applied.
Registering scopes alone does not enable full-filter pushdown.

See [Collection scopes](collection-scopes.md), including
[null handling](collection-scopes.md#null-handling).

## Without optimization (default)

Scalar predicates are applied to the projected view, after the mapping Select.

```sql
WHERE CASE WHEN [c].[Id] IS NOT NULL THEN [p].[Email] ELSE NULL END = @__Value_0
```

## With `RemoveRedundantNullChecks`

Set `Filter.Optimization` to `RemoveRedundantNullChecks` to apply the filter to the source instead, before the `Select`. RQL copies the projection into the filter and removes the null checks that make no difference when a member of a missing reference reads as null, as it does in a database query:

```sql
WHERE [p].[Email] = @__Value_0
```

```csharp
services.AddRql(options => options.Settings.Filter.Optimization = FilterOptimization.RemoveRedundantNullChecks);    // globally
rql.Transform(source, request, options => options.Settings.Filter.Optimization = FilterOptimization.RemoveRedundantNullChecks);    // per call
```

Null checks that do make a difference, such as the one around a computed member like `Phone ?? "none"`, are kept, so the filter selects the same rows as without the optimization. Each part of a filter is handled on its own: in an `and(...)` or `or(...)`, the parts that read a member directly become plain column predicates, and the others keep their null checks.

## When to use it

Collection scopes do not, by themselves, enable filter pushdown.

When redundant-null-check optimization is disabled, scalar predicates remain
on the mapped view. If collection scopes are registered, Any/All predicates
and their associated collection guards are normalized through the mapping
projection. Without scopes, collection filters remain on the projected view.

Full-filter pushdown is performed only when RemoveRedundantNullChecks is
enabled and both navigation strategies are Default.

To obtain plain column predicates through nullable references using
`RemoveRedundantNullChecks`, keep both navigation strategies at `Default`
and declare nested view references as nullable.
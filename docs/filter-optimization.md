# Filter optimization

When RQL maps the source to a view, `Filter.Optimization` decides how the filter is applied: as it is, to the projected view (the default), or with `RemoveRedundantNullChecks`, to the source, before the projection, without the null checks that make no difference.

The examples below filter orders by the email of the customer's contact person, `eq(customer.contact.email,ann@x.io)`, where both the customer and the contact are optional.

## Without optimization (default)

The filter is applied to the projected view, after the mapping `Select`. The projection puts a null check around every nullable nested reference (`x != null ? new T { ... } : null`). A provider that translates the query, such as EF Core, can keep that check around the compared column, which stops the database from using an index on it:

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

Use it with providers that translate the query, not with in-memory sources, where reading a member of a missing reference throws.

It applies with the default navigation only. With `Filter.Navigation` or `Mapping.Navigation` set to `Safe`, every member read gets its own null check (`a == null ? null : a.b`), which the optimization does not remove, so RQL keeps the filter on the projected view, exactly as without the optimization. To get the plain column predicate, keep both at `Default` and declare nested view references as nullable (for example `ContactView? Contact`), so the projection checks only the references.

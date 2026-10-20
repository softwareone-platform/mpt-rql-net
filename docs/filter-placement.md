# Filter placement

When RQL maps the source to a view, `Filter.Placement` decides where the filter goes: on the projected view (the default) or on the source, before the projection.

The examples below filter orders by the email of the customer's contact person, `eq(customer.contact.email,ann@x.io)`, where both the customer and the contact are optional.

## After the projection (default)

The filter is applied to the projected view, after the mapping `Select`. The projection puts a null check around every nullable nested reference (`x != null ? new T { ... } : null`). A provider that translates the query, such as EF Core, can keep that check around the compared column, which stops the database from using an index on it:

```sql
WHERE CASE WHEN [c].[Id] IS NOT NULL THEN [p].[Email] ELSE NULL END = @__Value_0
```

## Before the projection

Set `Filter.Placement` to `BeforeProjection` to apply the filter to the source instead, before the `Select`. RQL copies the projection into the filter and leaves out the null checks that make no difference when a member of a missing reference reads as null, as it does in a database query:

```sql
WHERE [p].[Email] = @__Value_0
```

```csharp
services.AddRql(options => options.Settings.Filter.Placement = FilterPlacement.BeforeProjection);    // globally
rql.Transform(source, request, options => options.Settings.Filter.Placement = FilterPlacement.BeforeProjection);    // per call
```

Null checks that do make a difference, such as the one around a computed member like `Phone ?? "none"`, are kept, so the filter selects the same rows as with the default placement. Each part of a filter is handled on its own: in an `and(...)` or `or(...)`, the parts that read a member directly become plain column predicates, and the others keep their null checks.

## When to use it

Use it with providers that translate the query, not with in-memory sources, where reading a member of a missing reference throws.

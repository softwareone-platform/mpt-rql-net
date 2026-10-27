# Collection scopes

`ScopeCollection` restricts the elements of a source collection for one
`Transform` call. RQL filters evaluate the restricted collection, and mapping
uses the same restriction when projecting its elements.

This is different from trimming a collection after materialization: an element
excluded by the scope cannot make an `any(...)` filter match its parent.

## Basic usage

The collection selector refers to the storage model, not the view model.
The RQL request uses view property names.

The following example assumes a mapping from `DbContact` to `ContactView`,
with `Directories` mapped directly from `DbContact.Directories`:

```csharp
var accountId = securityContext.AccountId;

var request = new RqlRequest
{
    Filter = "any(directories,eq(name,\"a\"))",
    Order = "id"
};

var response = rql.Transform(dbContext.Contacts, request, options =>
{
    options.ScopeCollection<DbContact, DbDirectory>(
        contact => contact.Directories,
        directory => directory.Account.Id == accountId);
});
```

If a contact contains directory `"a"` only under another account, the contact
does not match this filter. Returned directory collections contain only
elements allowed by the scope.

Check `response.IsSuccess` before executing `response.Query`. Unsupported
mapping expressions can throw during `Transform`; provider translation errors
can occur when the query is executed.

Scopes do not fetch identifiers or execute the query when registered.
Predicates remain expression trees and must be translatable by the LINQ
provider.

## Parent visibility is a separate rule

A collection scope does not, by itself, remove a parent whose restricted
collection is empty.

If a contact must have at least one directory belonging to the current account,
restrict the source separately:

```csharp
var source = dbContext.Contacts.Where(contact =>
    contact.Directories.Any(directory =>
        directory.Account.Id == accountId));

var response = rql.Transform(source, request, options =>
{
    options.ScopeCollection<DbContact, DbDirectory>(
        contact => contact.Directories,
        directory => directory.Account.Id == accountId);
});
```

The source restriction controls which contacts are accessible. The collection
scope controls which directory elements participate in the supported mapped
query.

Apply pagination and count operations to `response.Query`, after RQL
transformation.

## Combining scopes

Multiple scopes for the same path are combined with AND semantics:

```csharp
options.ScopeCollection<DbContact, DbDirectory>(
    contact => contact.Directories,
    directory => directory.Account.Id == accountId);

options.ScopeCollection<DbContact, DbDirectory>(
    contact => contact.Directories,
    directory => directory.IsActive);
```

This is equivalent to restricting the collection by both predicates.
Later registrations do not replace earlier ones.

Scopes belong to the current transformation. They are not stored in the shared
mapping configuration or reused by subsequent calls.

## Supported paths and mappings

The selector must be a member path rooted in the storage parameter:

```csharp
contact => contact.Directories
contact => contact.Account.Directories
```

### Type validation

`TElement` must exactly match the selector's actual collection element type.
The selected collection type must expose a single distinct `IEnumerable<T>`
element type. Specifying a base element type through covariance is not
supported, even if the registration compiles.

For example, a selector returning `ICollection<DbDirectory>` requires
`TElement = DbDirectory`, not a base class of `DbDirectory`.

An element-type mismatch throws `ArgumentException` from `ScopeCollection`
before the scope is registered.

`TStorage` must exactly match the query's declared storage type.
Both `Transform` and `BuildGraph` validate this after the configuration callback,
before graph construction or mapping. A mismatch throws `ArgumentException`
rather than returning an error in the response.

These checks validate scope registration types only. They do not guarantee
that mapping expressions or the resulting query can be translated by the
LINQ provider.

Computed selectors, such as `contact.Directories.Where(...)`, are not supported.
Put the restriction in the predicate argument instead.

Scope matching is path-specific. Restricting `contact.Directories` does not
restrict another collection property, even if both properties happen to
reference the same collection instance.

Supported mapping sources include:

- A directly mapped collection.
- A mapping factory returning that direct collection.
- A direct collection returned by a factory using `ExpressionFactoryHint.TakeFirst`;
  the restriction is applied before selecting the first element.
- A collection reached through dynamically mapped containing references.

Collection scopes do not broaden the existing collection classification or
dynamic-mapping target support. Use `List<T>` for dynamically mapped collection
properties. The existing `System.Collections.IList` compatibility check remains
in place; satisfying that check alone does not guarantee that an arbitrary
collection type can be mapped.

Primitive arrays such as `byte[]`, `string[]`, and `int[]` retain their existing
`Primitive` classification. This feature does not introduce array collection
mapping.

Dynamically mapped primitive lists without an applied scope retain their
existing pass-through behavior. When a scope applies, the restricted collection
is materialized for the result.

Transparent mapping is not supported when collection scopes are registered.

## Unsupported uses in mapping

A mapping expression that uses a scoped collection indirectly is rejected
instead of silently ignoring the scope. Examples include:

```csharp
contact => contact.Directories.OrderBy(directory => directory.Id)
contact => contact.Directories.Where(directory => directory.IsActive)
contact => contact.Directories.Count
contact => contact.Directories.Count()
contact => contact.Directories.Any()
```

The restriction also applies to expressions supplied by mapping factories and
to conditional mapping predicates.

Returning a containing object through a static mapping is rejected when it
would bypass a scope on a collection inside that object. Use dynamic mapping
so RQL can process the collection.

Validation checks individual uses. A correctly scoped `Directories` property
does not make a separate unscoped `DirectoryCount` mapping acceptable.

Expressions using unrelated source paths remain unaffected.

These restrictions concern mapping expressions, not normal RQL collection
filters. For example, `any(directories,...)` remains supported, and
`response.Query.CountAsync()` can count the filtered parents.

## Null handling

The rules below describe conditional collection normalization when collection
scopes are registered. They do not establish new default semantics for queries
without scopes.

Any/All predicates over conditional collection projections are rewritten
into the corresponding conditional predicates. A null branch of the projection
evaluates to false; an existing empty collection retains normal Any/All
semantics.

Conditional projections can arise from nullable containing references,
conditional collection mappings, or safe mapping navigation. Their collection
predicates are supported with `Filter.Navigation = Default`; using
`Mapping.Navigation = Safe` does not require `Filter.Navigation = Safe`.

| Predicate | Null branch of a conditional projection | Existing empty collection |
|---|---:|---:|
| `any(collection,predicate)` | false | false |
| `all(collection,predicate)` | false | true |
| `not(any(collection,predicate))` | true | true |

An existing collection that becomes empty after applying a scope retains
the same empty-collection semantics.

This does not introduce a general null guard for directly mapped collections.
RQL does not automatically change the filtering strategy or convert null
collections into empty collections. With default filtering navigation,
unguarded LINQ-to-Objects `Any` and `All` over a directly mapped null collection
still throw.

## Filter rewriting and optimization

Registering a collection scope does not push the whole filter beneath the
mapping projection. When scopes are registered, Any/All predicates and their
associated collection guards are normalized through collection projections,
retaining applicable scope predicates. Unrelated scalar predicates remain
on the mapped view unless `RemoveRedundantNullChecks` is enabled and both
navigation strategies are `Default`.

Without collection scopes, the new collection normalizer is not applied.
Explicit full-filter optimization continues to use the pushdown visitor.

For example:

```text
source.Where(scope).Select(mapping).ToList().Any(viewPredicate)
```

can become:

```text
source.Where(scope).Any(sourcePredicate)
```

where `sourcePredicate` is obtained by substituting the mapping into the view
predicate.

This normalization is separate from
`FilterOptimization.RemoveRedundantNullChecks`. With safe navigation, the
optional removal of redundant reference null checks remains disabled.

Collection projection and materialization are retained for the result.
No `AsEnumerable()` or early query materialization is required.

## Boundaries and security

Scopes are not EF global query filters. They do not automatically rewrite:

- Arbitrary queries executed outside this transformation.
- Source predicates or callbacks supplied by application code.
- Hidden accesses inside custom methods or resolvers.
- Alternative navigation paths to the same underlying data.

The validator detects unsupported accesses visible in the mapping expression
tree; it is not a general analysis of application code.

Do not treat successful `BuildGraph` execution as validation of mapping or SQL
translation: these require building and executing the actual query.

When scopes enforce access restrictions, test the real mappings and database
provider. Cover list reads, single-item reads, counts, pagination and streaming,
including every query used to load stream batches.
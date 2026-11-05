# Computed properties

A view property whose getter computes its value from other properties of the same view has no source of its own: the mapping never binds it, and the getter reads whatever the projection bound to its inputs. When a request selects the computed property on its own, its inputs are not part of the projection, so the getter has nothing to read and the property comes back empty.

`RqlPropertyAttribute.DependsOn` declares the properties a computed property reads, so that RQL projects them whenever it projects the computed property, and only then.

```csharp
public class Parameter
{
    public string? ExternalId { get; set; }

    [RqlProperty(Actions = RqlActions.None)]
    public string? RawValue { get; set; }

    [RqlProperty(Actions = RqlActions.None)]
    public string? Format { get; set; }

    [RqlProperty(Actions = RqlActions.Select, DependsOn = new[] { nameof(RawValue), nameof(Format) })]
    public object? Value => ParameterValue.Parse(RawValue, Format);
}
```

## What changes

- `select=value` projects `RawValue` and `Format` along with `Value`, so `Value` has its inputs. `select=externalId` reads neither.
- A dependency is projected whatever its own select settings and actions say, including when the select mode leaves it out, when the request deselects it with `-format`, when its actions do not allow selecting it, and when `SetVisibility` hides it for the call. To keep the inputs out, hide or deselect the computed property itself, unless it is `Forced` or a dependency of another projected property, which keeps it projected. Selecting the dependency directly is still validated against its own actions, so `select=rawValue` above leaves it out.
- Dependencies are expanded transitively: a dependency that declares dependencies of its own brings them in as well. A cycle is harmless.
- A node added for this reason alone carries the `Dependency` include reason and no other, which `Graph.Print()` shows. It is part of the projection (`IsIncluded` is true), so a response writer that shows only selected and default properties leaves it out.
- Filtering and ordering are not affected: they follow the property's own actions. A computed property has no source to translate, so it should not allow either.

## Rules

`DependsOn` takes the CLR names of properties declared on, or inherited by, the same type. The metadata of the type is rejected with `RqlInvalidDependencyException` when it is first built, which is the first time a request uses the type, if:

- the declaring property is `RqlPropertyMode.Ignored`, which is excluded from projection by its contract;
- a name is the declaring property itself, or is not a property of the type;
- a name is an ignored property, which the mapping never binds;
- a name is a reference or collection property, as a dependency is projected without the defaults beneath it.

A dependency is bound by the mapping like any other property, so it needs a source: a storage property of the same name, or an explicit mapping. A dependency without one is projected but stays at its default value.

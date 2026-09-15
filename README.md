# Mpt.Rql - Resource Query Language implementation for .NET

[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=softwareone-platform_mpt-rql-net&metric=alert_status)](https://github.com/softwareone/mpt-rql-net/actions)
[![NuGet](https://img.shields.io/nuget/v/Mpt.Rql)](https://www.nuget.org/packages/Mpt.Rql)
[![License](https://img.shields.io/github/license/softwareone-platform/mpt-rql-net)](LICENSE)

## Overview

Mpt.Rql is a high-performance implementation of [Resource Query Language (RQL)](https://docs.platform.softwareone.com/developer-resources/rest-api/resource-query-language) for .NET applications. It enables API consumers to efficiently filter, sort, and paginate data with an intuitive and expressive syntax, delivering precise data retrieval capabilities.

## Features

- **Filtering** - Support for complex logical expressions
- **Sorting** - Multiple fields with ascending/descending options
- **Projections** - Return only required fields to minimize payload size
- **LINQ Integration** - Seamless integration with Entity Framework and other LINQ providers
- **Validation** - Comprehensive error handling and input validation
- **Performance Optimized** - Efficient query processing for minimal overhead

## Usage

### Installing Required Packages

```bash
dotnet add package Mpt.Rql
```

### Registering RQL Services

```csharp
// In Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register RQL services
    services.AddRql(options =>
    {
        // Configure RQL options here
    });
    
    // Other service registrations...
}
```

### Applying RQL to IQueryable<>
```csharp
public class UserQueryBuilder(IRqlQueryable<User> rql)
{
    public IQueryable<User> GetUsersOlderThan(IQueryable<User> sourceQuery, int age)
    {
        var request = new RqlRequest
        {
            Filter = $"gt(age,{age})",              // Age must be greater than age specified
            Order = "-age",                         // Order by age desc
            Select = "id,name"                      // Select id and name
        };

        var response = rql.Transform(sourceQuery, request);

        if (response.IsSuccess)
            return response.Query;                  // Return transformed query

        response.Errors.ForEach(t => { });          // Iterate through transformation errors (optional)

        Console.WriteLine(response.Graph.Print())   // Visualize the decision graph (optional)
    }
}
```

### Ordering by a collection value with `first()`

To sort by a value that lives inside a child collection — for example the `value` of the parameter whose `name` is `priority` — use the `first()` ordering function:

```
order=+first(<collection>,<predicate>,<path>)
order=+first(<collection>,<path>)              # no predicate: the first element
```

RQL does not allow whitespace between arguments.

- `<collection>` — a path to a collection property of the entity (dotted paths allowed). Must permit ordering. With mapping enabled the property must be a list type (`List<T>`, `IList`, arrays).
- `<predicate>` — any RQL filter expression, evaluated per element (`eq`, `ne`, `in`, `like`, `and`, `or`, `not`, quoted values). Element properties must permit filtering. An unquoted value that matches an element property name **of a compatible type** is compared as a property (`eq(name,value)` means `name == value`; `eq(clientName,id)` falls back to the literal `"id"` because `int` cannot be compared to a string); quote it (`eq(name,'value')`) to force the literal.
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
- **Null collections**: no null check is emitted around the collection itself — EF Core cannot translate one for collection navigations (they are never null in SQL). Under LINQ-to-Objects a `null` collection throws, exactly as `any()` does; initialize collections to empty lists. With `Ordering.Navigation = Safe`, dotted prefixes (`reference.orders`), the selector path and the whole predicate are null-guarded — the predicate is part of the ordering key, so it follows the ordering strategy rather than `Filter.Navigation`.
- **Cost**: the key is a correlated subquery in `ORDER BY`; it cannot use an index and paging forces a full sort of the filtered set. For very large tables prefer a denormalized sort column.
- For **filtering** by a collection element use `any(collection, predicate)`, e.g. `any(parameters,and(eq(name,priority),eq(value,high)))`.

## Using RQL mapping
In many projects, developers prefer to keep database entities separate from data transfer objects (DTOs), especially when their structures differ. To support this approach, Mpt.Rql includes built-in mapping functionality, which is enabled by default and relies on name-based matching. For more advanced scenarios, custom mappings can also be defined manually 

### Configure RQL
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddRql(options =>
    {
        // Instruct RQL to look for mappers in specific asembly 
        options.ScanForMappers(typeof(Program).Assembly); 
    });
}
```

### Specify mapping
```csharp
internal class UserMapper : IRqlMapper<DbUser, User>
{
    public void MapEntity(IRqlMapperContext<DbUser, User> context)
    {
        context
        .MapStatic(t => t.Id, t => t.UserId)
        .MapStatic(t => t.Age, t => t.AgeInYears);
    }
}
```

### Apply RQL
```csharp
public class UserQueryBuilder(IRqlQueryable<DbUser, User> rql)
{
    public IQueryable<User> GetUsersOlderThan(IQueryable<DbUser> sourceQuery, int age)
    {
        var request = new RqlRequest
        {
            Filter = $"gt(age,{age})",          // Age must be greater than age specified
            Order = "-age",                     // Order by age desc
            Select = "id,name"                  // Select id and name
        };

        return rql.Transform(sourceQuery, request).Query;
    }
}
```

## Contributing

We welcome contributions to enhance the library. Please see our Contributing Guide for details:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/new-capability`)
3. Commit your changes (`git commit -m 'Add new capability'`)
4. Push to the branch (`git push origin feature/new-capability`)
5. Open a Pull Request

## License

This project is licensed under the Apache License 2.0 - see the [`LICENSE`](LICENSE) file for details.

## Acknowledgements

- The Marketplace team for creating and maintaining this library
- All contributors who have helped improve this project

---

Developed by the SWO Marketplace team
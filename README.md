# Mpt.Rql - Resource Query Language implementation for .NET

[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=softwareone-platform_mpt-rql-net&metric=alert_status)](https://github.com/softwareone/mpt-rql-net/actions)
[![NuGet](https://img.shields.io/nuget/v/Mpt.Rql)](https://www.nuget.org/packages/Mpt.Rql)
[![License](https://img.shields.io/github/license/softwareone-platform/mpt-rql-net)](LICENSE)

## Overview

SoftwareOne.Rql is a high-performance implementation of [Resource Query Language (RQL)](https://docs.platform.softwareone.com/developer-resources/rest-api/resource-query-language) for .NET applications. It enables API consumers to efficiently filter, sort, and paginate data with an intuitive and expressive syntax, delivering precise data retrieval capabilities.

## Features

- **Advanced Filtering** - Support for complex logical expressions (AND, OR, NOT)
- **Dynamic Sorting** - Multiple fields with ascending/descending options
- **Pagination** - Skip and limit results for efficient data loading
- **Field Selection** - Return only required fields to minimize payload size
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
            Filter = $"gt(age,{age})",          // Age must be greater than age specified
            Order = "-age",                     // Order by age desc
            Select = "id,name"                  // Select id and name
        };

        var response = rql.Transform(sourceQuery, request);

        if (response.IsSuccess)
            return response.Query;              // Return transformed query

        response.Errors.ForEach(t => { });      // Iterate through transformation errors (optional)

        Console.WriteLine(res.Graph.Print())    // Visualize the decision graph (optional)
    }
}
```

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
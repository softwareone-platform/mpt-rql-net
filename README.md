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

### Using RQL in a Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IQueryable<User> _users;
    private readonly IRqlProcessor _rqlProcessor;

    public UsersController(DbContext context, IRqlProcessor rqlProcessor)
    {
        _users = context.Users;
        _rqlProcessor = rqlProcessor;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string filter, 
                                              [FromQuery] string sort,
                                              [FromQuery] int? limit,
                                              [FromQuery] int? skip,
                                              [FromQuery] string fields)
    {
        // Apply RQL processing to the queryable
        var result = await _rqlProcessor.ProcessAsync(_users, new RqlOptions
        {
            Filter = filter,
            Sort = sort,
            Limit = limit,
            Skip = skip,
            Fields = fields
        });

        return Ok(result);
    }
}
```

## Advanced Usage Examples

### Custom Field Mapping

```csharp
services.AddRql(options =>
{
    options.ConfigureFieldMap<User>(map =>
    {
        map.MapField("fullName", user => $"{user.FirstName} {user.LastName}");
    });
});
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
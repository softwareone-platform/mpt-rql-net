# RQL 
POC to adopt RQL (https://connect.cloudblue.com/community/developers/api/rql/) to C# world. 
Created:
- RQL Expression Tree
- RQL Expression tree to .NET Expression Tree

Current state: alpha

# Rql.Linq
RQL stands for Resource Query Language, and it is a powerful way to query data on the web. 
RQL is based on a tree structure that represents the query logic and parameters. 

To translate a raw query into RQL, we need a parser that can parse the query string and build a RQL tree. 
Then, we need to convert the RQL tree into a .net Expression tree, which is a representation of lambda expressions that can be compiled and executed. 
The Expression tree can be used to transform an IQueryable<T> object, which is an interface for querying data sources that implement IEnumerable<T>. 

The advantage of using Linq for this task is that it is independent of the underlying data provider. 
You can use any data source that supports IQueryable<T>, such as Entity Framework, NHibernate, or MongoDB. 
Linq also provides extension points that allow you to customize its behavior if needed. 
For example, you can define your own query operators, permissons, or result transformers.

To use the SoftwareOne.Rql.Linq library in your API project, you need to install the corresponding nuget package. This package provides a set of extension methods and classes that enable you to write LINQ queries against the SoftwareOne REST API. To install the package, you can use the Package Manager Console in Visual Studio or the dotnet CLI tool. For example, to install the latest version of the package using the dotnet CLI, run the following command in your project directory:

``` csharp
dotnet add package SoftwareOne.Rql.Linq
```


## Usage
### Basic scenario:

Register Rql services using following code.

``` csharp
services.AddRql();
```

Inject `IRqlQueryable<TView>` interface into controller/service to start using it.  
Call `Transform()` method to apply transformation to IQueryable.

Example:
``` csharp
[HttpGet]
public async Task<IEnumerable<Product>> NoMappingExample(
    [FromServices] IRqlQueryable<Product> rqlQueryable,
    [FromQuery] string? query, [FromQuery] string? order, [FromQuery] string? select)
{
    IQueryable<Product> data = await _mediator.Send(new ListProductsQuery());
    IQueryable<Product> result = rqlQueryable.Transform(data, query, order, select);
    return result.Take(100).ToList();
}
```

### Advanced scenario:
Rql.Linq offers several extension points that can be customized to suit different scenarios and requirements.   
These extension points can be accessed through the AddRql() extension method, which allows configuring various aspects of the library's behavior.
``` csharp
services.AddRql(t =>
{
    // Use this to configure location of mappers
    t.ScanForMappings(typeof(Program).Assembly);
});
```

Sample code:

``` csharp
[HttpGet]
public async Task<IEnumerable<ProductView>> GetByExpressionTree(
    [FromServices] IRqlQueryable<Product, ProductView> rqlQueryable,
    [FromQuery] string? query, [FromQuery] string? order, [FromQuery] string? select)
{
    IQueryable<Product> data = await _mediator.Send(new ListProductsQuery());
    IQueryable<ProductView> result = rqlQueryable.Transform(data, query, order, select);
    return result.Take(100).ToList();
}
```

Mapper is a simple class implementing `IRqlMapper<TStore, TView>` interface:

``` csharp
internal class ProductViewMapper : IRqlMapper<Product, ProductView>
{
    public Expression<Func<Product, ProductView>> GetMapping()
        => (t) => new ProductView
        {
            Id = t.Id,
            Desc = t.Description,
            Name = t.ProductName,
            Category = t.Category,
            Price = t.Price,
            SellPrice = t.SalePrice,
            ListDate = t.ListDate,
        };
}
```

## Extensibility
Rql.Linq provides various extension points allowing to twea it's behavior to fit developer's needs. Most of them are accessible via `AddRql()` extension method.

``` csharp
services.AddRql(t =>
{
    // Use this mwthod to configure location of mappers
    t.ScanForMappings(typeof(Program).Assembly);

    // override behavior of various types of operators
    t.OverrideListOperator<IListIn, CustomListInOperatorHandler>();
    t.OverrideComparisonOperator<IEqual, CustomEqualOperatorHandler>();
    t.OverrideSearchOperator<ILike, CustomLikeOperatorHandler>();

    // by default Rql.Linq uses System.Text.Json as a property name provider
    // this behavior may be changed using following method
    t.OverridePropertyMapper<CustomPropertyNameProvider>();

    // Define what type of action will be available for all properties by default
    // This behaviour may be overriden by [RqlMember] attribute
    t.DefaultPermission = MemberPermission.All;
    // OR
    // t.DefaultPermission = MemberPermission.Filter;
    // t.DefaultPermission = MemberPermission.Order;
});
```

`[RqlMember]` attribute usage:

``` csharp
[RqlMember(MemberPermission.All)]
public class ProductView
{
    public int Id { get; set; }
    
    public string? Desc { get; set; }
    
    [RqlMember(MemberPermission.Filter)]
    public string Name { get; set; } = null!;
    
    [RqlMember(MemberPermission.Order)]
    public string Category { get; set; } = null!;
    
    [RqlMember(MemberPermission.None)]
    
    public DateTime ListDate { get; set; }
    
    public decimal Price { get; set; }

    public decimal SellPrice { get; set; }
}
```



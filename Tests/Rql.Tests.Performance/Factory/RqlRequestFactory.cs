using SoftwareOne.Rql;

namespace Rql.Tests.Performance.Factory;

internal static class RqlRequestFactory
{
    internal static RqlRequest Rql => new()
    {
        Filter = "and(eq(name,bobby),gt(id,3));date=123",
        Order = "-name,type",
        Select = "description"
    };

    internal static RqlRequest RqlLarge => new()
    {
        Filter = "or(and(eq(name,bobby),gt(id,3));date=123,le(date,1);(ilike(name,some);ilike(name,some1)),and(eq(name,bobby),gt(id,3));date=123,le(date,1);(ilike(name,some);ilike(name,some1)),and(eq(name,bobby),gt(id,3));date=123,le(date,1);(ilike(name,some);ilike(name,some1)),and(eq(name,bobby),gt(id,3));date=123,le(date,1);(ilike(name,some);ilike(name,some1)))",
        Order = "id,-name,type,-name,description",
        Select = "id,-name,type,-name,description"
    };

    internal static RqlRequest DynamicLinq => new()
    {
        Filter = "(Name = \"bobby\" AND Id > 3) OR Date = 123",
        Order = "Name desc, Type asc",
        Select = "new { Description }"
    };

    internal static RqlRequest DynamicLinqLarge = new()
    {
        Filter = "((Name = \"bobby\" AND Id > 3) OR Date = 123 OR date > 1 AND name =  \"some1\") OR ((Name = \"bobby\" AND Id > 3) OR Date = 123 OR date > 1 AND name =  \"some1\") OR ((Name = \"bobby\" AND Id > 3) OR Date = 123 OR date > 1 AND name =  \"some1\") OR (Name = \"bobby\" AND Id > 3) OR Date = 123 OR date > 1 AND name =  \"some1\"",
        Order = "Id asc, Name desc, Type asc, Name desc, Description asc",
        Select = "new { Id, Description, Name, Type }"
    };
}


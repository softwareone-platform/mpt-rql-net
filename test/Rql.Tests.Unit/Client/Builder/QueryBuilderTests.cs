using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Dsl;
using Xunit;

namespace Rql.Tests.Unit.Client.Builder;

public class QueryBuilderTests
{
    [Fact]
    public void WhenNothingIsSpecified_ThenDefaultPagingIsSet()
    {
        // Arrange & Act
        var (@operator, paging, (included, excluded), order) = ((IQueryBuilder)new QueryBuilder<User>()).Build();

        // Assert
        @operator.Should().BeOfType<EmptyOperator>();
        paging.Should().BeOfType<DefaultPaging>();
        included.Should().BeEmpty();
        excluded.Should().BeEmpty();
        order.Should().BeEmpty();
    }
}
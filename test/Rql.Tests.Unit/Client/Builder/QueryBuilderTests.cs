using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Builder.Paging;
using Xunit;

namespace Rql.Tests.Unit.Client.Builder;

public class QueryBuilderTests
{
    [Fact]
    public void WhenNothingIsSpecified_ThenDefaultPagingIsSet()
    {
        // Arrange & Act
        var (@operator, paging, (included, excluded), order) = new QueryBuilder<User>().Build();

        // Assert
        paging.Should().BeOfType<DefaultPaging>();
        included.Should().BeEmpty();
        excluded.Should().BeEmpty();
        order.Should().BeEmpty();
    }
}
using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Client.Builder.Paging;
using SoftwareOne.Rql.Client.Builder.Query;
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
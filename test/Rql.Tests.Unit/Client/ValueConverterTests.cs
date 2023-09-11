using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Core;
using Xunit;

namespace Rql.Tests.Unit.Client;

public class ValueConverterTests
{
    [Theory]
    [MemberData(nameof(GetData))]
    public void WhenDecimal_ThenConvertedSuccessfully(object value, string expected)
    {
        // Act
        var result = ValueConverter.Convert(value);

        // Assert
        result.Should().Be(expected);
    }

    public static IEnumerable<object?[]> GetData()
    {
        return new List<object?[]>
        {
            new object[] { TestEnum.One, "1"},
            new object[] { 5.1m, "5.1"},
            new object[] { (decimal?)5.1m, "5.1"},
            new object[] { 5.1F, "5.1"},
            new object[] { (float?)5.1F, "5.1"},
            new object[] { 5.1, "5.1"},
            new object[] { (double?)5.1, "5.1"},
            new object[] { 5, "5"},
            new object[] { (int?)5, "5"},
            new object ?[] { default, "null()"},
            new object[] { true, "true"},
            new object[] { false, "false"},
            new object[] { 'c', "'c'"},
            new object[] { "abc & def", "'abc & def'"},
            new object[] { "", "empty()"},
            new object[] { "   ", "empty()"},
            new object[] { new DateTime(2023, 08, 16, 1, 10, 15), "2023-08-16T01:10:15.0000000"},
            new object[] { new DateTimeOffset(2023, 08, 16, 1, 10, 15, new TimeSpan(0, 2, 0, 0)), "2023-08-16T01:10:15.0000000+02:00"},
        };
    }
}

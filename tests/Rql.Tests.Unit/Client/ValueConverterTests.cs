using FluentAssertions;
using Mpt.Rql.Client.Core;
using Rql.Tests.Unit.Client.Samples;
using Xunit;

namespace Rql.Tests.Unit.Client;

public class ValueConverterTests
{
    [Theory]
    [MemberData(nameof(GetData))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable", Justification = "test for various data types")]
    public void WhenDecimal_ThenConvertedSuccessfully(object value, string expected)
    {
        // Act
        var result = ValueConverter.Convert(value);

        // Assert
        result.Should().Be(expected);
    }

    public static TheoryData<object, string> GetData() => new()
    {
        { TestEnum.One, "1"},
        { 5.1m, "5.1"},
        { (decimal?)5.1m, "5.1"},
        { 5.1F, "5.1"},
        { (float?)5.1F, "5.1"},
        { 5.1, "5.1"},
        { (double?)5.1, "5.1"},
        { 5, "5"},
        { (int?)5, "5"},
        { default!, "null()"},
        { true, "true"},
        { false, "false"},
        { 'c', "'c'"},
        { "abc & def", "'abc & def'"},
        { "", "empty()"},
        { "   ", "empty()"},
        { new DateTime(2023, 08, 16, 1, 10, 15), "2023-08-16T01:10:15.0000000"},
        { new DateTimeOffset(2023, 08, 16, 1, 10, 15, new TimeSpan(0, 2, 0, 0)), "2023-08-16T01:10:15.0000000+02:00"},
    };
}

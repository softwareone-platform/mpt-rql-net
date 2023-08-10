using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;

[Expression(typeof(RqlNotEqual), typeof(NotEqual))]
public interface INotEqual : IComparisonOperator { }
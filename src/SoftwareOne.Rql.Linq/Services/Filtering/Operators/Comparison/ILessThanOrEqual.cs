using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;

[Expression(typeof(RqlLessThanOrEqual), typeof(LessThanOrEqual))]
public interface ILessThanOrEqual : IComparisonOperator { }
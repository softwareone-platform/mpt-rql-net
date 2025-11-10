using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Comparison;

[Expression(typeof(RqlLessThanOrEqual), typeof(LessThanOrEqual))]
public interface ILessThanOrEqual : IComparisonOperator, IActualOperator { }
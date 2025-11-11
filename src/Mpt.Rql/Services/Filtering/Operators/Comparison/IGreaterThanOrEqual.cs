using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;

namespace Mpt.Rql.Services.Filtering.Operators.Comparison;

[Expression(typeof(RqlGreaterThanOrEqual), typeof(GreaterThanOrEqual))]
public interface IGreaterThanOrEqual : IComparisonOperator, IActualOperator { }
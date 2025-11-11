using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;

namespace Mpt.Rql.Services.Filtering.Operators.Comparison;

[Expression(typeof(RqlNotEqual), typeof(NotEqual))]
public interface INotEqual : IComparisonOperator, IActualOperator { }
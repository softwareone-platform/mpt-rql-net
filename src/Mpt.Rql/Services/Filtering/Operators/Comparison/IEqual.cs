using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;

namespace Mpt.Rql.Services.Filtering.Operators.Comparison;

[Expression(typeof(RqlEqual), typeof(Equal))]
public interface IEqual : IComparisonOperator, IActualOperator { }
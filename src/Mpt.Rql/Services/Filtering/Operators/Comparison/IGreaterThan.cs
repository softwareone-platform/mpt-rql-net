using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;

namespace Mpt.Rql.Services.Filtering.Operators.Comparison;

[Expression(typeof(RqlGreaterThan), typeof(GreaterThan))]
public interface IGreaterThan : IComparisonOperator, IActualOperator { }
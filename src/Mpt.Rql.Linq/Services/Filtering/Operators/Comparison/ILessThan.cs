using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Comparison;

[Expression(typeof(RqlLessThan), typeof(LessThan))]
public interface ILessThan : IComparisonOperator, IActualOperator { }
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;

[Expression(typeof(RqlGreaterThanOrEqual), typeof(GreaterThanOrEqual))]
public interface IGreaterThanOrEqual : IComparisonOperator, IActualOperator { }
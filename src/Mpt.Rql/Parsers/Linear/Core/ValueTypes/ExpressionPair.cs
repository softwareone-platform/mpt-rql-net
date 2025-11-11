using Mpt.Rql.Abstractions;
using Mpt.Rql.Parsers.Linear.Domain.Core.Enumerations;

namespace Mpt.Rql.Parsers.Linear.Domain.Core.ValueTypes;

internal record struct ExpressionPair(GroupType Type, RqlExpression Expression);
